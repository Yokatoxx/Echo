using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.AI;

[System.Serializable]
public class EnemyStateSounds
{
    [Header("State FMOD Events")]
    [Tooltip("Son de patrouille (en boucle)")]
    public EventReference patrolEvent;
    [Tooltip("Son d'attaque (one-shot)")]
    public EventReference attackEvent;
    [Tooltip("Son quand touché par le scanner (one-shot)")]
    public EventReference hitByScannerEvent;

    [Header("Footsteps")]
    [Tooltip("Son des pas")]
    public EventReference footstepEvent;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float patrolVolume = 0.7f;
    [Range(0f, 1f)] public float attackVolume = 1f;
    [Range(0f, 1f)] public float hitByScannerVolume = 0.9f;
    [Range(0f, 1f)] public float footstepVolume = 0.8f;
}

public class EnemySoundManager : MonoBehaviour
{
    [Header("Sound Configuration")]
    public EnemyStateSounds stateSounds;

    [Header("Settings")]
    public bool enableSounds = true;
    public float fadeSpeed = 2f;

    [Header("HitByScanner Cooldown")]
    [Tooltip("Temps minimum entre deux sons HitByScanner (en secondes)")]
    [Range(0.1f, 5.0f)]
    public float hitByScannerCooldown = 1.0f;

    [Header("Footsteps Configuration")]
    [Tooltip("Vitesse des pas en patrouille")]
    [Range(0.1f, 2.0f)]
    public float patrolFootstepRate = 0.8f;
    [Tooltip("Vitesse des pas en poursuite")]
    [Range(0.1f, 2.0f)]
    public float chaseFootstepRate = 0.4f;
    [Tooltip("Seuil de vitesse minimum pour jouer les pas")]
    [Range(0.1f, 1.0f)]
    public float minimumSpeedForFootsteps = 0.5f;

    [Header("Surface Detection")]
    [Tooltip("Distance maximale pour détecter le sol")]
    [Range(0.1f, 5.0f)]
    public float groundCheckDistance = 1.5f;
    [Tooltip("Décalage vertical pour le point de départ du raycast")]
    [Range(0.0f, 1.0f)]
    public float raycastOffset = 0.1f;
    [Tooltip("Layers à ignorer lors de la détection du sol")]
    public LayerMask groundLayerMask = -1;
    [Tooltip("Nom du paramètre SurfaceType dans FMOD")]
    public string surfaceTypeParam = "SurfaceType";
    [Tooltip("Afficher les raycast dans la vue Scene")]
    public bool showDebugRaycast = true;

    [Header("FMOD Parameters (Optional)")]
    [SerializeField] private string intensityParameter = "Intensity";
    [SerializeField] private string stateParameter = "EnemyState";

    private Enemy enemy; // Support pour Enemy classique
    private EnemyType1 enemyType1; // Support pour EnemyType1
    private NavMeshAgent navAgent; // Pour détecter le mouvement
    private string currentState = "";
    private string previousState = "";

    // FMOD Event Instance pour patrol (seul event en boucle)
    private EventInstance patrolEventInstance;

    // Fade coroutines
    private Coroutine fadeCoroutine;

    // Footsteps variables
    private float footstepTimer = 0f;
    private int currentSurfaceType = 0; // Par défaut, WoodSurface (0)
    private Vector3 lastPosition;

    // HitByScanner cooldown management
    private float lastHitByScannerTime = -999f;
    private bool hasPlayedHitByScannerForCurrentState = false;

    // Enum pour les types de surface
    public enum SurfaceType
    {
        WoodSurface = 0,
        TileSurface = 1,
        CarpetSurface = 2
    }

