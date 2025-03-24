using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrolState : EnemyState
{
    public bool invertPatrol = false;

    public GameObject patrolRoute;
    private PatrolRoute route;

    [SerializeField]
    private int currentPatrolIndex = 0;

    public float patrolSpeed = 5f;
    private Transform targetPos;

    public EnemyPatrolState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();

        route = patrolRoute.GetComponent<PatrolRoute>();


        currentPatrolIndex = GetClosestPatrolPointID(enemy.transform.position);
        targetPos = route.patrolPoints[currentPatrolIndex];

        enemy.MoveEnemy(patrolSpeed, targetPos);
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.agent.remainingDistance < 0.5f && !enemy.agent.pathPending)
        {
            if (invertPatrol)
            {
                currentPatrolIndex -= 1;
            }
            else
            {
                currentPatrolIndex += 1;
            }

            currentPatrolIndex = (int)Mathf.Repeat(currentPatrolIndex, route.patrolPoints.Length);
            targetPos = route.patrolPoints[currentPatrolIndex];
            enemy.MoveEnemy(patrolSpeed, targetPos);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    private int GetClosestPatrolPointID(Vector3 position)
    {
        float closestDistance = Mathf.Infinity;
        int closestPatrolPointID = 0;

        for (var i = 0; i < route.patrolPoints.Length; i++)
        {
            if (route.patrolPoints[i] == null) continue;

            float dist = Vector3.Distance(position, route.patrolPoints[i].position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestPatrolPointID = i;
            }
        }
        return closestPatrolPointID;
    }
}