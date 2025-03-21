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
    [SerializeField] float collisionCooldown = 0.5f;
    [Range(0.1f, 1.0f)]
    [SerializeField] float echolocationCooldown = 0.2f;

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
        sprintInstance = RuntimeManager.CreateInstance(SprintEvent);

        if (player != null)
        {
            RuntimeManager.AttachInstanceToGameObject(sprintInstance, player.transform);
        }

        sprintInstance.setParameterByName(sprintVolumeParam, 0.0f);
    }
    

    void OnDestroy()
    {
        if (sprintInstance.isValid())
        {
            sprintInstance.release();
        }

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
        HandleSprinting();
        HandleEcholocation();
    }

    void HandleFootsteps()
    {
        time += Time.deltaTime;

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
        if (controller == null)
            return;

        bool isSprinting = controller.isWalking && Input.GetKey(KeyCode.LeftShift) && controller.stamina.CanSprint();

        if (isSprinting && !isSprintSoundPlaying)
        {
            sprintInstance.start();
            isSprintSoundPlaying = true;
            isFadingIn = true;
            sprintFadeTime = 0f;
        }
        else if (!isSprinting && isSprintSoundPlaying)
        {
            sprintInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            isSprintSoundPlaying = false;
            isFadingIn = false;
        }

        if (isSprintSoundPlaying && isFadingIn)
        {
            sprintFadeTime += Time.deltaTime;
            float fadeProgress = Mathf.Clamp01(sprintFadeTime / fadeInDuration);
            sprintInstance.setParameterByName(sprintVolumeParam, fadeProgress);

            if (fadeProgress >= 1.0f)
            {
                isFadingIn = false;
            }
        }
    }

    void HandleEcholocation()
    {
        if (Input.GetKeyDown(KeyCode.Space) && player != null)
        {
            PlayEcholocationSound(player.transform.position);
        }
    }

    void OnValidate()
    {
        sprintRate = Mathf.Min(sprintRate, walkRate);
    }
}