    void Start()
    {
        // Essayer de trouver Enemy ou EnemyType1
        enemy = GetComponent<Enemy>();
        enemyType1 = GetComponent<EnemyType1>();
        navAgent = GetComponent<NavMeshAgent>();

        if (enemy == null && enemyType1 == null)
        {
            Debug.LogError("EnemySoundManager: No Enemy or EnemyType1 component found on " + gameObject.name);
            return;
        }

        if (navAgent == null)
        {
            Debug.LogError("EnemySoundManager: No NavMeshAgent component found on " + gameObject.name);
        }

        // Initialiser la position pour le calcul de mouvement
        lastPosition = transform.position;

        // Créer l'instance patrol si l'event existe
        if (!stateSounds.patrolEvent.IsNull)
        {
            patrolEventInstance = RuntimeManager.CreateInstance(stateSounds.patrolEvent);
        }
    }

    void Update()
    {
        if (!enableSounds) return;

        CheckStateChange();
        UpdateFMODParameters();
        HandleFootsteps();
    }

    private void CheckStateChange()
    {
        string newState = GetCurrentStateName();

        if (newState != currentState)
        {
            previousState = currentState;
            currentState = newState;

            // Reset le flag quand on change d'état
            if (currentState != "HitByScanner")
            {
                hasPlayedHitByScannerForCurrentState = false;
            }

            OnStateChanged(currentState, previousState);
        }
    }

    private string GetCurrentStateName()
    {
        EnemyStateMachine stateMachine = null;

        // Récupérer la state machine selon le type d'ennemi
        if (enemyType1 != null)
        {
            stateMachine = enemyType1.stateMachine;
        }
        else if (enemy != null)
        {
            stateMachine = enemy.stateMachine;
        }

        if (stateMachine?.CurrentEnemyState == null) return "Unknown";

        string stateName = stateMachine.CurrentEnemyState.GetType().Name;

        // Nettoyer le nom pour enlever "Enemy" et "State"
        stateName = stateName.Replace("Enemy", "").Replace("State", "");

        return stateName;
    }

    private void OnStateChanged(string newState, string oldState)
    {
        Debug.Log($"EnemySoundManager: State changed from {oldState} to {newState}");

        switch (newState)
        {
            case "Patrol":
                PlayPatrolSound();
                break;

            case "Attack":
                PlayAttackSound();
                break;

            case "HitByScanner":
                // Utiliser la nouvelle méthode avec cooldown
                TryPlayHitByScannerSound();
                break;

            default:
                // Arrêter le son de patrouille pour tous les autres états
                StopPatrolSound();
                break;
        }
    }

    #region Sound Methods

    private void PlayPatrolSound()
    {
        if (stateSounds.patrolEvent.IsNull || !patrolEventInstance.isValid()) return;

        // Arrêter le fade précédent s'il y en a un
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Démarrer le fade-in vers le son de patrouille
        fadeCoroutine = StartCoroutine(FadeInPatrolSound());
    }

    private void StopPatrolSound()
    {
        if (!patrolEventInstance.isValid()) return;

        // Arrêter le fade précédent s'il y en a un
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Démarrer le fade-out
        fadeCoroutine = StartCoroutine(FadeOutPatrolSound());
    }

    private void PlayAttackSound()
    {
        if (stateSounds.attackEvent.IsNull) return;

        EventInstance attackInstance = RuntimeManager.CreateInstance(stateSounds.attackEvent);
        attackInstance.setVolume(stateSounds.attackVolume);
        attackInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        attackInstance.start();
        attackInstance.release(); // Libère automatiquement après lecture

        Debug.Log("EnemySoundManager: Playing attack sound");
    }

    private void TryPlayHitByScannerSound()
    {
        // Vérifier si on peut jouer le son (cooldown + flag d'état)
        bool canPlay = CanPlayHitByScannerSound();

        if (canPlay)
        {
            PlayHitByScannerSound();
            lastHitByScannerTime = Time.time;
            hasPlayedHitByScannerForCurrentState = true;
        }
        else
        {
            Debug.Log($"EnemySoundManager: HitByScanner sound blocked (cooldown: {Time.time - lastHitByScannerTime:F2}s, hasPlayed: {hasPlayedHitByScannerForCurrentState})");
        }
    }

