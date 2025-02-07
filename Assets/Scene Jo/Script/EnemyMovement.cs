using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [Header("Vision Settings")]
    [SerializeField] private float visionRange = 10f;
    [SerializeField] private float fieldOfView = 60f;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Detection Settings")]
    [SerializeField] private int numberOfRays = 4;

    [Header("Memory Settings")]
    [SerializeField] private float timeToForget = 2f;

    [Header("Material Settings")]
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material chaseMaterial;

    private NavMeshAgent navAgent;
    private int currentPatrolIndex = 0;
    private GameObject player;
    private Transform playerTransform;
    private PlayerMovement playerScript;
    private bool isChasing = false;

    private float visionRangeSqr;
    private float lostPlayerTimer = 0f;

    private Renderer enemyRenderer;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();

        if (patrolPoints.Length > 0)
        {
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }

        player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
            playerScript = player.GetComponent<PlayerMovement>();

            if (playerScript == null)
            {
                Debug.LogError("Le script PlayerMovement n'est pas trouvé sur le joueur.");
            }
        }

        visionRangeSqr = visionRange * visionRange;

        enemyRenderer = GetComponent<Renderer>();
        if (enemyRenderer != null)
        {
            if (normalMaterial != null)
            {
                enemyRenderer.material = normalMaterial;
            }
        }
    }

    void Update()
    {
        if (player != null && playerScript != null)
        {
            if (!playerScript.isHiding)
            {
                if (CanSeePlayer())
                {
                    ChasingPlayer();
                    lostPlayerTimer = 0f;
                }
                else if (isChasing)
                {
                    lostPlayerTimer += Time.deltaTime;
                    if (lostPlayerTimer >= timeToForget)
                    {
                        StopChasing();
                        Patrol();
                    }
                    else
                    {
                        navAgent.SetDestination(playerTransform.position);
                    }
                }
                else
                {
                    Patrol();
                }
            }
            else
            {
                StopChasing();
                Patrol();
            }
        }
        else
        {
            Patrol();
        }
    }

    private void ChasingPlayer()
    {
        if (!isChasing)
        {
            isChasing = true;
            if (enemyRenderer != null && chaseMaterial != null)
            {
                enemyRenderer.material = chaseMaterial;
            }
        }
        navAgent.SetDestination(playerTransform.position);
    }

    private void StopChasing()
    {
        if (isChasing)
        {
            isChasing = false;
            if (enemyRenderer != null && normalMaterial != null)
            {
                enemyRenderer.material = normalMaterial;
            }
        }
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayerSqr = directionToPlayer.sqrMagnitude;

        if (distanceToPlayerSqr <= visionRangeSqr)
        {
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle <= fieldOfView / 2f)
            {
                float angleStep = fieldOfView / (numberOfRays - 1);
                float startAngle = -fieldOfView / 2f;

                for (int i = 0; i < numberOfRays; i++)
                {
                    float currentAngle = startAngle + angleStep * i;
                    Vector3 rotatedDirection = Quaternion.Euler(0, currentAngle, 0) * transform.forward;

                    if (Physics.Raycast(transform.position, rotatedDirection.normalized, out RaycastHit hit, visionRange))
                    {
                        if (hit.collider.gameObject == player)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    void Patrol()
    {
        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void OnDrawGizmos()
    {
        if (numberOfRays <= 0)
            return;

        float angleStep = fieldOfView / (numberOfRays - 1);
        float startAngle = -fieldOfView / 2f;

        for (int i = 0; i < numberOfRays; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * transform.forward;

            if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, visionRange))
            {
                if (hit.collider.gameObject == player)
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = Color.red;
                }
            }
            else
            {
                Gizmos.color = Color.red;
            }

            Gizmos.DrawRay(transform.position, direction.normalized * visionRange);
        }

        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawFrustum(transform.position, fieldOfView, visionRange, 0, 1);
    }
}
 