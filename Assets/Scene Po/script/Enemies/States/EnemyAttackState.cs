using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EnemyAttackState : EnemyState
{
    private GameObject player;
    private GameObject spawnPoint;
    private int numberOfDeaths = 0;
    private int deathsBeforeReset;

    // Variables pour l'effet de fondu
    private Image fadeImage;
    private Canvas fadeCanvas;
    private bool isFading = false;

    // Variables pour l'affichage du compteur de morts
    private Text deathCounterText;
    private GameObject deathCounterPanel;

    // Variables pour la gestion des scènes (récupérées depuis Enemy)
    private string gameOverSceneName;
    private int gameOverSceneIndex;
    private bool useSceneName;

    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        spawnPoint = GameObject.FindGameObjectWithTag("SpawnPoint");
        player = GameObject.FindGameObjectWithTag("Player");
        numberOfDeaths = 0;
        deathsBeforeReset = enemy.NumberOfDeathsBeforeReset;

        // Récupérer les paramètres de scène depuis l'Enemy
        gameOverSceneName = enemy.gameOverSceneName;
        gameOverSceneIndex = enemy.gameOverSceneIndex;

        // Créer le canvas et l'image pour le fondu
        CreateFadeUI();
        // Créer l'UI pour le compteur de morts
        CreateDeathCounterUI();
    }

    private void CreateFadeUI()
    {
        // Créer un canvas pour l'effet de fondu
        GameObject canvasObj = new GameObject("FadeCanvas");
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // S'assurer qu'il est au-dessus de tout

        // Ajouter CanvasScaler pour la responsivité
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Créer l'image noire pour le fondu
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeCanvas.transform, false);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Noir transparent

        // Faire en sorte que l'image couvre tout l'écran
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Désactiver le canvas au début
        fadeCanvas.gameObject.SetActive(false);
    }

    private void CreateDeathCounterUI()
    {
        // Créer un panel pour le compteur de morts
        deathCounterPanel = new GameObject("DeathCounterPanel");
        deathCounterPanel.transform.SetParent(fadeCanvas.transform, false);

        // Ajouter une image de fond semi-transparente
        Image panelBg = deathCounterPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.7f); // Fond noir semi-transparent

        // Positionner le panel au centre de l'écran
        RectTransform panelRect = deathCounterPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 150);
        panelRect.anchoredPosition = Vector2.zero;

        // Créer le texte pour le compteur
        GameObject textObj = new GameObject("DeathCounterText");
        textObj.transform.SetParent(deathCounterPanel.transform, false);

        deathCounterText = textObj.AddComponent<Text>();
        deathCounterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        deathCounterText.fontSize = 24;
        deathCounterText.color = Color.white;
        deathCounterText.alignment = TextAnchor.MiddleCenter;
        deathCounterText.text = "";

        // Positionner le texte pour qu'il remplisse le panel
        RectTransform textRect = deathCounterText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Désactiver le panel au début
        deathCounterPanel.SetActive(false);
    }

    public override void EnterState()
    {
        Debug.Log("Attack Player - Starting fade effect");

        if (!isFading)
        {
            enemy.StartCoroutine(HandlePlayerDeath());
        }
    }

    private IEnumerator HandlePlayerDeath()
    {
        isFading = true;

        // Activer le canvas
        fadeCanvas.gameObject.SetActive(true);

        // Fondu vers le noir (fade in)
        yield return enemy.StartCoroutine(FadeToBlack(1.0f));

        // Attendre un petit moment dans le noir
        yield return new WaitForSeconds(0.5f);

        // Incrémenter le nombre de morts
        numberOfDeaths++;

        // Vérifier s'il reste des vies
        if (numberOfDeaths >= deathsBeforeReset)
        {
            Debug.Log("Game Over - Changing to game over scene");

            // NOUVEAU: Débloquer le curseur avant de changer de scène
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Charger la scène selon la configuration
            if (useSceneName && !string.IsNullOrEmpty(gameOverSceneName))
            {
                try
                {
                    SceneManager.LoadScene(gameOverSceneName);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Impossible de charger la scène par nom '{gameOverSceneName}': {e.Message}. Utilisation de l'index à la place.");
                    SceneManager.LoadScene(gameOverSceneIndex);
                }
            }
            else
            {
                SceneManager.LoadScene(gameOverSceneIndex);
            }
        }
        else
        {
            Debug.Log("Respawning player");

            // Téléporter le joueur au point de spawn
            player.transform.position = spawnPoint.transform.position;
            Physics.SyncTransforms();

            // Fondu depuis le noir (fade out)
            yield return enemy.StartCoroutine(FadeFromBlack(1.0f));

            // Afficher le compteur de morts restantes
            yield return enemy.StartCoroutine(ShowDeathCounter());

            // Désactiver le canvas
            fadeCanvas.gameObject.SetActive(false);

            // Retourner à l'état idle
            enemy.stateMachine.ChangeState(enemy.iddleState);
        }

        isFading = false;
    }

    private IEnumerator ShowDeathCounter()
    {
        int remainingLives = deathsBeforeReset - numberOfDeaths;

        // Mettre à jour le texte
        if (remainingLives == 1)
        {
            deathCounterText.text = $"ATTENTION !\nDernière chance restante !";
            deathCounterText.color = Color.red;
        }
        else
        {
            deathCounterText.text = $"Vous vous êtes fait attraper, chance restantes : {remainingLives}";
            deathCounterText.color = Color.white;
        }

        // Activer le panel
        deathCounterPanel.SetActive(true);

        // Effet d'apparition du panel (fade in)
        Image panelBg = deathCounterPanel.GetComponent<Image>();
        Text text = deathCounterText;

        float duration = 0.5f;
        float elapsedTime = 0;

        Color startBgColor = new Color(0, 0, 0, 0);
        Color endBgColor = new Color(0, 0, 0, 0.7f);
        Color startTextColor = new Color(text.color.r, text.color.g, text.color.b, 0);
        Color endTextColor = text.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);

            panelBg.color = Color.Lerp(startBgColor, endBgColor, alpha);
            text.color = Color.Lerp(startTextColor, endTextColor, alpha);

            yield return null;
        }

        panelBg.color = endBgColor;
        text.color = endTextColor;

        // Maintenir l'affichage pendant 3 secondes
        yield return new WaitForSeconds(3.0f);

        // Effet de disparition du panel (fade out)
        elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsedTime / duration);

            panelBg.color = Color.Lerp(startBgColor, endBgColor, alpha);
            text.color = Color.Lerp(startTextColor, endTextColor, alpha);

            yield return null;
        }

        panelBg.color = startBgColor;
        text.color = startTextColor;

        // Désactiver le panel
        deathCounterPanel.SetActive(false);
    }

    private IEnumerator FadeToBlack(float duration)
    {
        float elapsedTime = 0;
        Color startColor = new Color(0, 0, 0, 0);
        Color endColor = new Color(0, 0, 0, 1);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);
            fadeImage.color = Color.Lerp(startColor, endColor, alpha);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        float elapsedTime = 0;
        Color startColor = new Color(0, 0, 0, 1);
        Color endColor = new Color(0, 0, 0, 0);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / duration);
            fadeImage.color = Color.Lerp(startColor, endColor, alpha);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.SetAttackDistanceBool(false);

        // Si on sort de l'état pendant un fondu, s'assurer que l'UI est nettoyée
        if (fadeCanvas != null && fadeCanvas.gameObject.activeSelf)
        {
            fadeCanvas.gameObject.SetActive(false);
        }
    }

    // Méthode pour nettoyer les ressources quand l'ennemi est détruit
    public void Cleanup()
    {
        if (fadeCanvas != null)
        {
            Object.Destroy(fadeCanvas.gameObject);
        }
    }
}