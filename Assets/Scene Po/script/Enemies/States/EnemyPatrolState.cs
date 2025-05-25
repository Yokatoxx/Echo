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

        currentPatrolIndex = (int)Mathf.Repeat(currentPatrolIndex, route.patrolPoints.Length);
        targetPos = route.patrolPoints[currentPatrolIndex];
        enemy.MoveEnemy(enemy.patrolSpeed, targetPos);
    }


    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.IsAggroed)
        {
            enemy.stateMachine.ChangeState(enemy.chaseState);
        }

        if (enemy.IsWithinPickUpDistance)
        {
            enemy.stateMachine.ChangeState(enemy.pickUpState);
        }

        if (enemy.IsHitByScanner)
        {
            enemy.stateMachine.ChangeState(enemy.hitByScannerState);
        }
        if (enemy.IsWithinAttackDistance)
        {
            enemy.stateMachine.ChangeState(enemy.attackState);
        }


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

            enemy.stateMachine.ChangeState(enemy.iddleState);

        }
    }

}