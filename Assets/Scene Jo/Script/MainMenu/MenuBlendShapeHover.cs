using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using FMODUnity; // Ajout de l'import FMOD

[RequireComponent(typeof(Collider))]
public class InteractiveMenuItem : MonoBehaviour
{
    public enum MenuActionType
    {
        None,
        ChangeScene,
        ToggleCanvasGroup,
        QuitGame
    }

    [Header("Visual Feedback (Click)")]
    [Tooltip("Le SkinnedMeshRenderer contenant les blend shapes.")]
    public SkinnedMeshRenderer skinnedMeshRenderer;
    [Tooltip("L'index du blend shape à modifier.")]
    public int blendShapeIndex = 0;
    [Tooltip("Le poids cible du blend shape lorsque l'élément est cliqué (0-100).")]
    [Range(0f, 100f)]
    public float clickWeight = 100.0f;
    [Tooltip("Le poids par défaut du blend shape au repos (0-100).")]
    [Range(0f, 100f)]
    public float normalWeight = 0.0f;
    [Tooltip("La vitesse de transition entre les poids.")]
    public float transitionSpeed = 10.0f;

    [Header("FMOD Audio")]
    [Tooltip("Événement FMOD à jouer lors du clic sur le bouton.")]
    public EventReference clickSoundEvent; // Changé de [EventRef] string à EventReference
    [Tooltip("Volume de l'événement audio (0-1).")]
    [Range(0f, 1f)]
    public float audioVolume = 1.0f;

    [Header("Fade Screen")]
    [Tooltip("Image UI pour le fondu en noir (doit être plein écran et noire).")]
    public Image fadeScreenImage;
    [Tooltip("Durée du fondu en noir (en secondes).")]
    public float fadeDuration = 1.0f;
    [Tooltip("Temps d'attente entre la fin du blend shape et le début du fondu.")]
    public float delayBeforeFade = 0.2f;
    [Tooltip("Tolérance pour déterminer quand le blend shape a atteint sa valeur cible.")]
    public float blendShapeThreshold = 0.5f;

    [Header("Click Action")]
    [Tooltip("L'action à exécuter lorsque l'utilisateur clique sur cet objet.")]
    public MenuActionType actionType = MenuActionType.None;

    [Tooltip("L'index de la scène à charger dans les Build Settings (si actionType est ChangeScene).")]
    public int sceneBuildIndex = -1;

    [Tooltip("Le CanvasGroup à afficher/cacher (si actionType est ToggleCanvasGroup).")]
    public CanvasGroup canvasGroupToToggle;

