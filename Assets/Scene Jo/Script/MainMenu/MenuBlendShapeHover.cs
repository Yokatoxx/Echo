using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Toujours utile d'avoir la référence, même si on n'utilise plus la fonction

[RequireComponent(typeof(Collider))]
public class InteractiveMenuItem : MonoBehaviour
{
    public enum MenuActionType
    {
        None,
        ChangeScene,
        ToggleCanvasGroup
    }

    [Header("Visual Feedback (Hover)")]
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public int blendShapeIndex = 0;
    [Range(0f, 100f)]
    public float hoverWeight = 100.0f;
    [Range(0f, 100f)]
    public float normalWeight = 0.0f;
    public float transitionSpeed = 10.0f;

    [Header("Click Action")]
    public MenuActionType actionType = MenuActionType.None;
    public int sceneBuildIndex = -1;
    public CanvasGroup canvasGroupToToggle;

    private float _targetWeight;
    private bool _isHovering = false;

    void Start()
    {
        // --- Initialisation (inchangée) ---
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                Debug.LogError($"SkinnedMeshRenderer non trouvé sur {gameObject.name}. Script désactivé.", this);
                this.enabled = false;
                return;
            }
        }
        if (skinnedMeshRenderer.sharedMesh == null || blendShapeIndex < 0 || blendShapeIndex >= skinnedMeshRenderer.sharedMesh.blendShapeCount)
        {
            Debug.LogError($"Index de blend shape ({blendShapeIndex}) invalide ou mesh manquant sur {gameObject.name}. Script désactivé.", this);
            this.enabled = false;
            return;
        }
        _targetWeight = normalWeight;
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, normalWeight);
        // --- Fin Initialisation ---

        // Validations initiales (inchangées)
        if (actionType == MenuActionType.ChangeScene && sceneBuildIndex < 0)
        {
            Debug.LogWarning($"[InteractiveMenuItem Setup] ActionType ChangeScene mais sceneBuildIndex négatif ({sceneBuildIndex}) sur {gameObject.name}.", this);
        }
        if (actionType == MenuActionType.ToggleCanvasGroup && canvasGroupToToggle == null)
        {
            Debug.LogWarning($"[InteractiveMenuItem Setup] ActionType ToggleCanvasGroup mais canvasGroupToToggle non assigné sur {gameObject.name}.", this);
        }
        Debug.Log($"[InteractiveMenuItem Setup] Initialisé sur {gameObject.name}. Action: {actionType}, Index Scène: {sceneBuildIndex}");
    }

    void Update()
    {
        // --- Gestion Blend Shape (inchangée) ---
        if (!this.enabled || skinnedMeshRenderer == null) return;
        float currentWeight = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
        float newWeight = Mathf.Lerp(currentWeight, _targetWeight, Time.deltaTime * transitionSpeed);
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, newWeight);
        // --- Fin Gestion Blend Shape ---
    }

    // --- Détection Survol (inchangée) ---
    void OnMouseEnter()
    {
        if (!this.enabled) return;
        _targetWeight = hoverWeight;
        _isHovering = true;
    }
    void OnMouseExit()
    {
        if (!this.enabled) return;
        _targetWeight = normalWeight;
        _isHovering = false;
    }
    // --- Fin Détection Survol ---

    void OnMouseDown()
    {
        Debug.Log($"[InteractiveMenuItem] OnMouseDown Entré sur {gameObject.name} (Actif: {this.enabled})");
        if (!this.enabled)
        {
            Debug.Log("[InteractiveMenuItem] Script désactivé, sortie.");
            return;
        }

        // ------------------------------------------------------------------
        // LA VÉRIFICATION IsPointerOverGameObject EST MAINTENANT SUPPRIMÉE !
        // ------------------------------------------------------------------
        // bool isOverUI = false;
        // if (EventSystem.current != null) {
        //     isOverUI = EventSystem.current.IsPointerOverGameObject();
        //     Debug.Log($"[InteractiveMenuItem] EventSystem.current.IsPointerOverGameObject() a retourné: {isOverUI}");
        // } else {
        //     Debug.LogWarning("[InteractiveMenuItem] EventSystem.current est null !");
        // }
        // if (isOverUI) {
        //     Debug.Log("[InteractiveMenuItem] Clic bloqué car IsPointerOverGameObject est true.");
        //     return;
        // }

        // On exécute directement l'action
        Debug.Log($"[InteractiveMenuItem] Exécution de l'action de type: {actionType} (Vérification UI désactivée)");

        switch (actionType)
        {
            case MenuActionType.ChangeScene:
                Debug.Log($"[InteractiveMenuItem] Cas ChangeScene. Index demandé: {sceneBuildIndex}");
                if (sceneBuildIndex >= 0)
                {
                    if (sceneBuildIndex < SceneManager.sceneCountInBuildSettings)
                    {
                        Debug.Log($"[InteractiveMenuItem] Chargement de la scène avec l'index de build : {sceneBuildIndex}");
                        SceneManager.LoadScene(sceneBuildIndex);
                    }
                    else
                    {
                        Debug.LogError($"[InteractiveMenuItem] Index {sceneBuildIndex} hors limites Build Settings (max: {SceneManager.sceneCountInBuildSettings - 1}).", this);
                    }
                }
                else
                {
                    Debug.LogError($"[InteractiveMenuItem] sceneBuildIndex est négatif ({sceneBuildIndex}).", this);
                }
                break;

            case MenuActionType.ToggleCanvasGroup:
                Debug.Log($"[InteractiveMenuItem] Cas ToggleCanvasGroup.");
                if (canvasGroupToToggle != null)
                {
                    bool currentlyVisible = canvasGroupToToggle.alpha > 0.5f;
                    float targetAlpha = currentlyVisible ? 0f : 1f;
                    bool interactable = !currentlyVisible;
                    bool blocksRaycasts = !currentlyVisible;
                    Debug.Log($"[InteractiveMenuItem] Toggle CanvasGroup '{canvasGroupToToggle.gameObject.name}'. Nouvel état : {(interactable ? "Visible" : "Caché")}");
                    canvasGroupToToggle.alpha = targetAlpha;
                    canvasGroupToToggle.interactable = interactable;
                    canvasGroupToToggle.blocksRaycasts = blocksRaycasts;
                }
                else
                {
                    Debug.LogError($"[InteractiveMenuItem] canvasGroupToToggle non assigné.", this);
                }
                break;

            case MenuActionType.None:
                Debug.Log("[InteractiveMenuItem] Cas None, aucune action.");
                break;
            default:
                Debug.LogWarning($"[InteractiveMenuItem] Type d'action inconnu: {actionType}");
                break;
        }
        Debug.Log($"[InteractiveMenuItem] Fin de OnMouseDown.");
    }

    void OnDisable()
    {
        // --- Réinitialisation (inchangée) ---
        if (skinnedMeshRenderer != null && _isHovering)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, normalWeight);
            _targetWeight = normalWeight;
            _isHovering = false;
        }
        // --- Fin Réinitialisation ---
    }
}