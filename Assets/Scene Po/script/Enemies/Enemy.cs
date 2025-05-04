using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class Enemy : MonoBehaviour, IEnemyMoveable, ItriggerCheckable
{
    public Rigidbody rb { get; set; }
    public NavMeshAgent agent { get; private set; }

    //référence au PatrolRoute
    public GameObject patrolRouteObject;

    public GameObject scannerHit;

    public bool IsAggroed { get; set; }
    public bool IsWithinAttackDistance { get; set; }
    public bool IsWithinPickUpDistance { get; set; }
    public bool IsHitByScanner { get; set; }

    #region State Machine Variables
    public EnemyStateMachine stateMachine { get; set; }

    public EnemyIddleState iddleState { get; set; }
    public EnemyChaseState chaseState { get; set; }
    public EnemyAttackState attackState { get; set; }
    public EnemyPatrolState patrolState { get; set; }
    public EnemyPickUpState pickUpState { get; set; }
    public EnemyHitByScanner hitByScannerState { get; set; }


    #endregion

    private void Awake()
    {
        stateMachine = new EnemyStateMachine();

        iddleState = new EnemyIddleState(this, stateMachine);
        chaseState = new EnemyChaseState(this, stateMachine);
        attackState = new EnemyAttackState(this, stateMachine);
        patrolState = new EnemyPatrolState(this, stateMachine);
        pickUpState = new EnemyPickUpState(this, stateMachine);
        hitByScannerState = new EnemyHitByScanner(this, stateMachine);

        //passer la référence du PatrolRoute à l'état de patrouille
        if (patrolState is EnemyPatrolState enemyPatrolState)
        {
            enemyPatrolState.patrolRoute = patrolRouteObject;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();

        stateMachine.Initialize(patrolState);
    }

    private void Update()
    {
        stateMachine.CurrentEnemyState.FrameUpdate();

    }

    private void FixedUpdate()
    {
        stateMachine.CurrentEnemyState.PhysicsUpdate();
    }

    #region Idle Variables
    public float idleTime = 2f;
    public float idleTimer = 0f;
    #endregion

    #region Movement Functions  
    public void MoveEnemy(float speed, Transform p)
    {
        if (agent != null && p != null)
        {
            agent.speed = speed;
            agent.SetDestination(p.position);
        }
    }

    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    #endregion

    #region PickUp Variables
    public Transform pickUpPos;
    #endregion


    #region Distance Checks


    public void SetAggroStatus(bool isAggroed)
    {
        IsAggroed = isAggroed;
    }

    public void SetAttackDistanceBool(bool isWithinAttackDistance)
    {
        IsWithinAttackDistance = isWithinAttackDistance;
    }

    public void SetIsHitByScanner(bool isHitByScanner)
    {
        IsHitByScanner = isHitByScanner;
    }

    #endregion


    #region Animation Functions
    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        stateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
    }

    public void SetPickUpDistanceBool(bool isWithinPickUpDistance)
    {
        IsWithinPickUpDistance = isWithinPickUpDistance;
    }
    #endregion

    public enum AnimationTriggerType
    {
        PlayFoorStepSound,
        spotPlayer,
        chasingPlayer,
        attackPlayer,
    }


}