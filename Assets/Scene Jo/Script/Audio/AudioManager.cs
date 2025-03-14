using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [Tooltip("Son pendant le sprint")]
    [SerializeField] EventReference SprintEvent;
    [Tooltip("Son lors de l'utilisation de l'écholocation")]
    [SerializeField] EventReference EcholocationEvent;

    [Header("Configuration des pas")]
    [Range(0.1f, 2.0f)]
    [SerializeField] float walkRate = 0.5f;
    [Range(0.1f, 1.0f)]
    [SerializeField] float sprintRate = 0.3f;

    [Header("Configuration du sprint")]
    [Tooltip("Nom du paramètre de volume dans FMOD")]
    [SerializeField] string sprintVolumeParam = "SprintVolume";
    [Range(0.1f, 5.0f)]
    [SerializeField] float fadeInDuration = 2.0f;

    [Header("Cooldowns")]
    [Range(0.1f, 2.0f)]
    [SerializeField] float collisionCooldown = 0.5f; // Temps minimum entre deux sons de collision
    [Range(0.1f, 1.0f)]
    [SerializeField] float echolocationCooldown = 0.2f; // Temps minimum entre deux sons d'écholocation

    // Instance singleton pour faciliter l'accès
    public static AudioManager Instance { get; private set; }

    // Variables privées
    private float time;
    private bool isSprintSoundPlaying = false;
    private FMOD.Studio.EventInstance sprintInstance;
    private float sprintFadeTime = 0f;
    private bool isFadingIn = false;
    private float lastCollisionTime = 0f;
    private float lastEcholocationTime = 0f;

    void Awake()
    {
        // Pattern Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeSprintSound();
    }

    void InitializeSprintSound()
    {
        // Créer l'instance de l'événement sprint mais ne pas le démarrer tout de suite
        sprintInstance = RuntimeManager.CreateInstance(SprintEvent);

        // On peut attacher l'instance à notre joueur pour qu'elle suive ses mouvements
        if (player != null)
        {
            RuntimeManager.AttachInstanceToGameObject(sprintInstance, player.transform);
        }

        // Initialiser le paramètre du volume à zéro
        sprintInstance.setParameterByName(sprintVolumeParam, 0.0f);
    }

    void OnDestroy()
    {
        // Libérer l'instance quand l'AudioManager est détruit
        if (sprintInstance.isValid())
        {
            sprintInstance.release();
        }

        // Si c'était l'instance singleton
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PLayFootstep()
    {
        if (player != null)
        {
            RuntimeManager.PlayOneShotAttached(FootstepEvent, player);
        }
    }

    // Méthode pour jouer le son de collision avec le décor
    public void PlayDecorCollisionSound(Vector3 collisionPoint)
    {
        // Évite de jouer des sons de collision trop rapprochés
        if (Time.time - lastCollisionTime < collisionCooldown)
            return;

        // Joue le son à la position de la collision
        RuntimeManager.PlayOneShot(DecorCollisionEvent, collisionPoint);
        lastCollisionTime = Time.time;
    }

    // Méthode pour jouer le son d'écholocation
    public void PlayEcholocationSound(Vector3 position)
    {
        // Évite de jouer des sons d'écholocation trop rapprochés
        if (Time.time - lastEcholocationTime < echolocationCooldown)
            return;

        // Joue le son à la position spécifiée
        RuntimeManager.PlayOneShot(EcholocationEvent, position);
        lastEcholocationTime = Time.time;
    }

    void Update()
    {
        HandleFootsteps();
        HandleSprinting();
        HandleEcholocation();
    }

    void HandleFootsteps()
    {
        time += Time.deltaTime;

        // Vérifier si le contrôleur est valide
        if (controller == null || !controller.isWalking)
            return;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && controller.stamina.CanSprint();
        float currentRate = isSprinting ? sprintRate : walkRate;

        if (time > currentRate)
        {
            PLayFootstep();
            time = 0;
        }
    }

    void HandleSprinting()
    {
        // Vérifier si le contrôleur est valide
        if (controller == null)
            return;

        bool isSprinting = controller.isWalking && Input.GetKey(KeyCode.LeftShift) && controller.stamina.CanSprint();

        // Si le joueur commence à sprinter et que le son n'est pas déjà en cours
        if (isSprinting && !isSprintSoundPlaying)
        {
            // Démarrer le son de sprint avec le volume à 0
            sprintInstance.start();
            isSprintSoundPlaying = true;
            isFadingIn = true;
            sprintFadeTime = 0f;
        }
        // Si le joueur arrête de sprinter et que le son est en cours
        else if (!isSprinting && isSprintSoundPlaying)
        {
            // Arrêter le son de sprint
            sprintInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            isSprintSoundPlaying = false;
            isFadingIn = false;
        }

        // Gérer le fondu d'entrée du son de sprint
        if (isSprintSoundPlaying && isFadingIn)
        {
            sprintFadeTime += Time.deltaTime;
            float fadeProgress = Mathf.Clamp01(sprintFadeTime / fadeInDuration);
            // Mettre à jour le paramètre de volume dans FMOD
            sprintInstance.setParameterByName(sprintVolumeParam, fadeProgress);

            // Une fois le fondu terminé, on arrête de l'ajuster
            if (fadeProgress >= 1.0f)
            {
                isFadingIn = false;
            }
        }
    }

    void HandleEcholocation()
    {
        // Écouter l'appui sur la touche espace pour l'écholocation
        if (Input.GetKeyDown(KeyCode.Space) && player != null)
        {
            PlayEcholocationSound(player.transform.position);
        }
    }

    // Pour le débogage dans l'éditeur
    void OnValidate()
    {
        // S'assurer que le taux de sprint est toujours plus faible que le taux de marche
        sprintRate = Mathf.Min(sprintRate, walkRate);
    }
}