    private float _targetWeight;
    private bool _isActivated = false;
    private bool _actionInProgress = false;

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
            // blend shape invalide
            Debug.LogError($"Index de blend shape ({blendShapeIndex}) invalide ou mesh manquant pour {gameObject.name}. Le script InteractiveMenuItem sera désactivé.", this);
            this.enabled = false;
            return;
        }

        if (fadeScreenImage == null)
        {
            Debug.LogWarning($"Aucune image de fondu assignée à {gameObject.name}. Le fondu en noir ne fonctionnera pas.", this);
        }
        else
        {
            // Configurer l'image de fondu
            fadeScreenImage.color = new Color(0, 0, 0, 0); // Noir transparent au départ
        }

        _targetWeight = normalWeight;
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, normalWeight);
    }

    void Update()
    {
        if (!this.enabled || skinnedMeshRenderer == null) return;

        // Mise à jour du blend shape
        float currentWeight = skinnedMeshRenderer.GetBlendShapeWeight(blendShapeIndex);
        float newWeight = Mathf.Lerp(currentWeight, _targetWeight, Time.deltaTime * transitionSpeed);
        skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, newWeight);

        // Vérifier si le blend shape a atteint sa cible après un clic
        if (_isActivated && !_actionInProgress)
        {
            if (Mathf.Abs(currentWeight - clickWeight) < blendShapeThreshold)
            {
                _actionInProgress = true;
                StartCoroutine(ExecuteActionWithFade());
            }
        }
    }

    void OnMouseDown()
    {
        if (!this.enabled || _isActivated) return;

        // Jouer l'événement FMOD
        PlayClickSound();

        _targetWeight = clickWeight;
        _isActivated = true;
    }

    /// <summary>
    /// Joue l'événement FMOD assigné au clic
    /// </summary>
    private void PlayClickSound()
    {
        if (!clickSoundEvent.IsNull) // Changé de !string.IsNullOrEmpty à !clickSoundEvent.IsNull
        {
            try
            {
                // Créer et jouer l'événement FMOD
                FMOD.Studio.EventInstance soundInstance = RuntimeManager.CreateInstance(clickSoundEvent); // Plus besoin de passer une string

                // Définir le volume si spécifié
                if (audioVolume != 1.0f)
                {
                    soundInstance.setVolume(audioVolume);
                }

                // Définir la position 3D si l'objet a une position
                if (transform != null)
                {
                    soundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                }

                // Démarrer l'événement
                soundInstance.start();

                // Libérer l'instance après lecture (optionnel - permet un nettoyage automatique)
                soundInstance.release();

                Debug.Log($"[InteractiveMenuItem] Événement FMOD '{clickSoundEvent}' joué sur {gameObject.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InteractiveMenuItem] Erreur lors de la lecture de l'événement FMOD '{clickSoundEvent}' : {e.Message}", this);
            }
        }
        else
        {
            Debug.LogWarning($"[InteractiveMenuItem] Aucun événement FMOD assigné pour {gameObject.name}");
        }
    }

    IEnumerator ExecuteActionWithFade()
    {
        // Attendre un court instant
        if (delayBeforeFade > 0)
            yield return new WaitForSeconds(delayBeforeFade);

        // Faire le fondu au noir
        if (fadeScreenImage != null)
        {
            float elapsed = 0;
            Color startColor = fadeScreenImage.color;
            Color endColor = new Color(0, 0, 0, 1); // Noir opaque

            while (elapsed < fadeDuration)
            {
                fadeScreenImage.color = Color.Lerp(startColor, endColor, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fadeScreenImage.color = endColor; // Assurer que c'est complètement noir
        }

        // Exécuter l'action
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
                        ResetState(); // Réinitialiser l'état puisque l'action a échoué
                    }
                }
                else
                {
                    Debug.LogError($"[InteractiveMenuItem] Tentative de charger une scène mais sceneBuildIndex est négatif ({sceneBuildIndex}). Assignez un index valide dans l'inspecteur.", this);
                    ResetState(); // Réinitialiser l'état puisque l'action a échoué
                }
                break;

            case MenuActionType.ToggleCanvasGroup:
                if (canvasGroupToToggle != null)
                {
                    bool currentlyVisible = canvasGroupToToggle.alpha > 0.5f;
                    float targetAlpha = currentlyVisible ? 0f : 1f;
                    bool interactable = !currentlyVisible;
                    bool blocksRaycasts = !currentlyVisible;

                    canvasGroupToToggle.alpha = targetAlpha;
                    canvasGroupToToggle.interactable = interactable;
                    canvasGroupToToggle.blocksRaycasts = blocksRaycasts;

                    // Pour cette action, nous restaurons l'état après l'exécution
                    ResetState();

                    // Faire un fondu inverse pour revenir
                    if (fadeScreenImage != null)
                    {
                        StartCoroutine(FadeOut());
                    }
                }
                break;

            case MenuActionType.QuitGame:
                // Quitter le jeu
#if UNITY_EDITOR
                // En mode éditeur
                EditorApplication.isPlaying = false;
#else
                // En mode build
                Application.Quit();
#endif
                Debug.Log("[InteractiveMenuItem] Quitter le jeu");
                break;

            case MenuActionType.None:
                ResetState(); // Réinitialiser l'état si aucune action
                // Faire un fondu inverse pour revenir
                if (fadeScreenImage != null)
                {
                    StartCoroutine(FadeOut());
                }
                break;
        }
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0;
        Color startColor = fadeScreenImage.color;
        Color endColor = new Color(0, 0, 0, 0); // Transparent

        while (elapsed < fadeDuration)
        {
            fadeScreenImage.color = Color.Lerp(startColor, endColor, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeScreenImage.color = endColor; // Assurer que c'est complètement transparent
    }

    void ResetState()
    {
        _targetWeight = normalWeight;
        _isActivated = false;
        _actionInProgress = false;
    }

    void OnDisable()
    {
        // Réinitialisation silencieuse
        if (skinnedMeshRenderer != null)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(blendShapeIndex, normalWeight);
            _targetWeight = normalWeight;
            _isActivated = false;
            _actionInProgress = false;
        }
    }
}