using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovingObject))]
public class PlayerProximityDetector : MonoBehaviour
{
    [Header("Paramètres de vitesse")]
    [SerializeField] private float normalSpeed = 3.5f;
    [SerializeField] private float runningSpeed = 7.0f;

    [Header("Paramètres de détection")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float speedChangeSmoothTime = 0.5f;
    [SerializeField] private LayerMask playerLayer;

    private SphereCollider detectionCollider;
    private MovingObject movingObject;
    private UnityEngine.AI.NavMeshAgent agent;
    private bool playerDetected = false;
    private float currentVelocity;
    private Transform playerTransform;

    private void Awake()
    {
        movingObject = GetComponent<MovingObject>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();


        GameObject detectionObject = new GameObject("PlayerDetector");
        detectionObject.transform.parent = transform;
        detectionObject.transform.localPosition = Vector3.zero;

        detectionCollider = detectionObject.AddComponent<SphereCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;

        PlayerDetector detector = detectionObject.AddComponent<PlayerDetector>();
        detector.Initialize(this);
    }

    private void Start()
    {
        if (agent != null)
        {
            agent.speed = normalSpeed;
        }
    }

    private void Update()
    {
        playerDetected = false;
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (colliders.Length > 0)
        {
            // Joueur détecté
            playerDetected = true;
            playerTransform = colliders[0].transform;
        }

        if (agent != null)
        {
            float targetSpeed = playerDetected ? runningSpeed : normalSpeed;
            agent.speed = Mathf.SmoothDamp(agent.speed, targetSpeed, ref currentVelocity, speedChangeSmoothTime);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    public void SetNormalSpeed(float speed)
    {
        normalSpeed = speed;
    }

    public void SetRunningSpeed(float speed)
    {
        runningSpeed = speed;
    }
}

public class PlayerDetector : MonoBehaviour
{
    private PlayerProximityDetector parent;

    public void Initialize(PlayerProximityDetector parentDetector)
    {
        parent = parentDetector;
    }
}
