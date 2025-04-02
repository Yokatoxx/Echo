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
    private float speed = 5f;

    private bool isPickedUp = false;

    private Transform originalPickUpPos;

    public EnemyPickUpState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {

        collectiblesToPickUp = enemy.GetComponent<TriggerCollect>().collectibles;

        pickUpPos = enemy.pickUpPos;

    }

    public override void EnterState()
    {
        base.EnterState();

        closestCollectible = collectiblesToPickUp[FindClosestCollectible()];
        originalPickUpPos = closestCollectible.GetComponent<InPlace>().originalPosition;

        enemy.MoveEnemy(speed, closestCollectible.transform);
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (enemy.agent.remainingDistance <= 1f)
        {
            PickUp();
            Debug.Log("Picking up");

        }

        if (isPickedUp && enemy.agent.remainingDistance <= 1f)
        {
            PutDown();
            Debug.Log("Putting down");
        }
    }

    private int FindClosestCollectible()
    {
        float closestDistance = Mathf.Infinity;
        int closestCollectibleId = 0;
        for (var i = 0; i < collectiblesToPickUp.Length; i++)
        {
            float dist = Vector3.Distance(enemy.transform.position, collectiblesToPickUp[i].transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestCollectibleId = i;
            }
        }

        return closestCollectibleId;
    }

    private void PickUp()
    {

        closestCollectible.transform.position = pickUpPos.position;

        isPickedUp = true;

        enemy.MoveEnemy(speed, originalPickUpPos);
    }

    private void PutDown()
    {
        closestCollectible.transform.position = originalPickUpPos.position;
        isPickedUp = false;
        enemy.stateMachine.ChangeState(enemy.iddleState);

    }
}
