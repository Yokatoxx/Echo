using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMOD.Studio;

public class AudioManager : MonoBehaviour
{
    [Header("Références de base")]
    [SerializeField] GameObject player;
    [SerializeField] PlayerMovement controller;

    [Header("Événements audio")]
    [Tooltip("Son des pas du joueur")]
    [SerializeField] EventReference FootstepEvent;
    [Tooltip("Son lors d'une collision avec le décor")]
    [SerializeField] EventReference DecorCollisionEvent;
    [Tooltip("Son lors de l'utilisation de l'écholocation")]
    [SerializeField] EventReference EcholocationEvent;
    [Tooltip("Musique de fond")]
    [SerializeField] EventReference BackgroundMusicEvent;

    [Header("Configuration des pas")]
    [Range(0.1f, 2.0f)]
    [SerializeField] float walkRate = 0.5f;

    [Header("Configuration surface des pas")]
    [Tooltip("Distance maximale pour détecter le sol")]
    [Range(0.1f, 5.0f)]
    [SerializeField] float groundCheckDistance = 1.5f;
    [Tooltip("Décalage vertical pour le point de départ du raycast")]
    [Range(0.0f, 1.0f)]
    [SerializeField] float raycastOffset = 0.1f;
    [Tooltip("Layers à ignorer lors de la détection du sol")]
    [SerializeField] LayerMask groundLayerMask = -1;
    [Tooltip("Nom du paramètre SurfaceType dans FMOD")]
    [SerializeField] string surfaceTypeParam = "SurfaceType";
    [Tooltip("Afficher les raycast dans la vue Scene")]
    [SerializeField] bool showDebugRaycast = true;

    [Header("Musique de fond")]
    [Tooltip("Volume de la musique de fond")]
    [Range(0.0f, 1.0f)]
    [SerializeField] float musicVolume = 0.7f;
    [Tooltip("Démarrer la musique automatiquement au lancement")]
    [SerializeField] bool playMusicOnStart = true;

    [Header("Cooldowns")]
    [Range(0.1f, 2.0f)]
    [SerializeField] float collisionCooldown = 0.5f;
    [Range(0.1f, 1.0f)]
    [SerializeField] float echolocationCooldown = 0.2f;

    [Header("--- FADE AUDIO SETTINGS ---")]
    [Tooltip("Durée du fade-in au démarrage de la scène")]
    [Range(0.5f, 10.0f)]
    [SerializeField] float fadeInDuration = 2f;
    [Tooltip("Durée du fade-out en sortie de scène")]
    [Range(0.5f, 5.0f)]
    [SerializeField] float fadeOutDuration = 1f;
    [Tooltip("Courbe d'animation pour le fade")]
    [SerializeField] AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Activer le fade-in automatique au démarrage")]
    [SerializeField] bool fadeInOnStart = true;
    [Tooltip("Activer le fade-out automatique en sortie")]
    [SerializeField] bool fadeOutOnSceneExit = true;

    public static AudioManager Instance { get; private set; }

    // Variables privées existantes
    private float time;
    private float lastCollisionTime = 0f;
    private float lastEcholocationTime = 0f;
    private int currentSurfaceType = 0; // Par défaut, WoodSurface (0)
    private FMOD.Studio.EventInstance backgroundMusicInstance;
    private bool isMusicPlaying = false;

    // Nouvelles variables pour le fade
    private Bus masterBus;
    private Bus musicBus;
    private Bus sfxBus;
    private Bus ambientBus;
    private Coroutine currentFadeCoroutine;
    private bool isAudioInitialized = false;
    private float masterTargetVolume = 1f;
    private float musicTargetVolume = 1f;
    private float sfxTargetVolume = 1f;
    private float ambientTargetVolume = 1f;

    // Enum pour les types de surface (correspond aux valeurs FMOD : 0, 1, 2)
    public enum SurfaceType
    {
        WoodSurface = 0,    // Correspond à la valeur FMOD 1
        TileSurface = 1,    // Correspond à la valeur FMOD 2
        CarpetSurface = 2   // Correspond à la valeur FMOD 3
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Si le gameObject n'est pas un objet racine, on le détache de son parent
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // NOUVEAU : Initialiser les bus audio pour le fade
        InitializeAudioBuses();

