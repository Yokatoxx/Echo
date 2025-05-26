using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("Références")]
    public PlayerMovement playerMovement;
    public ChargeableEchoScanner echoScanner;
    public PlayerHandController handController;
    public Camera playerCamera;

    [Header("UI Tutorial")]
    public GameObject echoTutorialPanel; // Panel UI classique

    [Header("Canvas Settings")]
    [Tooltip("Canvas pour afficher les textes de tutoriel au premier plan")]
    public Canvas tutorialCanvas;
    [Tooltip("Si true, créé automatiquement un canvas")]
    public bool autoCreateCanvas = true;

    [Header("Paramètres Texte Dépôt")]
    [Range(0.5f, 5f)]
    public float depositTextHeightOffset = 0.5f;
    [Range(0.5f, 10f)]
    public float depositTextSize = 2f;
    [Range(0.1f, 3f)]
    public float depositTextScale = 1f;
    [Tooltip("Distance maximale d'affichage des textes de dépôt")]
    [Range(5f, 50f)]
    public float depositTextDisplayDistance = 15f;
    [Tooltip("Si true, les textes de dépôt sont toujours visibles")]
    public bool alwaysShowDepositTexts = false;

    [Header("Paramètres Texte Collectible")]
    [Range(0.5f, 5f)]
    public float collectibleTextHeightOffset = 1.2f;
    [Range(0.5f, 10f)]
    public float collectibleTextSize = 1.8f;
    [Range(0.1f, 3f)]
    public float collectibleTextScale = 0.8f;

    [Header("Paramètres Texte Echo")]
    [Range(0.5f, 10f)]
    public float echoTextSize = 3f;
    [Range(0.1f, 3f)]
    public float echoTextScale = 1.2f;
    [Range(1f, 10f)]
    public float echoTextDistance = 5f; // Distance devant la caméra
    [Range(-2f, 2f)]
    public float echoTextVerticalOffset = 0f; // Décalage vertical

    [Header("⭐ TUTORIEL STEALTH ⭐")]
    [Space(10)]
    [Tooltip("Distance à laquelle le tutoriel stealth se déclenche")]
    [Range(5f, 50f)]
    public float stealthTriggerDistance = 15f;
    [Tooltip("Distance du texte stealth devant la caméra (pour le centrage à l'écran)")]
    [Range(1f, 10f)]
    public float stealthTextDistance = 3f;
    [Range(0.5f, 10f)]
    public float stealthTextSize = 3f;
    [Range(0.1f, 3f)]
    public float stealthTextScale = 1.2f;
    [Range(-2f, 2f)]
    public float stealthTextVerticalOffset = 0f;

    [Header("🎨 FOND STEALTH 🎨")]
    [Space(10)]
    [Tooltip("Afficher un fond derrière le texte stealth")]
    public bool showStealthBackground = true;
    [Tooltip("Couleur du fond stealth")]
    [SerializeField] private Color stealthBackgroundColor = new Color(0, 0, 0, 0.7f); // Noir semi-transparent
    [Tooltip("Padding autour du texte stealth")]
    [Range(10f, 100f)]
    public float stealthBackgroundPadding = 50f;
    [Tooltip("Rayon des coins arrondis du fond")]
    [Range(0f, 50f)]
    public float stealthBackgroundCornerRadius = 20f;
    [Tooltip("Effet de flou/ombre derrière le fond")]
    public bool stealthBackgroundShadow = true;

    [Header("⭐ TUTORIEL DÉPÔT ⭐")]
    [Space(10)]
    [Tooltip("Fréquence de vérification des CollectibleCount (en secondes)")]
    [Range(0.1f, 2f)]
    public float collectibleCheckInterval = 0.5f;

    private GameObject backpack;
    private GameObject exit;

    [Header("⭐ TEXTES PERSONNALISABLES ⭐")]
    [Space(10)]
    [TextArea(2, 4)]
    public string echoTutorialText = "Appuyez sur ESPACE pour faire un écho";
    [TextArea(2, 4)]
    public string depositText = "↓ Déposez ici ↓";
    [TextArea(2, 4)]
    public string collectibleText = "Appuyez sur E";
    [TextArea(2, 4)]
    public string stealthTutorialText = "Maintenez SHIFT pour vous déplacer en mode furtif";

    [Header("🎨 COULEURS PERSONNALISABLES 🎨")]
    [Space(10)]
    [SerializeField] private Color echoTextColor = Color.cyan;
    [SerializeField] private Color depositTextColor = Color.green;
    [SerializeField] private Color collectibleTextColor = Color.yellow;
    [SerializeField] private Color stealthTextColor = Color.white; // Changé en blanc pour contraster avec le fond

    [Header("⚙️ PARAMÈTRES COLLECTIBLES ⚙️")]
    [Space(10)]
    [Tooltip("Tolérance pour considérer qu'un blendshape est à sa valeur de repos")]
    [Range(0.01f, 10f)]
    public float blendshapeRestingTolerance = 0.1f;

    [Header("Autres paramètres")]
    public float raycastDistance = 5f;
    public LayerMask collectibleLayer = -1;

    [Header("États des tutoriels")]
    public bool echoTutorialCompleted = false;
    public bool stealthTutorialCompleted = false;
    public bool collectibleTutorialCompleted = false;
    public bool depositTutorialCompleted = false;

    // Propriétés publiques pour accéder aux couleurs depuis l'inspector
    public Color EchoTextColor
    {
        get { return echoTextColor; }
        set
        {
            echoTextColor = value;
            UpdateEchoTextColor();
        }
    }

    public Color DepositTextColor
    {
        get { return depositTextColor; }
        set
        {
            depositTextColor = value;
            UpdateDepositTextsColor();
        }
    }

    public Color CollectibleTextColor
    {
        get { return collectibleTextColor; }
        set
        {
            collectibleTextColor = value;
            UpdateCollectibleTextColor();
        }
    }

    public Color StealthTextColor
    {
        get { return stealthTextColor; }
        set
        {
            stealthTextColor = value;
            UpdateStealthTextColor();
        }
    }

    public Color StealthBackgroundColor
    {
        get { return stealthBackgroundColor; }
        set
        {
            stealthBackgroundColor = value;
            UpdateStealthBackgroundColor();
        }
    }

    // Listes pour stocker les objets et leurs textes
    private List<GameObject> collectibleObjects = new List<GameObject>();
    private List<GameObject> depositObjects = new List<GameObject>();
    private List<GameObject> depositTexts = new List<GameObject>();
    private List<GameObject> enemyObjects = new List<GameObject>();
    private List<CollectibleCount> collectibleCountScripts = new List<CollectibleCount>();

    private bool isShowingEchoTutorial = false;
    private bool isShowingStealthTutorial = false;
    private bool hasTriggeredStealthTutorial = false;
    private Camera mainCamera;
    private GameObject currentCollectibleText;
    private GameObject echoWorldText; // Texte 3D pour l'écho
    private GameObject stealthWorldText; // Texte 3D pour le stealth
    private GameObject stealthBackgroundPanel; // Fond pour le texte stealth
    private float originalTimeScale = 1f;

    // Variables pour le suivi des CollectibleCount
    private Dictionary<CollectibleCount, int> previousCollectibleCounts = new Dictionary<CollectibleCount, int>();
    private float lastCollectibleCheck = 0f;

    // Matériaux pour texte toujours visible
    private Material alwaysOnTopMaterial;
    private Material normalTextMaterial;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = playerCamera;

        originalTimeScale = Time.timeScale;

        backpack = GameObject.FindGameObjectWithTag("Backpack");
        exit = GameObject.FindGameObjectWithTag("Finish");

        // Créer les matériaux spéciaux
        CreateAlwaysOnTopMaterials();

        // Créer ou configurer le canvas pour les tutoriels
        SetupTutorialCanvas();

        FindAllTaggedObjects();
        SetupCollectibleCountTracking();
        CreateDepositTexts();
        CreateEchoWorldText();
        CreateStealthWorldText();

        if (playerMovement != null)
        {
            playerMovement.DisableMovement();
        }

        StartEchoTutorial();
    }

    void CreateAlwaysOnTopMaterials()
    {
        // Matériau pour texte toujours visible (devant tout)
        alwaysOnTopMaterial = new Material(Shader.Find("Sprites/Default"));
        alwaysOnTopMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        alwaysOnTopMaterial.SetFloat("_ZWrite", 0);
        alwaysOnTopMaterial.renderQueue = 4000; // Queue très élevée

        // Matériau normal pour les autres textes
        normalTextMaterial = new Material(Shader.Find("TextMeshPro/Distance Field"));
    }

    void SetupTutorialCanvas()
    {
        if (tutorialCanvas == null && autoCreateCanvas)
        {
            // Créer un nouveau Canvas pour les tutoriels
            GameObject canvasObj = new GameObject("TutorialCanvas");
            tutorialCanvas = canvasObj.AddComponent<Canvas>();

            // Configuration du Canvas pour être toujours au premier plan
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tutorialCanvas.sortingOrder = 32767; // Maximum possible
            tutorialCanvas.overrideSorting = true;

            // Ajouter les composants nécessaires
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("Canvas de tutoriel créé en mode ScreenSpace-Overlay");
        }

        if (tutorialCanvas != null)
        {
            // S'assurer que le canvas a un sortingOrder élevé
            tutorialCanvas.sortingOrder = 32767; // Maximum possible
            tutorialCanvas.overrideSorting = true;
            tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay; // Force en overlay
        }
    }

    void Update()
    {
        UpdateEchoTextPosition(); // Mettre à jour la position du texte d'écho en continu
        UpdateStealthTextPosition(); // Mettre à jour la position du texte stealth
        UpdateDepositTextsVisibility(); // Mettre à jour la visibilité des textes de dépôt

        // Vérifier les CollectibleCount périodiquement
        if (!depositTutorialCompleted && Time.time - lastCollectibleCheck >= collectibleCheckInterval)
        {
            CheckCollectibleCounts();
            lastCollectibleCheck = Time.time;
        }

        if (!echoTutorialCompleted)
        {
            HandleEchoTutorial();
        }
        else
        {
            // Après l'écho, tous les autres tutoriels peuvent se faire dans n'importe quel ordre
            if (!stealthTutorialCompleted)
            {
                HandleStealthTutorial();
            }

            if (!collectibleTutorialCompleted)
            {
                HandleCollectibleTutorial();
            }

            // Le tutoriel de dépôt se gère automatiquement via la vérification des CollectibleCount
        }
    }

    void FindAllTaggedObjects()
    {
        GameObject[] collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        collectibleObjects.AddRange(collectibles);

        GameObject[] deposits = GameObject.FindGameObjectsWithTag("Deposit");
        depositObjects.AddRange(deposits);

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        enemyObjects.AddRange(enemies);

        Debug.Log($"Trouvé {collectibleObjects.Count} objets collectibles, {depositObjects.Count} points de dépôt et {enemyObjects.Count} ennemis");
    }

    /// <summary>
    /// Configure le suivi des scripts CollectibleCount
    /// </summary>
    void SetupCollectibleCountTracking()
    {
        // Chercher tous les scripts CollectibleCount dans la scène
        CollectibleCount[] allCollectibleCounts = FindObjectsOfType<CollectibleCount>();
        collectibleCountScripts.AddRange(allCollectibleCounts);

        // Initialiser le dictionnaire avec les valeurs actuelles
        foreach (CollectibleCount collectibleCount in collectibleCountScripts)
        {
            if (collectibleCount != null)
            {
                // Obtenir la valeur actuelle du count
                int currentCount = GetCollectibleCountValue(collectibleCount);
                previousCollectibleCounts[collectibleCount] = currentCount;

                Debug.Log($"CollectibleCount trouvé sur: {collectibleCount.gameObject.name} - Count initial: {currentCount}");
            }
        }

        Debug.Log($"Configuré le suivi pour {collectibleCountScripts.Count} scripts CollectibleCount");
    }

    /// <summary>
    /// Obtient la valeur du count depuis le script CollectibleCount
    /// </summary>
    /// <param name="collectibleCount">Le script CollectibleCount</param>
    /// <returns>La valeur actuelle du count</returns>
    int GetCollectibleCountValue(CollectibleCount collectibleCount)
    {
        if (collectibleCount == null) return 0;

        try
        {
            // Méthode 1: Essayer d'accéder via une propriété publique
            // Si CollectibleCount a une propriété publique comme 'Count' ou 'collectibleCount'
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

            // Méthode 3: Essayer d'accéder via un champ privé (si nécessaire)
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

    /// <summary>
    /// Vérifie si les counts des CollectibleCount ont changé
    /// </summary>
    void CheckCollectibleCounts()
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

    /// <summary>
    /// Méthode appelée quand un collectible est déposé
    /// </summary>
    /// <param name="collectibleCount">Le script CollectibleCount qui a changé</param>
    /// <param name="addedCount">Le nombre de collectibles ajoutés</param>
    void OnCollectibleDeposited(CollectibleCount collectibleCount, int addedCount)
    {
        if (!depositTutorialCompleted)
        {
            Debug.Log($"{addedCount} collectible(s) déposé(s) dans {collectibleCount.gameObject.name} - Tutoriel dépôt validé !");
            CompleteDepositTutorial();
        }
        else
        {
            Debug.Log($"{addedCount} collectible(s) déposé(s) dans {collectibleCount.gameObject.name} - Tutoriel déjà terminé");
        }
    }

    /// <summary>
    /// Méthode publique pour forcer la validation du tutoriel dépôt
    /// À appeler depuis CollectibleCount si vous pouvez modifier ce script
    /// </summary>
    public void NotifyCollectibleDeposited(CollectibleCount collectibleCount)
    {
        OnCollectibleDeposited(collectibleCount, 1);
    }

    /// <summary>
    /// Méthode publique alternative
    /// </summary>
    public void NotifyCollectibleDeposited()
    {
        if (!depositTutorialCompleted)
        {
            Debug.Log("Collectible déposé notifié - Tutoriel dépôt validé !");
            CompleteDepositTutorial();
        }
    }

    void CreateDepositTexts()
    {
        for (int i = 0; i < depositObjects.Count; i++)
        {
            GameObject deposit = depositObjects[i];
            GameObject textObj = CreateDepositText(depositText, deposit.transform.position);
            depositTexts.Add(textObj);

            // Si pas en mode "toujours visible", cacher le texte au début
            if (!alwaysShowDepositTexts)
            {
                textObj.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Met à jour la visibilité des textes de dépôt en fonction de la distance du joueur
    /// ET de l'état du tutoriel de dépôt
    /// </summary>
    void UpdateDepositTextsVisibility()
    {
        if (depositTutorialCompleted)
        {
            // Si le tutoriel de dépôt est terminé, cacher tous les textes
            foreach (GameObject depositText in depositTexts)
            {
                if (depositText != null && depositText.activeInHierarchy)
                {
                    depositText.SetActive(false);
                }
            }
            return;
        }

        if (alwaysShowDepositTexts || playerMovement == null) return;

        Vector3 playerPosition = playerMovement.transform.position;

        for (int i = 0; i < depositObjects.Count && i < depositTexts.Count; i++)
        {
            GameObject deposit = depositObjects[i];
            GameObject depositText = depositTexts[i];

            if (deposit != null && depositText != null)
            {
                float distance = Vector3.Distance(playerPosition, deposit.transform.position);
                bool shouldShow = distance <= depositTextDisplayDistance;

                // Activer/désactiver le texte selon la distance
                if (depositText.activeInHierarchy != shouldShow)
                {
                    depositText.SetActive(shouldShow);

                    // Debug optionnel
                    if (Debug.isDebugBuild && shouldShow)
                    {
                        Debug.Log($"Texte de dépôt {i} affiché - Distance: {distance:F2}m (seuil: {depositTextDisplayDistance}m)");
                    }
                }
            }
        }
    }

    void CreateEchoWorldText()
    {
        if (tutorialCanvas != null)
        {
            // Créer le texte UI sur le canvas overlay
            echoWorldText = new GameObject("EchoTutorialText");
            echoWorldText.transform.SetParent(tutorialCanvas.transform, false);

            // Utiliser TextMeshProUGUI pour les canvas
            TextMeshProUGUI textMesh = echoWorldText.AddComponent<TextMeshProUGUI>();
            textMesh.text = echoTutorialText;
            textMesh.color = echoTextColor;
            textMesh.fontSize = echoTextSize * 10f; // Plus grand pour UI
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;

            // Centrer le texte sur l'écran
            RectTransform rectTransform = echoWorldText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(800, 200);
        }
        else
        {
            // Fallback : créer comme TextMeshPro 3D avec matériau spécial
            echoWorldText = CreateAlwaysVisibleText("EchoTutorialText", echoTutorialText, echoTextColor, echoTextSize);
        }

        // Désactiver au début
        echoWorldText.SetActive(false);
    }

    void CreateStealthWorldText()
    {
        if (tutorialCanvas != null)
        {
            // D'abord créer le fond si activé
            if (showStealthBackground)
            {
                CreateStealthBackground();
            }

            // Créer le texte UI sur le canvas overlay
            stealthWorldText = new GameObject("StealthTutorialText");
            stealthWorldText.transform.SetParent(tutorialCanvas.transform, false);

            // Utiliser TextMeshProUGUI pour les canvas
            TextMeshProUGUI textMesh = stealthWorldText.AddComponent<TextMeshProUGUI>();
            textMesh.text = stealthTutorialText;
            textMesh.color = stealthTextColor;
            textMesh.fontSize = stealthTextSize * 10f; // Plus grand pour UI
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;

            // Ajouter un effet d'ombre au texte pour plus de lisibilité
            Shadow textShadow = stealthWorldText.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0, 0, 0, 0.8f);
            textShadow.effectDistance = new Vector2(3, -3);

            // Centrer le texte sur l'écran
            RectTransform rectTransform = stealthWorldText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(1000, 300);

            // S'assurer que le texte est devant le fond
            stealthWorldText.transform.SetAsLastSibling();
        }
        else
        {
            // Fallback : créer comme TextMeshPro 3D avec matériau spécial
            stealthWorldText = CreateAlwaysVisibleText("StealthTutorialText", stealthTutorialText, stealthTextColor, stealthTextSize);
        }

        // Désactiver au début
        stealthWorldText.SetActive(false);
    }

    void CreateStealthBackground()
    {
        if (tutorialCanvas == null) return;

        // Créer l'objet fond
        stealthBackgroundPanel = new GameObject("StealthBackgroundPanel");
        stealthBackgroundPanel.transform.SetParent(tutorialCanvas.transform, false);

        // Ajouter un composant Image pour le fond
        Image backgroundImage = stealthBackgroundPanel.AddComponent<Image>();

        // Créer une texture arrondie pour le fond
        Texture2D roundedTexture = CreateRoundedTexture(200, 100, (int)stealthBackgroundCornerRadius);
        Sprite roundedSprite = Sprite.Create(roundedTexture, new Rect(0, 0, roundedTexture.width, roundedTexture.height), new Vector2(0.5f, 0.5f));

        backgroundImage.sprite = roundedSprite;
        backgroundImage.color = stealthBackgroundColor;
        backgroundImage.type = Image.Type.Sliced;

        // Positionner et dimensionner le fond
        RectTransform bgRectTransform = stealthBackgroundPanel.GetComponent<RectTransform>();
        bgRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        bgRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        bgRectTransform.anchoredPosition = Vector2.zero;
        bgRectTransform.sizeDelta = new Vector2(1000 + stealthBackgroundPadding * 2, 300 + stealthBackgroundPadding * 2);

        // Ajouter une ombre si demandée
        if (stealthBackgroundShadow)
        {
            Shadow bgShadow = stealthBackgroundPanel.AddComponent<Shadow>();
            bgShadow.effectColor = new Color(0, 0, 0, 0.5f);
            bgShadow.effectDistance = new Vector2(5, -5);
        }

        // S'assurer que le fond est derrière le texte
        stealthBackgroundPanel.transform.SetAsFirstSibling();

        // Désactiver au début
        stealthBackgroundPanel.SetActive(false);
    }

    Texture2D CreateRoundedTexture(int width, int height, int cornerRadius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Calculer la distance aux coins
                float distanceToCorner = 0f;

                // Coin top-left
                if (x < cornerRadius && y < cornerRadius)
                {
                    distanceToCorner = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius));
                }
                // Coin top-right
                else if (x >= width - cornerRadius && y < cornerRadius)
                {
                    distanceToCorner = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, cornerRadius));
                }
                // Coin bottom-left
                else if (x < cornerRadius && y >= height - cornerRadius)
                {
                    distanceToCorner = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, height - cornerRadius - 1));
                }
                // Coin bottom-right
                else if (x >= width - cornerRadius && y >= height - cornerRadius)
                {
                    distanceToCorner = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, height - cornerRadius - 1));
                }

                // Si on est dans une zone de coin et au-delà du rayon, transparent
                if ((x < cornerRadius || x >= width - cornerRadius) &&
                    (y < cornerRadius || y >= height - cornerRadius) &&
                    distanceToCorner > cornerRadius)
                {
                    colors[y * width + x] = Color.clear;
                }
                else
                {
                    colors[y * width + x] = Color.white;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    GameObject CreateAlwaysVisibleText(string name, string text, Color color, float fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.localScale = Vector3.one;

        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.rectTransform.sizeDelta = new Vector2(12, 4);

        // Appliquer le matériau spécial pour être toujours visible
        MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
        if (meshRenderer != null && alwaysOnTopMaterial != null)
        {
            meshRenderer.material = alwaysOnTopMaterial;
            meshRenderer.material.color = color;
        }

        // S'assurer que l'objet a la layer la plus prioritaire
        textObj.layer = 31; // Layer très haute

        return textObj;
    }

    GameObject CreateDepositText(string text, Vector3 worldPosition)
    {
        GameObject textObj = new GameObject("DepositText");
        Vector3 textPosition = CalculateDepositTextPosition(worldPosition);
        textObj.transform.position = textPosition;
        textObj.transform.localScale = Vector3.one * depositTextScale;

        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.color = depositTextColor;
        textMesh.fontSize = depositTextSize;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.alignment = TextAlignmentOptions.Center;

        textMesh.rectTransform.sizeDelta = new Vector2(5, 2);
        textMesh.sortingOrder = 900; // Haut mais moins que les tutoriels

        textObj.AddComponent<Billboard>();

        return textObj;
    }

    GameObject CreateCollectibleText(string text, Vector3 worldPosition)
    {
        GameObject textObj = new GameObject("CollectibleText");
        Vector3 textPosition = CalculateCollectibleTextPosition(worldPosition);
        textObj.transform.position = textPosition;
        textObj.transform.localScale = Vector3.one * collectibleTextScale;

        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.color = collectibleTextColor;
        textMesh.fontSize = collectibleTextSize;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.alignment = TextAlignmentOptions.Center;

        textMesh.rectTransform.sizeDelta = new Vector2(4, 1.5f);
        textMesh.sortingOrder = 950; // Haut mais moins que les tutoriels

        textObj.AddComponent<Billboard>();

        return textObj;
    }

    Vector3 CalculateDepositTextPosition(Vector3 objectPosition)
    {
        Vector3 textPosition = objectPosition;
        Collider objCollider = GetColliderAtPosition(objectPosition);
        if (objCollider != null)
        {
            textPosition.y = objCollider.bounds.max.y + depositTextHeightOffset;
        }
        else
        {
            textPosition.y += depositTextHeightOffset;
        }
        return textPosition;
    }

    Vector3 CalculateCollectibleTextPosition(Vector3 objectPosition)
    {
        Vector3 textPosition = objectPosition;
        Collider objCollider = GetColliderAtPosition(objectPosition);
        if (objCollider != null)
        {
            textPosition.y = objCollider.bounds.max.y + collectibleTextHeightOffset;
        }
        else
        {
            textPosition.y += collectibleTextHeightOffset;
        }
        return textPosition;
    }

    void UpdateEchoTextPosition()
    {
        if (echoWorldText != null && echoWorldText.activeInHierarchy && tutorialCanvas == null)
        {
            // Seulement si on utilise le fallback 3D
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraPosition = mainCamera.transform.position;

            Vector3 textPosition = cameraPosition + cameraForward * echoTextDistance;
            textPosition.y += echoTextVerticalOffset;

            echoWorldText.transform.position = textPosition;
            echoWorldText.transform.LookAt(mainCamera.transform);
            echoWorldText.transform.Rotate(0, 180, 0);
        }
    }

    void UpdateStealthTextPosition()
    {
        if (stealthWorldText != null && stealthWorldText.activeInHierarchy && tutorialCanvas == null)
        {
            // Seulement si on utilise le fallback 3D
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraPosition = mainCamera.transform.position;

            Vector3 textPosition = cameraPosition + cameraForward * stealthTextDistance;
            textPosition.y += stealthTextVerticalOffset;

            stealthWorldText.transform.position = textPosition;
            stealthWorldText.transform.LookAt(mainCamera.transform);
            stealthWorldText.transform.Rotate(0, 180, 0);
        }
    }

    Collider GetColliderAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 1f);
        if (colliders.Length > 0)
        {
            return colliders[0];
        }
        return null;
    }

    void StartEchoTutorial()
    {
        if (echoWorldText != null)
        {
            echoWorldText.SetActive(true);
            isShowingEchoTutorial = true;
        }

        if (echoTutorialPanel != null)
        {
            echoTutorialPanel.SetActive(false);
        }
    }

    void HandleEchoTutorial()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (echoWorldText != null && isShowingEchoTutorial)
            {
                echoWorldText.SetActive(false);
                isShowingEchoTutorial = false;
            }
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!isShowingEchoTutorial)
            {
                CompleteEchoTutorial();
            }
        }
    }

    void CompleteEchoTutorial()
    {
        echoTutorialCompleted = true;

        if (playerMovement != null)
        {
            playerMovement.EnableMovement();
        }

        Debug.Log("Tutoriel écho terminé - Mouvement activé - Tous les autres tutoriels sont maintenant disponibles !");
    }

    void HandleStealthTutorial()
    {
        if (!hasTriggeredStealthTutorial && IsPlayerNearEnemy())
        {
            StartStealthTutorial();
        }

        if (isShowingStealthTutorial)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                CompleteStealthTutorial();
            }
        }
    }

    bool IsPlayerNearEnemy()
    {
        if (playerMovement == null) return false;

        Vector3 playerPosition = playerMovement.transform.position;

        foreach (GameObject enemy in enemyObjects)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(playerPosition, enemy.transform.position);
                if (distance <= stealthTriggerDistance)
                {
                    Debug.Log($"Joueur proche de l'ennemi {enemy.name} à une distance de {distance:F2}m (seuil: {stealthTriggerDistance}m)");
                    return true;
                }
            }
        }

        return false;
    }

    void StartStealthTutorial()
    {
        hasTriggeredStealthTutorial = true;
        isShowingStealthTutorial = true;

        Time.timeScale = 0f;

        // Afficher le fond d'abord si activé
        if (stealthBackgroundPanel != null && showStealthBackground)
        {
            stealthBackgroundPanel.SetActive(true);
        }

        // Puis afficher le texte
        if (stealthWorldText != null)
        {
            stealthWorldText.SetActive(true);
        }

        Debug.Log("Tutoriel stealth commencé - Temps arrêté");
    }

    void CompleteStealthTutorial()
    {
        stealthTutorialCompleted = true;
        isShowingStealthTutorial = false;

        Time.timeScale = originalTimeScale;

        // Cacher le texte et le fond
        if (stealthWorldText != null)
        {
            stealthWorldText.SetActive(false);
        }

        if (stealthBackgroundPanel != null)
        {
            stealthBackgroundPanel.SetActive(false);
        }

        Debug.Log("Tutoriel stealth terminé - Temps repris");
    }

    void HandleCollectibleTutorial()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        bool lookingAtCollectible = false;

        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            if (hit.collider.CompareTag("Collectible"))
            {
                if (IsCollectibleRevealed(hit.collider.gameObject))
                {
                    lookingAtCollectible = true;

                    if (currentCollectibleText == null)
                    {
                        currentCollectibleText = CreateCollectibleText(collectibleText,
                                                                     hit.collider.transform.position);
                    }
                    else
                    {
                        Vector3 newPos = CalculateCollectibleTextPosition(hit.collider.transform.position);
                        currentCollectibleText.transform.position = newPos;
                        currentCollectibleText.SetActive(true);
                    }
                }
            }
        }

        if (!lookingAtCollectible && currentCollectibleText != null)
        {
            currentCollectibleText.SetActive(false);
        }

        if (handController != null && (handController.rightHeldObject != null || handController.leftHeldObject != null))
        {
            CompleteCollectibleTutorial();
        }
    }

    private bool IsCollectibleRevealed(GameObject collectibleObject)
    {
        PointCloudRevert pointCloudRevert = collectibleObject.GetComponent<PointCloudRevert>();

        if (pointCloudRevert == null)
        {
            pointCloudRevert = collectibleObject.GetComponentInChildren<PointCloudRevert>();
        }

        if (pointCloudRevert == null)
        {
            pointCloudRevert = collectibleObject.GetComponentInParent<PointCloudRevert>();
        }

        if (pointCloudRevert == null)
        {
            Debug.LogWarning($"PointCloudRevert script not found on collectible: {collectibleObject.name}. Assuming it's revealed.");
            return true;
        }

        try
        {
            var restingValueField = pointCloudRevert.GetType().GetField("restingValue",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var skinnedMeshRendererField = pointCloudRevert.GetType().GetField("skinnedMeshRenderer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var blendShapeIndexField = pointCloudRevert.GetType().GetField("blendShapeIndex",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (restingValueField != null && skinnedMeshRendererField != null && blendShapeIndexField != null)
            {
                float restingValue = (float)restingValueField.GetValue(pointCloudRevert);
                SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)skinnedMeshRendererField.GetValue(pointCloudRevert);
                int blendShapeIndex = (int)blendShapeIndexField.GetValue(pointCloudRevert);

                return CheckBlendshapeValue(skinnedMeshRenderer, blendShapeIndex, restingValue);
            }
            else
            {
                Debug.LogWarning($"Cannot access private fields in PointCloudRevert script for: {collectibleObject.name}. Fields found: restingValue={restingValueField != null}, skinnedMeshRenderer={skinnedMeshRendererField != null}, blendShapeIndex={blendShapeIndexField != null}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error accessing PointCloudRevert fields for {collectibleObject.name}: {e.Message}");
            return true;
        }
    }

    private bool CheckBlendshapeValue(SkinnedMeshRenderer skinnedMeshRenderer, int blendShapeIndex, float restingValue)
    {
        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
        {
            return true;
        }

        if (blendShapeIndex < 0 || blendShapeIndex >= skinnedMeshRenderer.sharedMesh.blendShapeCount)
        {
            Debug.LogWarning($"BlendShape index {blendShapeIndex} is out of range. Mesh has {skinnedMeshRenderer.sharedMesh.blendShapeCount} blendshapes.");
            return true;
        }

        float currentValue = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
        bool isRevealed = Mathf.Abs(currentValue - restingValue) > blendshapeRestingTolerance;

        if (Debug.isDebugBuild)
        {
            Debug.Log($"Collectible {skinnedMeshRenderer.gameObject.name}: BlendShape[{blendShapeIndex}] = {currentValue}, RestingValue = {restingValue}, IsRevealed = {isRevealed}");
        }

        return isRevealed;
    }

    void CompleteCollectibleTutorial()
    {
        collectibleTutorialCompleted = true;

        if (currentCollectibleText != null)
        {
            currentCollectibleText.SetActive(false); // Cacher au lieu de détruire
        }

        Debug.Log("Tutoriel collectible terminé");
    }

    void CompleteDepositTutorial()
    {
        depositTutorialCompleted = true;

        // Cacher tous les textes de dépôt
        foreach (GameObject depositText in depositTexts)
        {
            if (depositText != null)
            {
                depositText.SetActive(false);
            }
        }

        exit.GetComponent<Exit>().isCollectComplete = true; 
        

        Debug.Log("Tutoriel dépôt terminé - Tous les textes de dépôt cachés");
    }

    public void HideAllDepositTexts()
    {
        foreach (GameObject text in depositTexts)
        {
            if (text != null)
            {
                text.SetActive(false);
            }
        }
    }

    public void ShowAllDepositTexts()
    {
        if (!depositTutorialCompleted)
        {
            foreach (GameObject text in depositTexts)
            {
                if (text != null)
                {
                    text.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Force la mise à jour de la visibilité des textes de dépôt
    /// </summary>
    public void RefreshDepositTextsVisibility()
    {
        UpdateDepositTextsVisibility();
    }

    private void UpdateEchoTextColor()
    {
        if (echoWorldText != null)
        {
            if (tutorialCanvas != null)
            {
                TextMeshProUGUI textMesh = echoWorldText.GetComponent<TextMeshProUGUI>();
                if (textMesh != null) textMesh.color = echoTextColor;
            }
            else
            {
                TextMeshPro textMesh = echoWorldText.GetComponent<TextMeshPro>();
                if (textMesh != null) textMesh.color = echoTextColor;
            }
        }
    }

    private void UpdateDepositTextsColor()
    {
        foreach (GameObject textObj in depositTexts)
        {
            if (textObj != null)
            {
                TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
                if (textMesh != null)
                {
                    textMesh.color = depositTextColor;
                }
            }
        }
    }

    private void UpdateCollectibleTextColor()
    {
        if (currentCollectibleText != null)
        {
            TextMeshPro textMesh = currentCollectibleText.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.color = collectibleTextColor;
            }
        }
    }

    private void UpdateStealthTextColor()
    {
        if (stealthWorldText != null)
        {
            if (tutorialCanvas != null)
            {
                TextMeshProUGUI textMesh = stealthWorldText.GetComponent<TextMeshProUGUI>();
                if (textMesh != null) textMesh.color = stealthTextColor;
            }
            else
            {
                TextMeshPro textMesh = stealthWorldText.GetComponent<TextMeshPro>();
                if (textMesh != null) textMesh.color = stealthTextColor;
            }
        }
    }

    private void UpdateStealthBackgroundColor()
    {
        if (stealthBackgroundPanel != null)
        {
            Image backgroundImage = stealthBackgroundPanel.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = stealthBackgroundColor;
            }
        }
    }

    // Méthodes publiques pour changer les textes
    public void ChangeEchoText(string newText)
    {
        echoTutorialText = newText;
        if (echoWorldText != null)
        {
            if (tutorialCanvas != null)
            {
                TextMeshProUGUI textMesh = echoWorldText.GetComponent<TextMeshProUGUI>();
                if (textMesh != null) textMesh.text = newText;
            }
            else
            {
                TextMeshPro textMesh = echoWorldText.GetComponent<TextMeshPro>();
                if (textMesh != null) textMesh.text = newText;
            }
        }
    }

    public void ChangeDepositText(string newText)
    {
        depositText = newText;
        foreach (GameObject textObj in depositTexts)
        {
            if (textObj != null)
            {
                TextMeshPro textMesh = textObj.GetComponent<TextMeshPro>();
                if (textMesh != null)
                {
                    textMesh.text = newText;
                }
            }
        }
    }

    public void ChangeCollectibleText(string newText)
    {
        collectibleText = newText;
    }

    public void ChangeStealthText(string newText)
    {
        stealthTutorialText = newText;
        if (stealthWorldText != null)
        {
            if (tutorialCanvas != null)
            {
                TextMeshProUGUI textMesh = stealthWorldText.GetComponent<TextMeshProUGUI>();
                if (textMesh != null) textMesh.text = newText;
            }
            else
            {
                TextMeshPro textMesh = stealthWorldText.GetComponent<TextMeshPro>();
                if (textMesh != null) textMesh.text = newText;
            }
        }
    }

    // Méthodes publiques pour changer les couleurs
    public void ChangeEchoTextColor(Color newColor)
    {
        EchoTextColor = newColor;
    }

    public void ChangeDepositTextColor(Color newColor)
    {
        DepositTextColor = newColor;
    }

    public void ChangeCollectibleTextColor(Color newColor)
    {
        CollectibleTextColor = newColor;
    }

    public void ChangeStealthTextColor(Color newColor)
    {
        StealthTextColor = newColor;
    }

    public void ChangeStealthBackgroundColor(Color newColor)
    {
        StealthBackgroundColor = newColor;
    }

    public void ChangeStealthTriggerDistance(float newDistance)
    {
        stealthTriggerDistance = Mathf.Clamp(newDistance, 5f, 50f);
        Debug.Log($"Distance de déclenchement stealth changée à {stealthTriggerDistance}m");
    }

    /// <summary>
    /// Change la distance d'affichage des textes de dépôt
    /// </summary>
    /// <param name="newDistance">Nouvelle distance en mètres</param>
    public void ChangeDepositTextDisplayDistance(float newDistance)
    {
        depositTextDisplayDistance = Mathf.Clamp(newDistance, 5f, 50f);
        Debug.Log($"Distance d'affichage des textes de dépôt changée à {depositTextDisplayDistance}m");
    }

    /// <summary>
    /// Change l'intervalle de vérification des CollectibleCount
    /// </summary>
    /// <param name="newInterval">Nouvel intervalle en secondes</param>
    public void ChangeCollectibleCheckInterval(float newInterval)
    {
        collectibleCheckInterval = Mathf.Clamp(newInterval, 0.1f, 2f);
        Debug.Log($"Intervalle de vérification CollectibleCount changé à {collectibleCheckInterval}s");
    }

    /// <summary>
    /// Active/désactive l'affichage permanent des textes de dépôt
    /// </summary>
    /// <param name="alwaysShow">True pour toujours afficher, false pour afficher selon la distance</param>
    public void SetAlwaysShowDepositTexts(bool alwaysShow)
    {
        alwaysShowDepositTexts = alwaysShow;

        if (alwaysShow && !depositTutorialCompleted)
        {
            // Afficher tous les textes seulement si le tutoriel n'est pas terminé
            ShowAllDepositTexts();
        }
        else
        {
            // Mettre à jour selon la distance
            UpdateDepositTextsVisibility();
        }

        Debug.Log($"Affichage permanent des textes de dépôt: {(alwaysShow ? "ACTIVÉ" : "DÉSACTIVÉ")}");
    }

    /// <summary>
    /// Active/désactive le fond du texte stealth
    /// </summary>
    /// <param name="enabled">True pour activer le fond</param>
    public void SetStealthBackgroundEnabled(bool enabled)
    {
        showStealthBackground = enabled;
        if (stealthBackgroundPanel != null)
        {
            stealthBackgroundPanel.SetActive(enabled && isShowingStealthTutorial);
        }
    }

    /// <summary>
    /// Change le padding du fond stealth
    /// </summary>
    /// <param name="padding">Nouveau padding</param>
    public void ChangeStealthBackgroundPadding(float padding)
    {
        stealthBackgroundPadding = padding;
        if (stealthBackgroundPanel != null)
        {
            RectTransform bgRect = stealthBackgroundPanel.GetComponent<RectTransform>();
            if (bgRect != null)
            {
                bgRect.sizeDelta = new Vector2(1000 + padding * 2, 300 + padding * 2);
            }
        }
    }

    /// <summary>
    /// Remet tous les tutoriels à zéro (sauf l'écho)
    /// </summary>
    public void ResetAllTutorials()
    {
        stealthTutorialCompleted = false;
        collectibleTutorialCompleted = false;
        depositTutorialCompleted = false;
        hasTriggeredStealthTutorial = false;

        // Réinitialiser le suivi des CollectibleCount
        foreach (CollectibleCount collectibleCount in collectibleCountScripts)
        {
            if (collectibleCount != null)
            {
                int currentCount = GetCollectibleCountValue(collectibleCount);
                previousCollectibleCounts[collectibleCount] = currentCount;
            }
        }

        Debug.Log("Tous les tutoriels remis à zéro (sauf écho)");
    }

    /// <summary>
    /// Force la validation d'un tutoriel spécifique
    /// </summary>
    /// <param name="tutorialType">Type de tutoriel à valider</param>
    public void ForceCompleteTutorial(string tutorialType)
    {
        switch (tutorialType.ToLower())
        {
            case "stealth":
                CompleteStealthTutorial();
                break;
            case "collectible":
                CompleteCollectibleTutorial();
                break;
            case "deposit":
                CompleteDepositTutorial();
                break;
            default:
                Debug.LogWarning($"Type de tutoriel inconnu: {tutorialType}");
                break;
        }
    }

    public bool DebugCheckCollectible(GameObject collectible)
    {
        return IsCollectibleRevealed(collectible);
    }

    public void ForceStealthTutorial()
    {
        if (!hasTriggeredStealthTutorial)
        {
            StartStealthTutorial();
        }
    }

    public void ForceResumeTime()
    {
        Time.timeScale = originalTimeScale;
        Debug.Log("Temps forcé à reprendre");
    }

    public void DebugShowEnemyDistances()
    {
        if (playerMovement == null) return;

        Vector3 playerPosition = playerMovement.transform.position;
        Debug.Log("=== DISTANCES AVEC LES ENNEMIS ===");

        foreach (GameObject enemy in enemyObjects)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(playerPosition, enemy.transform.position);
                Debug.Log($"Ennemi {enemy.name}: {distance:F2}m (seuil: {stealthTriggerDistance}m)");
            }
        }
    }

    /// <summary>
    /// Debug pour afficher les distances avec tous les dépôts
    /// </summary>
    public void DebugShowDepositDistances()
    {
        if (playerMovement == null) return;

        Vector3 playerPosition = playerMovement.transform.position;
        Debug.Log("=== DISTANCES AVEC LES DÉPÔTS ===");

        for (int i = 0; i < depositObjects.Count; i++)
        {
            GameObject deposit = depositObjects[i];
            if (deposit != null)
            {
                float distance = Vector3.Distance(playerPosition, deposit.transform.position);
                bool isVisible = distance <= depositTextDisplayDistance;
                Debug.Log($"Dépôt {i} ({deposit.name}): {distance:F2}m (seuil: {depositTextDisplayDistance}m) - {(isVisible ? "VISIBLE" : "CACHÉ")}");
            }
        }
    }

    /// <summary>
    /// Affiche l'état de tous les tutoriels
    /// </summary>
    public void DebugShowTutorialStatus()
    {
        Debug.Log("=== ÉTAT DES TUTORIELS ===");
        Debug.Log($"Écho: {(echoTutorialCompleted ? "✅ TERMINÉ" : "❌ EN COURS")}");
        Debug.Log($"Stealth: {(stealthTutorialCompleted ? "✅ TERMINÉ" : "❌ EN ATTENTE")}");
        Debug.Log($"Collectibles: {(collectibleTutorialCompleted ? "✅ TERMINÉ" : "❌ EN ATTENTE")}");
        Debug.Log($"Dépôts: {(depositTutorialCompleted ? "✅ TERMINÉ" : "❌ EN ATTENTE")}");
        Debug.Log($"CollectibleCount scripts trouvés: {collectibleCountScripts.Count}");

        // Afficher les valeurs actuelles des CollectibleCount
        foreach (CollectibleCount cc in collectibleCountScripts)
        {
            if (cc != null)
            {
                int currentCount = GetCollectibleCountValue(cc);
                int previousCount = previousCollectibleCounts.ContainsKey(cc) ? previousCollectibleCounts[cc] : 0;
                Debug.Log($"  - {cc.gameObject.name}: Count actuel = {currentCount}, Précédent = {previousCount}");
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateEchoTextColor();
            UpdateDepositTextsColor();
            UpdateCollectibleTextColor();
            UpdateStealthTextColor();
            UpdateStealthBackgroundColor();
        }
    }
#endif
}