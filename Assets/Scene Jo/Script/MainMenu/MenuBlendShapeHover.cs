using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Gardé au cas où

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
    [Tooltip("Le SkinnedMeshRenderer contenant les blend shapes.")]
    public SkinnedMeshRenderer skinnedMeshRenderer;
    [Tooltip("L'index du blend shape à modifier.")]
    public int blendShapeIndex = 0;
    [Tooltip("Le poids cible du blend shape lorsque la souris est dessus (0-100).")]
    [Range(0f, 100f)]
    public float hoverWeight = 100.0f;
    [Tooltip("Le poids par défaut du blend shape lorsque la souris n'est pas dessus (0-100).")]
    [Range(0f, 100f)]
    public float normalWeight = 0.0f;
    [Tooltip("La vitesse de transition entre les poids.")]
    public float transitionSpeed = 10.0f;

    [Header("Click Action")]
    [Tooltip("L'action à exécuter lorsque l'utilisateur clique sur cet objet.")]
    public MenuActionType actionType = MenuActionType.None;

    [Tooltip("L'index de la scène à charger dans les Build Settings (si actionType est ChangeScene).")]
    public int sceneBuildIndex = -1;

    [Tooltip("Le CanvasGroup à afficher/cacher (si actionType est ToggleCanvasGroup).")]
    public CanvasGroup canvasGroupToToggle;

    private float _targetWeight;
    private bool _isHovering = false;

    void Start()
    {
        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer == null)
            {
                Debug.LogError($"SkinnedMeshRenderer non trouvé sur {gameObject.name}. Le script InteractiveMenuItem sera désactivé.", this);
                this.enabled = false;
                return;
            }
        }

        if (skinnedMeshRenderer.sharedMesh == null || blendShapeIndex < 0 || blendShapeIndex >= skinnedMeshRenderer.sharedMesh.blendShapeCount)
        {
            //  blend shape invalide
            Debug.LogError($"Index de blend shape ({blendShapeIndex}) invalide ou mesh manquant pour {gameObject.name}. Le script InteractiveMenuItem sera désactivé.", this);
            this.enabled = false;
            return;
        }

        _targetWeight = normalWeight;
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, normalWeight);
    }

    void Update()
    {
        if (!this.enabled || skinnedMeshRenderer == null) return;

        float currentWeight = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
        float newWeight = Mathf.Lerp(currentWeight, _targetWeight, Time.deltaTime * transitionSpeed);
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, newWeight);
    }

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

    void OnMouseDown()
    {
        if (!this.enabled)
        {
            return;
        }

        switch (actionType)
        {
            case MenuActionType.ChangeScene:
                if (sceneBuildIndex >= 0)
                {
                    if (sceneBuildIndex < SceneManager.sceneCountInBuildSettings)
                    {
                        SceneManager.LoadScene(sceneBuildIndex);
                    }
                    else
                    {
                        Debug.LogError($"[InteractiveMenuItem] L'index de scène {sceneBuildIndex} est hors limites pour les Build Settings (Nombre de scènes dans le build: {SceneManager.sceneCountInBuildSettings}). Vérifiez File > Build Settings.", this);
                    }
                }
                else
                {
                    Debug.LogError($"[InteractiveMenuItem] Tentative de charger une scène mais sceneBuildIndex est négatif ({sceneBuildIndex}). Assignez un index valide dans l'inspecteur.", this);
                }
                break;

            case MenuActionType.ToggleCanvasGroup:
                if (canvasGroupToToggle != null)
                {
                    bool currentlyVisible = canvasGroupToToggle.alpha > 0.5f;
                    float targetAlpha = currentlyVisible ? 0f : 1f;
                    bool interactable = !currentlyVisible;
                    bool blocksRaycasts = !currentlyVisible;
                    // Action principale - Pas besoin de log ici en temps normal
                    canvasGroupToToggle.alpha = targetAlpha;
                    canvasGroupToToggle.interactable = interactable;
                    canvasGroupToToggle.blocksRaycasts = blocksRaycasts;
                }
                break;

            case MenuActionType.None:
                break;
        }
    }

    void OnDisable()
    {
        // Réinitialisation silencieuse
        if (skinnedMeshRenderer != null && _isHovering)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, normalWeight);
            _targetWeight = normalWeight;
            _isHovering = false;
        }
    }
}