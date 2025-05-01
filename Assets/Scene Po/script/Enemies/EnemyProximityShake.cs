using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class EnemyProximityShake : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Assignez le Transform du joueur ici dans l'inspecteur.")]
    public Transform playerTransform;
    private CinemachineImpulseSource impulseSource;

    [Header("Shake Parameters")]
    [Tooltip("Distance maximale à laquelle le tremblement commence.")]
    public float maxDistance = 15f;
    [Tooltip("Distance à laquelle le tremblement atteint son maximum.")]
    public float minDistance = 2f;
    [Tooltip("Force maximale de l'impulsion de tremblement à la distance minimale.")]
    public float maxShakeForce = 0.5f;
    [Tooltip("Fréquence de génération d'une impulsion (en secondes).")]
    public float shakeInterval = 0.25f;

    [Header("Movement Detection")]
    [Tooltip("Seuil de distance minimal pour considérer que l'ennemi bouge (en unités/frame). Ajustez si nécessaire.")]
    public float movementThreshold = 0.01f; // Très petite valeur pour détecter le moindre mouvement

    private float timeSinceLastShake = 0f;
    private Vector3 lastPosition; // Pour stocker la position de la frame précédente

    void Start()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        lastPosition = transform.position; // Initialiser la position précédente

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

        // --- Détection du Mouvement ---
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        bool isMoving = distanceMoved > movementThreshold;
        // Met à jour la position pour la prochaine frame
        lastPosition = transform.position;
        // --- Fin Détection Mouvement ---


        timeSinceLastShake += Time.deltaTime;

        if (timeSinceLastShake >= shakeInterval)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            // Vérifie si le joueur est dans la portée ET si l'ennemi bouge
            if (distance <= maxDistance && isMoving) // <--- Condition Modifiée
            {
                float intensityFactor = Mathf.Clamp01(1f - Mathf.InverseLerp(minDistance, maxDistance, distance));
                float currentForce = intensityFactor * maxShakeForce;

                if (currentForce > 0.01f)
                {
                    impulseSource.GenerateImpulseWithForce(currentForce);
                }

                timeSinceLastShake = 0f; // Réinitialise le timer seulement si une impulsion a été (potentiellement) générée
            }
            else if (distance > maxDistance || !isMoving) // Si hors de portée OU immobile
            {
                // Réinitialise aussi le timer si on est hors de portée ou immobile
                // pour éviter une impulsion dès qu'on re-rentre dans la zone ou qu'on se remet à bouger.
                timeSinceLastShake = 0f;
            }
            // Si on est dans la zone, immobile, mais que le timer n'était pas prêt, on ne fait rien et on laisse le timer continuer.
        }
    }
}