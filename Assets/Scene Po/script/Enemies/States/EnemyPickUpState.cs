using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyPickUpState : EnemyState
{
    public GameObject[] collectiblesToPickUp;
    private GameObject closestCollectible;

    private Transform pickUpPos;

    private bool isPickedUp = false;

    private Transform originalPickUpPos;


    public EnemyPickUpState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        pickUpPos = enemy.pickUpPos;

    }

    public override void EnterState()
    {
        base.EnterState();

        isPickedUp = false;

        collectiblesToPickUp = enemy.GetComponent<TriggerCollect>().collectibles;
        closestCollectible = collectiblesToPickUp[FindClosestCollectible()];
        originalPickUpPos = closestCollectible.GetComponent<InPlace>().originalPosition;

        enemy.MoveEnemy(enemy.patrolSpeed, closestCollectible.transform);


    }

    public override void ExitState()
    {
        base.ExitState();

        enemy.IsWithinPickUpDistance = false;

    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.IsAggroed)
        {
            enemy.stateMachine.ChangeState(enemy.chaseState);
        }


    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!isPickedUp && enemy.agent.remainingDistance <= 1f)
        {

            isPickedUp = true;
            Debug.Log("Picking up");

            enemy.MoveEnemy(enemy.patrolSpeed, originalPickUpPos);

        }

        else if (isPickedUp)
        {
            closestCollectible.transform.position = pickUpPos.position;


            if (enemy.agent.remainingDistance <= 2f)
            {
                Debug.Log("Putting down");

                enemy.MoveEnemy(enemy.patrolSpeed, enemy.transform);

                closestCollectible.transform.position = originalPickUpPos.position;
                closestCollectible.GetComponent<InPlace>().isInPlace = true;
                enemy.stateMachine.ChangeState(enemy.iddleState);

            }
        }

    }

    private int FindClosestCollectible()
    {
        float closestDistance = Mathf.Infinity;
        int closestCollectibleId = 0;
        for (var i = 0; i < collectiblesToPickUp.Length; i++)
        {
            float dist = Vector3.Distance(enemy.transform.position, collectiblesToPickUp[i].transform.position);
            if (dist < closestDistance && !collectiblesToPickUp[i].GetComponent<InPlace>().isInPlace)
            {
                closestDistance = dist;
                closestCollectibleId = i;
            }
        }

        return closestCollectibleId;
    }

}
