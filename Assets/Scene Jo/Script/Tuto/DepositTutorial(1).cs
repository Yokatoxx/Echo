using UnityEngine;
using System.Collections.Generic;

public class DepositTutorial : BaseTutorial
{
    private List<GameObject> depositObjects = new List<GameObject>();
    private List<GameObject> depositTexts = new List<GameObject>();
    private List<CollectibleCount> collectibleCountScripts = new List<CollectibleCount>();
    
    // Suivi des CollectibleCount
    private Dictionary<CollectibleCount, int> previousCollectibleCounts = new Dictionary<CollectibleCount, int>();
    private float lastCollectibleCheck = 0f;

    protected override void OnInitialize()
    {
        // Trouver tous les points de dépôt
        GameObject[] deposits = GameObject.FindGameObjectsWithTag("Deposit");
        depositObjects.AddRange(deposits);

        // Configurer le suivi des CollectibleCount
        SetupCollectibleCountTracking();

        // Créer les textes de dépôt
        CreateDepositTexts();

        Debug.Log($"DepositTutorial: Trouvé {depositObjects.Count} points de dépôt et {collectibleCountScripts.Count} CollectibleCount scripts");
    }

    private void SetupCollectibleCountTracking()
    {
        // Chercher tous les scripts CollectibleCount dans la scène
        CollectibleCount[] allCollectibleCounts = FindObjectsOfType<CollectibleCount>();
        collectibleCountScripts.AddRange(allCollectibleCounts);

        // Initialiser le dictionnaire avec les valeurs actuelles
        foreach (CollectibleCount collectibleCount in collectibleCountScripts)
        {
            if (collectibleCount != null)
            {
                int currentCount = GetCollectibleCountValue(collectibleCount);
                previousCollectibleCounts[collectibleCount] = currentCount;
                Debug.Log($"CollectibleCount trouvé sur: {collectibleCount.gameObject.name} - Count initial: {currentCount}");
            }
        }
    }

    private void CreateDepositTexts()
    {
        for (int i = 0; i < depositObjects.Count; i++)
        {
            GameObject deposit = depositObjects[i];
            Vector3 textPosition = textManager.CalculateTextPosition(
                deposit.transform.position,
                tutorialData.depositTextHeightOffset
            );

            GameObject textObj = textManager.CreateWorldText(
                $"deposit_{i}",
                tutorialData.depositText,
                tutorialData.depositTextColor,
                tutorialData.depositTextSize,
                textPosition,
                tutorialData.depositTextScale
            );

            depositTexts.Add(textObj);

            // Gérer la visibilité initiale
            if (!tutorialData.alwaysShowDepositTexts)
            {
                textManager.HideText($"deposit_{i}");
            }
            else
            {
                textManager.ShowText($"deposit_{i}");
            }
        }
    }

    protected override void OnStartTutorial()
    {
        // Mettre à jour la visibilité des textes
        UpdateDepositTextsVisibility();
    }

    protected override void OnUpdateTutorial()
    {
        // Mettre à jour la visibilité des textes selon la distance
        UpdateDepositTextsVisibility();

        // Vérifier les CollectibleCount périodiquement
        if (Time.time - lastCollectibleCheck >= tutorialData.collectibleCheckInterval)
        {
            CheckCollectibleCounts();
            lastCollectibleCheck = Time.time;
        }
    }

    private void UpdateDepositTextsVisibility()
    {
        if (tutorialData.alwaysShowDepositTexts) return;

        Vector3 playerPosition = GetPlayerPosition();

        for (int i = 0; i < depositObjects.Count; i++)
        {
            GameObject deposit = depositObjects[i];
            if (deposit != null)
            {
                float distance = Vector3.Distance(playerPosition, deposit.transform.position);
                bool shouldShow = distance <= tutorialData.depositTextDisplayDistance;

                if (shouldShow)
                {
                    textManager.ShowText($"deposit_{i}");
                }
                else
                {
                    textManager.HideText($"deposit_{i}");
                }
            }
        }
    }

    private void CheckCollectibleCounts()
    {
        foreach (CollectibleCount collectibleCount in collectibleCountScripts)
        {
            if (collectibleCount != null && previousCollectibleCounts.ContainsKey(collectibleCount))
            {
                int currentCount = GetCollectibleCountValue(collectibleCount);
                int previousCount = previousCollectibleCounts[collectibleCount];

                // Si le count a augmenté, un collectible a été déposé
                if (currentCount > previousCount)
                {
                    Debug.Log($"Collectible déposé détecté ! {collectibleCount.gameObject.name}: {previousCount} → {currentCount}");
                    OnCollectibleDeposited(collectibleCount, currentCount - previousCount);

                    // Mettre à jour la valeur précédente
                    previousCollectibleCounts[collectibleCount] = currentCount;
                }
            }
        }
    }

