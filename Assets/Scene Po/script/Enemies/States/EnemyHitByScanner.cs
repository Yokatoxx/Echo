using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitByScanner : EnemyState
{

    public float speed = 5f;
    public EnemyHitByScanner(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
    }
    public override void EnterState()
    {
        base.EnterState();

        enemy.MoveEnemy(speed, enemy.scannerHit.transform);

        enemy.IsHitByScanner = false;

    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.agent.remainingDistance <= 0.5f)
        {
            enemy.stateMachine.ChangeState(enemy.iddleState);
        }
        if (enemy.IsAggroed)
        {
            enemy.stateMachine.ChangeState(enemy.chaseState);
        }
        if (enemy.IsHitByScanner)
        {
            enemy.stateMachine.ChangeState(enemy.hitByScannerState);
        }
        if (enemy.IsWithinPickUpDistance)
        {
            enemy.stateMachine.ChangeState(enemy.pickUpState);
        }



    }
    public override void ExitState()
    {
        base.ExitState();

    }

}
