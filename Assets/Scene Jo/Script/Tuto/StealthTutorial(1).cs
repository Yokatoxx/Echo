using UnityEngine;
using System.Collections.Generic;

public class StealthTutorial : BaseTutorial
{
    private List<GameObject> enemyObjects = new List<GameObject>();
    private GameObject stealthText;
    private GameObject stealthBackground;
    private bool hasTriggered = false;
    private bool isShowingTutorial = false;
    private float originalTimeScale = 1f;

    protected override void OnInitialize()
    {
        // Trouver tous les ennemis
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        enemyObjects.AddRange(enemies);

        originalTimeScale = Time.timeScale;

        // Créer le texte stealth avec l'offset vertical
        Vector2 stealthOffset = new Vector2(0, tutorialData.stealthTextVerticalOffset * 50f); // Multiplier par 50 pour convertir en pixels UI
        stealthText = textManager.CreateScreenCenterText(
            "stealth",
            tutorialData.stealthTutorialText,
            tutorialData.stealthTextColor,
            tutorialData.stealthTextSize,
            stealthOffset
        );

        // Créer le fond si activé
        if (tutorialData.showStealthBackground)
        {
            Vector2 backgroundSize = new Vector2(
                1000 + tutorialData.stealthBackgroundPadding * 2,
                300 + tutorialData.stealthBackgroundPadding * 2
            );

            stealthBackground = textManager.CreateBackgroundPanel(
                "stealth",
                backgroundSize,
                tutorialData.stealthBackgroundColor,
                tutorialData.stealthBackgroundCornerRadius
            );
        }
    }

    protected override void OnStartTutorial()
    {
        // Le tutoriel stealth se déclenche automatiquement quand on s'approche d'un ennemi
    }

    protected override void OnUpdateTutorial()
    {
        if (!hasTriggered && IsPlayerNearEnemy())
        {
            TriggerStealthTutorial();
        }

        if (isShowingTutorial)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                CompleteTutorial();
            }
        }
    }

    private bool IsPlayerNearEnemy()
    {
        Vector3 playerPosition = GetPlayerPosition();

        foreach (GameObject enemy in enemyObjects)
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(playerPosition, enemy.transform.position);
                if (distance <= tutorialData.stealthTriggerDistance)
                {
                    Debug.Log($"Joueur proche de l'ennemi {enemy.name} à {distance:F2}m");
                    return true;
                }
            }
        }

        return false;
    }

    private void TriggerStealthTutorial()
    {
        hasTriggered = true;
        isShowingTutorial = true;

        // Arrêter le temps
        Time.timeScale = 0f;

        // Afficher le fond d'abord si activé
        if (stealthBackground != null)
        {
            textManager.ShowText("stealth_Background");
        }

        // Puis afficher le texte
        textManager.ShowText("stealth");

        Debug.Log("Tutoriel stealth déclenché - Temps arrêté");
    }

    protected override void OnCompleteTutorial()
    {
        isShowingTutorial = false;

        // Reprendre le temps
        Time.timeScale = originalTimeScale;

        // Cacher le texte et le fond
        textManager.HideText("stealth");
        if (stealthBackground != null)
        {
            textManager.HideText("stealth_Background");
        }
    }

    protected override void OnStopTutorial()
    {
        // S'assurer que le temps reprend si on arrête le tutoriel
        if (isShowingTutorial)
        {
            Time.timeScale = originalTimeScale;
        }

        textManager.HideText("stealth");
        if (stealthBackground != null)
        {
            textManager.HideText("stealth_Background");
        }

        isShowingTutorial = false;
    }

    // Méthode publique pour forcer le déclenchement (debug)
    public void ForceStealthTutorial()
    {
        if (!hasTriggered)
        {
            TriggerStealthTutorial();
        }
    }
}