    private bool CanPlayHitByScannerSound()
    {
        // Vérifier le cooldown temporel
        bool cooldownPassed = (Time.time - lastHitByScannerTime) >= hitByScannerCooldown;

        // Vérifier si on n'a pas déjà joué le son pour cet état
        bool notPlayedYet = !hasPlayedHitByScannerForCurrentState;

        return cooldownPassed && notPlayedYet;
    }

    private void PlayHitByScannerSound()
    {
        if (stateSounds.hitByScannerEvent.IsNull) return;

        EventInstance hitInstance = RuntimeManager.CreateInstance(stateSounds.hitByScannerEvent);
        hitInstance.setVolume(stateSounds.hitByScannerVolume);
        hitInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        hitInstance.start();
        hitInstance.release(); // Libère automatiquement après lecture

        Debug.Log("EnemySoundManager: Playing hit by scanner sound");
    }

    #endregion

    #region Fade Coroutines

    private IEnumerator FadeInPatrolSound()
    {
        if (!patrolEventInstance.isValid()) yield break;

        // S'assurer que l'event est démarré
        PLAYBACK_STATE playbackState;
        patrolEventInstance.getPlaybackState(out playbackState);

        if (playbackState != PLAYBACK_STATE.PLAYING)
        {
            patrolEventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            patrolEventInstance.start();
        }

        // Fade in
        float volume = 0f;
        patrolEventInstance.setVolume(0f);

        while (volume < stateSounds.patrolVolume)
        {
            volume += fadeSpeed * Time.deltaTime;
            volume = Mathf.Min(stateSounds.patrolVolume, volume);
            patrolEventInstance.setVolume(volume);
            yield return null;
        }

        patrolEventInstance.setVolume(stateSounds.patrolVolume);
        fadeCoroutine = null;
        Debug.Log("EnemySoundManager: Patrol sound fade-in complete");
    }

    private IEnumerator FadeOutPatrolSound()
    {
        if (!patrolEventInstance.isValid()) yield break;

        float currentVolume;
        patrolEventInstance.getVolume(out currentVolume);

        while (currentVolume > 0.01f)
        {
            currentVolume -= fadeSpeed * Time.deltaTime;
            currentVolume = Mathf.Max(0f, currentVolume);
            patrolEventInstance.setVolume(currentVolume);
            yield return null;
        }

        patrolEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        fadeCoroutine = null;
        Debug.Log("EnemySoundManager: Patrol sound fade-out complete");
    }

    #endregion

    #region Footsteps System

    private void HandleFootsteps()
    {
        if (navAgent == null || stateSounds.footstepEvent.IsNull) return;

        // Vérifier si l'ennemi bouge
        bool isMoving = IsEnemyMoving();

        if (isMoving)
        {
            footstepTimer += Time.deltaTime;

            // Déterminer la vitesse des pas selon l'état
            float currentFootstepRate = GetFootstepRateForCurrentState();

            if (footstepTimer >= currentFootstepRate)
            {
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
        else
        {
            // Reset timer quand l'ennemi s'arrête
            footstepTimer = 0f;
        }
    }

    private bool IsEnemyMoving()
    {
        if (navAgent == null) return false;

        // Utiliser la vélocité du NavMeshAgent
        float agentSpeed = navAgent.velocity.magnitude;

        // Comparer la position (backup)
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        return agentSpeed > minimumSpeedForFootsteps || distanceMoved > 0.01f;
    }

    private float GetFootstepRateForCurrentState()
    {
        switch (currentState)
        {
            case "Patrol":
                return patrolFootstepRate;
            case "Chase":
                return chaseFootstepRate;
            default:
                return patrolFootstepRate;
        }
    }

    private void PlayFootstep()
    {
        // Détecter le type de surface avant de jouer le son
        DetectSurfaceType();

        // Créer une instance de l'événement
        var footstepInstance = RuntimeManager.CreateInstance(stateSounds.footstepEvent);

        // Attacher l'instance à l'ennemi
        RuntimeManager.AttachInstanceToGameObject(footstepInstance, gameObject);

        // Définir le paramètre SurfaceType
        footstepInstance.setParameterByName(surfaceTypeParam, currentSurfaceType);

        // Définir le volume
        footstepInstance.setVolume(stateSounds.footstepVolume);

        if (showDebugRaycast)
        {
            Debug.Log($"Enemy footstep: Surface type {currentSurfaceType}");
        }

        // Jouer le son
        footstepInstance.start();
        footstepInstance.release();
    }

    private void DetectSurfaceType()
    {
        Vector3 rayStart = transform.position + Vector3.up * raycastOffset;
        Vector3 rayDirection = Vector3.down;

        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, groundCheckDistance, groundLayerMask))
        {
            string surfaceTag = hit.collider.tag;

            switch (surfaceTag)
            {
                case "WoodSurface":
                    currentSurfaceType = (int)SurfaceType.WoodSurface;
                    break;
                case "TileSurface":
                    currentSurfaceType = (int)SurfaceType.TileSurface;
                    break;
                case "CarpetSurface":
                    currentSurfaceType = (int)SurfaceType.CarpetSurface;
                    break;
                default:
                    currentSurfaceType = (int)SurfaceType.WoodSurface;
                    break;
            }

            if (showDebugRaycast)
            {
                Debug.DrawRay(rayStart, rayDirection * hit.distance, Color.green, 0.1f);
            }
        }
        else
        {
            currentSurfaceType = (int)SurfaceType.WoodSurface;

            if (showDebugRaycast)
            {
                Debug.DrawRay(rayStart, rayDirection * groundCheckDistance, Color.red, 0.1f);
            }
        }
    }

