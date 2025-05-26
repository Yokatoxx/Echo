using UnityEngine;

public abstract class BaseTutorial : MonoBehaviour
{
    [Header("Base Tutorial Settings")]
    public bool isCompleted = false;
    public bool isActive = false;

    protected TutorialManager tutorialManager;
    protected TutorialTextManager textManager;
    protected TutorialData tutorialData;
    protected Camera mainCamera;

    public virtual void Initialize(TutorialManager manager, TutorialTextManager textMgr, TutorialData data, Camera camera)
    {
        tutorialManager = manager;
        textManager = textMgr;
        tutorialData = data;
        mainCamera = camera;
        
        OnInitialize();
    }

    public virtual void StartTutorial()
    {
        if (isCompleted) return;
        
        isActive = true;
        OnStartTutorial();
        Debug.Log($"{GetType().Name} started");
    }

    public virtual void StopTutorial()
    {
        isActive = false;
        OnStopTutorial();
        Debug.Log($"{GetType().Name} stopped");
    }

    public virtual void CompleteTutorial()
    {
        isCompleted = true;
        isActive = false;
        OnCompleteTutorial();
        Debug.Log($"{GetType().Name} completed");
        
        tutorialManager.OnTutorialCompleted(this);
    }

    public virtual void UpdateTutorial()
    {
        if (!isActive || isCompleted) return;
        
        OnUpdateTutorial();
    }

    // Méthodes abstraites à implémenter dans les classes dérivées
    protected virtual void OnInitialize() { }
    protected abstract void OnStartTutorial();
    protected virtual void OnStopTutorial() { }
    protected virtual void OnCompleteTutorial() { }
    protected abstract void OnUpdateTutorial();

    // Méthodes utilitaires
    protected bool IsPlayerLookingAt(string tag, float distance, out RaycastHit hit)
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        if (Physics.Raycast(ray, out hit, distance))
        {
            return hit.collider.CompareTag(tag);
        }
        return false;
    }

    protected Vector3 GetPlayerPosition()
    {
        return tutorialManager.playerMovement != null ? tutorialManager.playerMovement.transform.position : Vector3.zero;
    }
}