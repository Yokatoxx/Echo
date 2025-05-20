using UnityEngine;
using Cinemachine;
using FMODUnity;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class EnemyProximityShake : MonoBehaviour
{
    [Header("Setup")]
    public Transform playerTransform;
    private CinemachineImpulseSource impulseSource;

    [Header("Shake Parameters")]
    public float maxDistance = 15f;
    public float minDistance = 2f;
    public float maxShakeForce = 0.5f;
    public float shakeInterval = 0.25f;

    [Header("Movement Detection")]
    public float movementThreshold = 0.01f;

    [Header("FMOD Audio")]
    [SerializeField] private EventReference heartbeatEvent;
    [SerializeField] private string distanceParameterName = "Distance";
    [SerializeField] private float audioUpdateInterval = 0.1f;

    private float timeSinceLastShake = 0f;
    private Vector3 lastPosition;
    private FMOD.Studio.EventInstance heartbeatInstance;
    private float timeSinceLastAudioUpdate = 0f;
    private bool isHeartbeatPlaying = false;

    void Start()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        lastPosition = transform.position;
        heartbeatInstance = RuntimeManager.CreateInstance(heartbeatEvent);

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                Debug.Log("EnemyProximityShake: Joueur trouvé automatiquement via le tag 'Player'.", this);
            }
            else
            {
                Debug.LogError("EnemyProximityShake: Player Transform non assigné et GameObject avec tag 'Player' introuvable. Désactivation du script.", this);
                enabled = false;
            }
        }

        if (minDistance >= maxDistance)
        {
            Debug.LogWarning("EnemyProximityShake: minDistance devrait être inférieur à maxDistance. Ajustement automatique.", this);
            minDistance = maxDistance - 0.1f;
            if (minDistance < 0) minDistance = 0;
        }
    }

    void Update()
    {
        if (playerTransform == null || impulseSource == null)
        {
            return;
        }

        // Détection du Mouvement
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        bool isMoving = distanceMoved > movementThreshold;
        lastPosition = transform.position;

        timeSinceLastShake += Time.deltaTime;
        timeSinceLastAudioUpdate += Time.deltaTime;

        // Mise à jour de la caméra (tremblement)
        if (timeSinceLastShake >= shakeInterval)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance <= maxDistance && isMoving)
            {
                float intensityFactor = Mathf.Clamp01(1f - Mathf.InverseLerp(minDistance, maxDistance, distance));
                float currentForce = intensityFactor * maxShakeForce;

                if (currentForce > 0.01f)
                {
                    impulseSource.GenerateImpulseWithForce(currentForce);
                }

                timeSinceLastShake = 0f;
            }
            else if (distance > maxDistance || !isMoving)
            {
                timeSinceLastShake = 0f;
            }
        }

        // Mise à jour du son (battement cardiaque)
        if (timeSinceLastAudioUpdate >= audioUpdateInterval)
        {
            UpdateHeartbeatSound();
            timeSinceLastAudioUpdate = 0f;
        }
    }

    private void UpdateHeartbeatSound()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= maxDistance)
        {
            // Calcul de l'intensité entre 0 (loin) et 1 (proche)
            float distanceParameter = Mathf.Clamp01(1f - Mathf.InverseLerp(minDistance, maxDistance, distance));

            if (!isHeartbeatPlaying)
            {
                heartbeatInstance.start();
                isHeartbeatPlaying = true;
            }

            heartbeatInstance.setParameterByName(distanceParameterName, distanceParameter);
            RuntimeManager.AttachInstanceToGameObject(heartbeatInstance, playerTransform);
        }
        else if (isHeartbeatPlaying)
        {
            heartbeatInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            isHeartbeatPlaying = false;
        }
    }

    void OnDestroy()
    {
        if (heartbeatInstance.isValid())
        {
            heartbeatInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            heartbeatInstance.release();
        }
    }
}