        // NOUVEAU : Si le fade est activé, commencer par couper le son puis faire le fade-in
        if (fadeInOnStart)
        {
            StartSceneFade();
        }

        // Initialisation existante
        if (playMusicOnStart && !BackgroundMusicEvent.IsNull)
        {
            PlayBackgroundMusic();
        }
    }

    void OnDestroy()
    {
        // NOUVEAU : Arrêter le fade si en cours
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        // Code existant
        // Arrêter et libérer la musique si elle existe
        StopBackgroundMusic();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region ===== NOUVELLES MÉTHODES FADE =====

    /// <summary>
    /// Initialise les bus FMOD pour le contrôle du fade
    /// </summary>
    private void InitializeAudioBuses()
    {
        try
        {
            // Récupérer les bus principaux FMOD
            masterBus = RuntimeManager.GetBus("bus:/");
            musicBus = RuntimeManager.GetBus("bus:/Music");
            sfxBus = RuntimeManager.GetBus("bus:/SFX");
            ambientBus = RuntimeManager.GetBus("bus:/Ambient");

            // Stocker les volumes cibles actuels
            masterBus.getVolume(out masterTargetVolume);

            // Essayer de récupérer les volumes des autres bus
            if (musicBus.isValid())
                musicBus.getVolume(out musicTargetVolume);
            if (sfxBus.isValid())
                sfxBus.getVolume(out sfxTargetVolume);
            if (ambientBus.isValid())
                ambientBus.getVolume(out ambientTargetVolume);

            isAudioInitialized = true;
            Debug.Log("AudioManager: Bus FMOD initialisés avec succès pour le fade");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Erreur lors de l'initialisation des bus audio: {e.Message}");
            // Fallback: utiliser seulement le bus master
            masterBus = RuntimeManager.GetBus("bus:/");
            masterBus.getVolume(out masterTargetVolume);
            isAudioInitialized = true;
        }
    }

    /// <summary>
    /// Démarre le fade-in audio au début de la scène (coupe tout puis fait un fondu progressif)
    /// </summary>
    public void StartSceneFade()
    {
        if (!isAudioInitialized) return;

        Debug.Log("AudioManager: Démarrage du fade-in de scène");

        // Couper immédiatement tous les sons
        SetAllVolumes(0f);

        // Démarrer le fade-in
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Fade-out audio avant de quitter la scène
    /// </summary>
    public void EndSceneFade()
    {
        if (!isAudioInitialized) return;

        Debug.Log("AudioManager: Démarrage du fade-out de scène");

        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(FadeOut());
    }

    /// <summary>
    /// Coroutine de fade-in
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;
            float curveValue = fadeCurve.Evaluate(progress);

            // Appliquer le fade à tous les bus
            SetAllVolumes(curveValue);

            yield return null;
        }

        // S'assurer que les volumes finaux sont corrects
        SetAllVolumes(1f);
        currentFadeCoroutine = null;
        Debug.Log("AudioManager: Fade-in terminé");
    }

    /// <summary>
    /// Coroutine de fade-out
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;
            float curveValue = fadeCurve.Evaluate(1f - progress);

            // Appliquer le fade à tous les bus
            SetAllVolumes(curveValue);

