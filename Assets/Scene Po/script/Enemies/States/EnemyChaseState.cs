using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyChaseState : EnemyState
{
    private Transform playerTransform;

    public float timeTillExit = 3f;
    public float distanceToCountExit = 3f;
    private float exitTimer = 0f;

    public EnemyChaseState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public override void AnimationTriggerEvent(Enemy.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();


    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        enemy.MoveEnemy(enemy.chaseSpeed, playerTransform);


        if (enemy.agent.remainingDistance > distanceToCountExit)
        {
            exitTimer += Time.deltaTime;

            if (exitTimer >= timeTillExit)
            {
                enemy.stateMachine.ChangeState(enemy.iddleState);
            }
        }

        else if (enemy.agent.remainingDistance <= distanceToCountExit)
        {
            exitTimer = 0f;
        }

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

}
