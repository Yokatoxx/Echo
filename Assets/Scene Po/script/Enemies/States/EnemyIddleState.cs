using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIddleState : EnemyState
{
    public float timeTillExit = 1.5f;
    private float exitTimer;

    public EnemyIddleState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();

        exitTimer = 0f;
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();


        if (enemy.IsAggroed)
        {
            enemy.stateMachine.ChangeState(enemy.chaseState);
        }
        if (enemy.IsHitByScanner)
        {
            enemy.stateMachine.ChangeState(enemy.hitByScannerState);
        }
        if (enemy.IsWithinAttackDistance)
        {
            enemy.stateMachine.ChangeState(enemy.attackState);
        }

        exitTimer += Time.deltaTime;

        if (exitTimer >= timeTillExit)
        {
            enemy.stateMachine.ChangeState(enemy.patrolState);
        }

        if (enemy.IsWithinPickUpDistance)
        {
            enemy.stateMachine.ChangeState(enemy.pickUpState);
        }

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }
}
