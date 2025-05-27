using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("🎮 Références principales")]
    public PlayerMovement playerMovement;
    public ChargeableEchoScanner echoScanner;
    public PlayerHandController handController;
    public Camera playerCamera;

    [Header("📊 Configuration")]
    public TutorialData tutorialData;
    
    [Header("🎯 UI Legacy (optionnel)")]
    public GameObject echoTutorialPanel;

    [Header("📝 Composants internes")]
    public TutorialTextManager textManager;

    // Modules de tutoriels
    private EchoTutorial echoTutorial;
    private StealthTutorial stealthTutorial;
    private CollectibleTutorial collectibleTutorial;
    private DepositTutorial depositTutorial;
    private ScannerTutorial scannerTutorial;

    // Liste de tous les tutoriels pour faciliter la gestion
    private List<BaseTutorial> allTutorials = new List<BaseTutorial>();

    // État du système
    private Camera mainCamera;
    private bool systemInitialized = false;

    #region Unity Lifecycle

    void Start()
    {
        InitializeSystem();
    }

    void Update()
    {
        if (!systemInitialized) return;

        UpdateActiveTutorials();
    }

    #endregion

    #region Initialization

    // Remplacez la méthode InitializeSystem par cette version :

    private void InitializeSystem()
    {
        try
        {
            // Validation des références
            if (!ValidateReferences())
            {
                Debug.LogError("TutorialManager: References validation failed!");
                return;
            }

            // Initialiser la caméra
            mainCamera = Camera.main;
            if (mainCamera == null) mainCamera = playerCamera;

            // Initialiser le gestionnaire de texte
            if (textManager == null)
            {
                textManager = gameObject.AddComponent<TutorialTextManager>();
            }

            textManager.Initialize(tutorialData);

            // Créer et initialiser tous les modules de tutoriels
            CreateTutorialModules();
            InitializeTutorialModules();

            // Démarrer avec le tutoriel d'écho
            StartEchoTutorial();

            systemInitialized = true;
            Debug.Log("Tutorial System initialized successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize tutorial system: {e.Message}");
            Debug.LogException(e);

            // Essayer un mode de secours
            try
            {
                Debug.Log("Attempting fallback initialization...");
                InitializeFallbackMode();
            }
            catch (System.Exception fallbackException)
            {
                Debug.LogError($"Fallback initialization also failed: {fallbackException.Message}");
            }
        }
    }

    private void InitializeFallbackMode()
    {
        // Mode de secours sans matériaux spéciaux
        Debug.LogWarning("Tutorial system running in fallback mode - some features may be limited");

        // Créer seulement les modules essentiels
        if (echoTutorial == null)
        {
            echoTutorial = gameObject.AddComponent<EchoTutorial>();
            echoTutorial.Initialize(this, null, tutorialData, mainCamera);
        }

        systemInitialized = true;
    }

    private bool ValidateReferences()
    {
        if (tutorialData == null)
        {
            Debug.LogError("TutorialManager: TutorialData is missing! Please assign a TutorialData ScriptableObject.");
            return false;
        }

        if (playerMovement == null)
        {
            Debug.LogWarning("TutorialManager: PlayerMovement reference is missing.");
        }

        if (handController == null)
        {
            Debug.LogWarning("TutorialManager: PlayerHandController reference is missing.");
        }

        return true;
    }

    private void CreateTutorialModules()
    {
        // Créer les modules de tutoriels
        echoTutorial = gameObject.AddComponent<EchoTutorial>();
        stealthTutorial = gameObject.AddComponent<StealthTutorial>();
        collectibleTutorial = gameObject.AddComponent<CollectibleTutorial>();
        depositTutorial = gameObject.AddComponent<DepositTutorial>();
        scannerTutorial = gameObject.AddComponent<ScannerTutorial>();

        // Ajouter à la liste pour faciliter la gestion
        allTutorials.Add(echoTutorial);
        allTutorials.Add(stealthTutorial);
        allTutorials.Add(collectibleTutorial);
        allTutorials.Add(depositTutorial);
        allTutorials.Add(scannerTutorial);
    }

    private void InitializeTutorialModules()
    {
        foreach (BaseTutorial tutorial in allTutorials)
        {
            tutorial.Initialize(this, textManager, tutorialData, mainCamera);
        }
    }

    #endregion

    #region Tutorial Flow Management

    private void StartEchoTutorial()
    {
        if (echoTutorial != null && !echoTutorial.isCompleted)
        {
            echoTutorial.StartTutorial();
        }

        // Cacher le panel legacy s'il existe
        if (echoTutorialPanel != null)
        {
            echoTutorialPanel.SetActive(false);
        }
    }

    private void UpdateActiveTutorials()
    {
        // Mettre à jour seulement les tutoriels actifs
        foreach (BaseTutorial tutorial in allTutorials)
        {
            if (tutorial.isActive)
            {
                tutorial.UpdateTutorial();
            }
        }
    }

    public void OnTutorialCompleted(BaseTutorial completedTutorial)
    {
        Debug.Log($"Tutorial completed: {completedTutorial.GetType().Name}");

        // Logique spéciale pour l'écho : démarrer les autres tutoriels
        if (completedTutorial == echoTutorial)
        {
            StartPostEchoTutorials();
        }

        // Vérifier si tous les tutoriels sont terminés
        CheckAllTutorialsCompleted();
    }

    private void StartPostEchoTutorials()
    {
        // Après l'écho, tous les autres tutoriels peuvent se faire dans n'importe quel ordre
        if (stealthTutorial != null && !stealthTutorial.isCompleted)
        {
            stealthTutorial.StartTutorial();
        }

        if (collectibleTutorial != null && !collectibleTutorial.isCompleted)
        {
            collectibleTutorial.StartTutorial();
        }

        if (depositTutorial != null && !depositTutorial.isCompleted)
        {
            depositTutorial.StartTutorial();
        }

        if (scannerTutorial != null && !scannerTutorial.isCompleted)
        {
            scannerTutorial.StartTutorial();
        }

        Debug.Log("Post-Echo tutorials started!");
    }

    private void CheckAllTutorialsCompleted()
    {
        bool allCompleted = true;
        foreach (BaseTutorial tutorial in allTutorials)
        {
            if (!tutorial.isCompleted)
            {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted)
        {
            OnAllTutorialsCompleted();
        }
    }

    private void OnAllTutorialsCompleted()
    {
        Debug.Log("🎉 Tous les tutoriels sont terminés ! Félicitations !");
        // Ici vous pouvez ajouter des actions spéciales quand tous les tutoriels sont finis
    }

    #endregion

    #region Public API

    /// <summary>
    /// Force la completion d'un tutoriel spécifique
    /// </summary>
    public void ForceCompleteTutorial(string tutorialType)
    {
        BaseTutorial targetTutorial = GetTutorialByName(tutorialType);
        if (targetTutorial != null)
        {
            targetTutorial.CompleteTutorial();
        }
        else
        {
            Debug.LogWarning($"Tutorial type '{tutorialType}' not found!");
        }
    }

    /// <summary>
    /// Remet tous les tutoriels à zéro (sauf l'écho)
    /// </summary>
    public void ResetAllTutorials()
    {
        foreach (BaseTutorial tutorial in allTutorials)
        {
            if (tutorial != echoTutorial) // Garder l'écho terminé
            {
                tutorial.isCompleted = false;
                tutorial.StopTutorial();
            }
        }

        // Redémarrer les tutoriels post-écho si l'écho est terminé
        if (echoTutorial.isCompleted)
        {
            StartPostEchoTutorials();
        }

        Debug.Log("Tous les tutoriels remis à zéro (sauf écho)");
    }

    /// <summary>
    /// Obtient l'état de tous les tutoriels
    /// </summary>
    public void DebugShowTutorialStatus()
    {
        Debug.Log("=== ÉTAT DES TUTORIELS ===");
        foreach (BaseTutorial tutorial in allTutorials)
        {
            string status = tutorial.isCompleted ? "✅ TERMINÉ" : (tutorial.isActive ? "🔄 EN COURS" : "⏸️ EN ATTENTE");
            Debug.Log($"{tutorial.GetType().Name}: {status}");
        }
    }

    /// <summary>
    /// Accès aux tutoriels spécifiques pour des fonctionnalités avancées
    /// </summary>
    public T GetTutorial<T>() where T : BaseTutorial
    {
        foreach (BaseTutorial tutorial in allTutorials)
        {
            if (tutorial is T)
            {
                return tutorial as T;
            }
        }
        return null;
    }

    #endregion

    #region Utility Methods

    private BaseTutorial GetTutorialByName(string name)
    {
        switch (name.ToLower())
        {
            case "echo": return echoTutorial;
            case "stealth": return stealthTutorial;
            case "collectible": return collectibleTutorial;
            case "deposit": return depositTutorial;
            case "scanner": return scannerTutorial;
            default: return null;
        }
    }

    #endregion

    #region Legacy Support (pour compatibilité avec l'ancien système)

    // Propriétés publiques pour accès legacy
    public bool EchoTutorialCompleted => echoTutorial?.isCompleted ?? false;
    public bool StealthTutorialCompleted => stealthTutorial?.isCompleted ?? false;
    public bool CollectibleTutorialCompleted => collectibleTutorial?.isCompleted ?? false;
    public bool DepositTutorialCompleted => depositTutorial?.isCompleted ?? false;
    public bool ScannerTutorialCompleted => scannerTutorial?.isCompleted ?? false;

    // Méthodes legacy pour notification externe
    public void NotifyCollectibleDeposited()
    {
        depositTutorial?.NotifyCollectibleDeposited();
    }

    public void NotifyCollectibleDeposited(CollectibleCount collectibleCount)
    {
        depositTutorial?.NotifyCollectibleDeposited(collectibleCount);
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug - Show All Tutorial Status")]
    public void DebugShowAllStatus()
    {
        DebugShowTutorialStatus();
    }

    [ContextMenu("Debug - Reset All Tutorials")]
    public void DebugResetAll()
    {
        ResetAllTutorials();
    }

    [ContextMenu("Debug - Complete All Tutorials")]
    public void DebugCompleteAll()
    {
        foreach (BaseTutorial tutorial in allTutorials)
        {
            if (!tutorial.isCompleted)
            {
                tutorial.CompleteTutorial();
            }
        }
    }

    #endregion
}