    private void OnCollectibleDeposited(CollectibleCount collectibleCount, int addedCount)
    {
        Debug.Log($"{addedCount} collectible(s) déposé(s) dans {collectibleCount.gameObject.name} - Tutoriel dépôt validé !");
        CompleteTutorial();
    }

    private int GetCollectibleCountValue(CollectibleCount collectibleCount)
    {
        if (collectibleCount == null) return 0;

        try
        {
            // Méthode 1: Essayer d'accéder via une propriété publique
            var countProperty = collectibleCount.GetType().GetProperty("Count");
            if (countProperty != null)
            {
                return (int)countProperty.GetValue(collectibleCount);
            }

            // Méthode 2: Essayer d'accéder via un champ public
            var countField = collectibleCount.GetType().GetField("collectibleCount");
            if (countField != null)
            {
                return (int)countField.GetValue(collectibleCount);
            }

            // Méthode 3: Essayer d'accéder via un champ privé
            countField = collectibleCount.GetType().GetField("collectibleCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (countField != null)
            {
                return (int)countField.GetValue(collectibleCount);
            }

            // Méthode 4: Autres noms possibles
            string[] possibleFieldNames = { "count", "_count", "totalCount", "_collectibleCount", "counter" };
            foreach (string fieldName in possibleFieldNames)
            {
                countField = collectibleCount.GetType().GetField(fieldName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (countField != null && countField.FieldType == typeof(int))
                {
                    return (int)countField.GetValue(collectibleCount);
                }
            }

            Debug.LogWarning($"Impossible de trouver le champ count dans {collectibleCount.gameObject.name}");
            return 0;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors de l'accès au count de {collectibleCount.gameObject.name}: {e.Message}");
            return 0;
        }
    }

    protected override void OnCompleteTutorial()
    {
        // Cacher tous les textes de dépôt
        HideAllDepositTexts();

        // Activer la sortie si elle existe
        GameObject exit = GameObject.FindGameObjectWithTag("Finish");
        if (exit != null)
        {
            WinManager exitComponent = exit.GetComponent<WinManager>();
            if (exitComponent != null)
            {
                exitComponent.isCollectComplete = true;
            }
        }
    }

    protected override void OnStopTutorial()
    {
        HideAllDepositTexts();
    }

    private void HideAllDepositTexts()
    {
        for (int i = 0; i < depositTexts.Count; i++)
        {
            textManager.HideText($"deposit_{i}");
        }
    }

    private void ShowAllDepositTexts()
    {
        if (!isCompleted)
        {
            for (int i = 0; i < depositTexts.Count; i++)
            {
                textManager.ShowText($"deposit_{i}");
            }
        }
    }

    // Méthodes publiques pour contrôle externe
    public void NotifyCollectibleDeposited(CollectibleCount collectibleCount)
    {
        OnCollectibleDeposited(collectibleCount, 1);
    }

    public void NotifyCollectibleDeposited()
    {
        if (!isCompleted)
        {
            Debug.Log("Collectible déposé notifié - Tutoriel dépôt validé !");
            CompleteTutorial();
        }
    }

    public void SetAlwaysShowDepositTexts(bool alwaysShow)
    {
        // Cette méthode peut être appelée pour changer le mode d'affichage
        if (alwaysShow && !isCompleted)
        {
            ShowAllDepositTexts();
        }
        else
        {
            UpdateDepositTextsVisibility();
        }
    }

    // Méthodes de debug
    public void DebugShowDepositDistances()
    {
        Vector3 playerPosition = GetPlayerPosition();
        Debug.Log("=== DISTANCES AVEC LES DÉPÔTS ===");

        for (int i = 0; i < depositObjects.Count; i++)
        {
            GameObject deposit = depositObjects[i];
            if (deposit != null)
            {
                float distance = Vector3.Distance(playerPosition, deposit.transform.position);
                bool isVisible = distance <= tutorialData.depositTextDisplayDistance;
                Debug.Log($"Dépôt {i} ({deposit.name}): {distance:F2}m (seuil: {tutorialData.depositTextDisplayDistance}m) - {(isVisible ? "VISIBLE" : "CACHÉ")}");
            }
        }
    }
}