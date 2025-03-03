using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MovingObject : MonoBehaviour
{
    [SerializeField] private float minWanderTime = 2f;
    [SerializeField] private float maxWanderTime = 5f;
    [SerializeField] private float destinationThreshold = 1.0f;
    [SerializeField] private float maxNavMeshDistance = 50f;

    private NavMeshAgent agent;
    private bool canMove = true;
    private Vector3 currentDestination;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            return;
        }

        StartCoroutine(Wander());
        SetRandomDestination();
    }

    void Update()
    {
        if (canMove && agent != null && !agent.pathPending)
        {
            if (agent.remainingDistance <= destinationThreshold)
            {
                SetRandomDestination();
            }
        }
    }

    IEnumerator Wander()
    {
        while (true)
        {
            if (canMove && (currentDestination == Vector3.zero || agent.velocity.magnitude < 0.1f))
            {
                SetRandomDestination();
            }

            float waitTime = Random.Range(minWanderTime, maxWanderTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void SetRandomDestination()
    {
        bool found = false;
        NavMeshHit hit;

        for (int i = 0; i < 15; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-maxNavMeshDistance, maxNavMeshDistance),
                0,
                Random.Range(-maxNavMeshDistance, maxNavMeshDistance)
            );

            if (NavMesh.SamplePosition(randomPos, out hit, maxNavMeshDistance, NavMesh.AllAreas))
            {
                currentDestination = hit.position;
                agent.SetDestination(hit.position);
                found = true;
                break;
            }
        }

        if (!found)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 fallbackDirection = Random.insideUnitSphere * 20f;
                fallbackDirection += transform.position;

                if (NavMesh.SamplePosition(fallbackDirection, out hit, 20f, NavMesh.AllAreas))
                {
                    currentDestination = hit.position;
                    agent.SetDestination(hit.position);
                    break;
                }
            }
        }
    }

    public void StopMovement()
    {
        canMove = false;
        if (agent != null)
            agent.isStopped = true;
    }

    public void ResumeMovement()
    {
        canMove = true;
        if (agent != null)
        {
            agent.isStopped = false;
            SetRandomDestination();
        }
    }
}
