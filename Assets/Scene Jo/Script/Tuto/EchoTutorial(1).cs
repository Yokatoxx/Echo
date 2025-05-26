using UnityEngine;

public class EchoTutorial : BaseTutorial
{
    private GameObject echoText;
    private bool isShowingText = false;

    protected override void OnInitialize()
    {
        // Créer le texte d'écho centré à l'écran
        echoText = textManager.CreateScreenCenterText(
            "echo", 
            tutorialData.echoTutorialText, 
            tutorialData.echoTextColor, 
            tutorialData.echoTextSize
        );
    }

    protected override void OnStartTutorial()
    {
        // Désactiver le mouvement du joueur
        if (tutorialManager.playerMovement != null)
        {
            tutorialManager.playerMovement.DisableMovement();
        }

        // Afficher le texte d'écho
        textManager.ShowText("echo");
        isShowingText = true;
    }

    protected override void OnUpdateTutorial()
    {
        // Gérer l'appui sur ESPACE
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isShowingText)
            {
                textManager.HideText("echo");
                isShowingText = false;
            }
        }

        // Valider le tutoriel quand l'utilisateur relâche ESPACE
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (!isShowingText)
            {
                CompleteTutorial();
            }
        }
    }

    protected override void OnCompleteTutorial()
    {
        // Réactiver le mouvement du joueur
        if (tutorialManager.playerMovement != null)
        {
            tutorialManager.playerMovement.EnableMovement();
        }

        textManager.HideText("echo");
    }

    protected override void OnStopTutorial()
    {
        textManager.HideText("echo");
        isShowingText = false;
    }
}