    #endregion

    #region FMOD Parameters Update

    private void UpdateFMODParameters()
    {
        if (!patrolEventInstance.isValid()) return;

        // Mettre à jour la position 3D
        patrolEventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        // Mettre à jour les paramètres optionnels
        if (!string.IsNullOrEmpty(stateParameter))
        {
            float stateValue = GetStateParameterValue(currentState);
            patrolEventInstance.setParameterByName(stateParameter, stateValue);
        }

        if (!string.IsNullOrEmpty(intensityParameter))
        {
            float intensity = CalculateIntensity();
            patrolEventInstance.setParameterByName(intensityParameter, intensity);
        }
    }

    private float GetStateParameterValue(string state)
    {
        switch (state)
        {
            case "Patrol": return 1f;
            case "Attack": return 3f;
            case "HitByScanner": return 4f;
            default: return 0f;
        }
    }

    private float CalculateIntensity()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            return Mathf.Clamp01(10f / (distance + 1f));
        }
        return 1f;
    }

    #endregion

    #region Public Methods

    public void SetMasterVolume(float volume)
    {
        if (patrolEventInstance.isValid())
        {
            patrolEventInstance.setVolume(volume);
        }
    }

    public void EnableSounds(bool enable)
    {
        enableSounds = enable;
        if (!enable)
        {
            StopPatrolSound();
        }
    }

    public string GetCurrentState()
    {
        return currentState;
    }

    public bool IsMoving()
    {
        return IsEnemyMoving();
    }

    public float GetCurrentSpeed()
    {
        return navAgent != null ? navAgent.velocity.magnitude : 0f;
    }

    // Méthode publique pour forcer le reset du cooldown si nécessaire
    public void ResetHitByScannerCooldown()
    {
        hasPlayedHitByScannerForCurrentState = false;
        lastHitByScannerTime = -999f;
    }

    #endregion

    #region Debug

    void OnDrawGizmosSelected()
    {
        if (showDebugRaycast)
        {
            Vector3 rayStart = transform.position + Vector3.up * raycastOffset;
            Vector3 rayEnd = rayStart + Vector3.down * groundCheckDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rayStart, rayEnd);
            Gizmos.DrawWireSphere(rayEnd, 0.1f);
        }
    }

    #endregion

    void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // Arrêter et libérer l'instance patrol
        if (patrolEventInstance.isValid())
        {
            patrolEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            patrolEventInstance.release();
        }
    }
}