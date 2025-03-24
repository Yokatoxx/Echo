using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IEnemyMoveable
{
    public Rigidbody rb { get; set; }
    
    public NavMeshAgent agent;

    #region State Machine Variables
    public EnemyStateMachine stateMachine { get; set; }

    public EnemyIddleState iddleState { get; set; }
    public EnemyChaseState chaseState { get; set; }
    public EnemyAttackState attackState { get; set; }

    public EnemyPatrolState patrolState { get; set; }
    #endregion



    private void Awake()
    {
        stateMachine = new EnemyStateMachine();

        iddleState = new EnemyIddleState(this, stateMachine);
        chaseState = new EnemyChaseState(this, stateMachine);
        attackState = new EnemyAttackState(this, stateMachine);
        patrolState = new EnemyPatrolState(this, stateMachine);

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

    #region Patrol Variables



    #endregion

    #region Movement Functions  
    public void MoveEnemy(float speed, Transform p)
    {
        agent.speed = speed;
        agent.destination = p.position;

    }
    #endregion

    #region Animation Functions

    private void AnimationTriggerEvent(AnimationTriggerType triggerType)
    {
        stateMachine.CurrentEnemyState.AnimationTriggerEvent(triggerType);
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