            yield return null;
        }

        // Couper complètement
        SetAllVolumes(0f);
        currentFadeCoroutine = null;
        Debug.Log("AudioManager: Fade-out terminé");
    }

    /// <summary>
    /// Applique un volume normalisé à tous les bus
    /// </summary>
    private void SetAllVolumes(float normalizedVolume)
    {
        // Appliquer au bus master
        masterBus.setVolume(masterTargetVolume * normalizedVolume);

        // Appliquer aux autres bus s'ils existent
        if (musicBus.isValid())
            musicBus.setVolume(musicTargetVolume * normalizedVolume);
        if (sfxBus.isValid())
            sfxBus.setVolume(sfxTargetVolume * normalizedVolume);
        if (ambientBus.isValid())
            ambientBus.setVolume(ambientTargetVolume * normalizedVolume);
    }

    /// <summary>
    /// Fade manuel vers un volume spécifique
    /// </summary>
    /// <param name="targetVolume">Volume cible (0-1)</param>
    /// <param name="duration">Durée du fade (si -1, utilise fadeInDuration)</param>
    public void FadeToVolume(float targetVolume, float duration = -1f)
    {
        if (duration < 0) duration = fadeInDuration;

        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        currentFadeCoroutine = StartCoroutine(FadeToVolumeCoroutine(targetVolume, duration));
    }

    /// <summary>
    /// Coroutine pour fade vers un volume spécifique
    /// </summary>
    private IEnumerator FadeToVolumeCoroutine(float targetVolume, float duration)
    {
        float startVolume;
        masterBus.getVolume(out startVolume);
        startVolume /= masterTargetVolume; // Normaliser

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float currentVolume = Mathf.Lerp(startVolume, targetVolume, fadeCurve.Evaluate(progress));

            SetAllVolumes(currentVolume);

            yield return null;
        }

        SetAllVolumes(targetVolume);
        currentFadeCoroutine = null;
    }

    /// <summary>
    /// Arrêt immédiat de tous les sons
    /// </summary>
    public void StopAllAudio()
    {
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        SetAllVolumes(0f);
        Debug.Log("AudioManager: Tous les sons coupés immédiatement");
    }

    /// <summary>
    /// Restauration immédiate de tous les sons
    /// </summary>
    public void RestoreAllAudio()
    {
        if (currentFadeCoroutine != null)
            StopCoroutine(currentFadeCoroutine);

        SetAllVolumes(1f);
        Debug.Log("AudioManager: Tous les sons restaurés immédiatement");
    }

    // Getters/Setters pour le fade
    public void SetFadeInDuration(float duration) => fadeInDuration = duration;
    public void SetFadeOutDuration(float duration) => fadeOutDuration = duration;
    public bool IsFading => currentFadeCoroutine != null;

    #endregion

    #region ===== TOUTES VOS MÉTHODES EXISTANTES (INCHANGÉES) =====

    public void PlayBackgroundMusic()
    {
        if (BackgroundMusicEvent.IsNull)
        {
            Debug.LogWarning("BackgroundMusicEvent non assigné dans l'AudioManager");
            return;
        }

        // Arrêter l'instance existante si nécessaire
        StopBackgroundMusic();

        // Créer une nouvelle instance de l'événement musical
        backgroundMusicInstance = RuntimeManager.CreateInstance(BackgroundMusicEvent);

        // Définir le volume
        backgroundMusicInstance.setVolume(musicVolume);

        // Démarrer la lecture
        backgroundMusicInstance.start();
        isMusicPlaying = true;
    }

    public void PauseBackgroundMusic()
    {
        if (isMusicPlaying && backgroundMusicInstance.isValid())
        {
            backgroundMusicInstance.setPaused(true);
            isMusicPlaying = false;
        }
    }

    public void ResumeBackgroundMusic()
    {
        if (!isMusicPlaying && backgroundMusicInstance.isValid())
        {
            backgroundMusicInstance.setPaused(false);
            isMusicPlaying = true;
        }
    }

    public void StopBackgroundMusic()
    {
        if (backgroundMusicInstance.isValid())
        {
            backgroundMusicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            backgroundMusicInstance.release();
            isMusicPlaying = false;
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (backgroundMusicInstance.isValid())
        {
            backgroundMusicInstance.setVolume(musicVolume);
        }
    }

    public bool IsMusicPlaying()
    {
        return isMusicPlaying;
    }

    public void PLayFootstep()
    {
        if (player != null)
        {
            // Détecter le type de surface avant de jouer le son
            DetectSurfaceType();

            // Créer une instance de l'événement pour pouvoir modifier le paramètre
            var footstepInstance = RuntimeManager.CreateInstance(FootstepEvent);

            // Attacher l'instance au joueur
            RuntimeManager.AttachInstanceToGameObject(footstepInstance, player);

            // Définir le paramètre SurfaceType (valeurs 0, 1, 2 pour FMOD)
            footstepInstance.setParameterByName(surfaceTypeParam, currentSurfaceType);

            // Debug pour vérifier la valeur envoyée
            if (showDebugRaycast)
            {
                Debug.Log($"FMOD Parameter '{surfaceTypeParam}' set to: {currentSurfaceType}");
            }

            // Jouer le son
            footstepInstance.start();

            // Libérer l'instance après lecture
            footstepInstance.release();
        }
    }

    /// <summary>
    /// Détecte le type de surface sous le joueur en utilisant un raycast
    /// </summary>
    private void DetectSurfaceType()
    {
        if (player == null) return;

        // Point de départ du raycast (légèrement au-dessus du joueur)
        Vector3 rayStart = player.transform.position + Vector3.up * raycastOffset;

        // Direction vers le bas
        Vector3 rayDirection = Vector3.down;

        // Effectuer le raycast
        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, groundCheckDistance, groundLayerMask))
        {
            // Vérifier le tag de l'objet touché
            string surfaceTag = hit.collider.tag;

            // Déterminer le type de surface selon le tag
            switch (surfaceTag)
            {
                case "WoodSurface":
                    currentSurfaceType = (int)SurfaceType.WoodSurface; // 0
                    break;
                case "TileSurface":
                    currentSurfaceType = (int)SurfaceType.TileSurface; // 1
                    break;
                case "CarpetSurface":
                    currentSurfaceType = (int)SurfaceType.CarpetSurface; // 2
                    break;
                default:
                    // Si aucun tag spécifique n'est trouvé, utiliser WoodSurface par défaut
                    currentSurfaceType = (int)SurfaceType.WoodSurface; // 0
                    break;
            }

            // Debug optionnel
            if (showDebugRaycast)
            {
                Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green, 0.1f);
                Debug.Log($"Surface détectée: {surfaceTag} -> SurfaceType Unity: {currentSurfaceType} (FMOD value: {currentSurfaceType})");
            }
        }
        else
        {
            // Aucune surface détectée, utiliser la surface par défaut
            currentSurfaceType = (int)SurfaceType.WoodSurface; // 0

            if (showDebugRaycast)
            {
                Debug.DrawRay(rayStart, rayDirection * groundCheckDistance, Color.red, 0.1f);
                Debug.Log("Aucune surface détectée, utilisation de WoodSurface par défaut (value: 0)");
            }
        }
    }

    /// <summary>
    /// Méthode publique pour obtenir le type de surface actuel
    /// </summary>
    public int GetCurrentSurfaceType()
    {
        return currentSurfaceType;
    }

    /// <summary>
    /// Méthode publique pour forcer la détection de surface
    /// </summary>
    public void ForceDetectSurface()
    {
        DetectSurfaceType();
    }

    public void PlayDecorCollisionSound(Vector3 collisionPoint)
    {
        if (Time.time - lastCollisionTime < collisionCooldown)
            return;

        RuntimeManager.PlayOneShot(DecorCollisionEvent, collisionPoint);
        lastCollisionTime = Time.time;
    }

    public void PlayEcholocationSound(Vector3 position)
    {
        if (Time.time - lastEcholocationTime < echolocationCooldown)
            return;

        RuntimeManager.PlayOneShot(EcholocationEvent, position);
        lastEcholocationTime = Time.time;
    }

    void Update()
    {
        HandleFootsteps();
        HandleEcholocation();
    }

    void HandleFootsteps()
    {
        time += Time.deltaTime;

        if (controller == null || !controller.isWalking)
            return;

        float currentRate = walkRate;

        if (time > currentRate)
        {
            PLayFootstep();
            time = 0;
        }
    }

    void HandleEcholocation()
    {
        // On remplace GetKeyDown par GetKeyUp pour correspondre à la logique de ChargeableEchoScanner
        if (Input.GetKeyUp(KeyCode.Space) && player != null)
        {
            PlayEcholocationSound(player.transform.position);
        }
    }

    void OnValidate()
    {
        // Validation des paramètres
    }

    // Méthode pour dessiner les gizmos dans l'éditeur
    void OnDrawGizmosSelected()
    {
        if (player != null && showDebugRaycast)
        {
            Vector3 rayStart = player.transform.position + Vector3.up * raycastOffset;
            Vector3 rayEnd = rayStart + Vector3.down * groundCheckDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rayStart, rayEnd);
            Gizmos.DrawWireSphere(rayEnd, 0.1f);
        }
    }

    #endregion
}