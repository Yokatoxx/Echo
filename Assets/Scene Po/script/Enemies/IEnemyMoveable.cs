using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public interface IEnemyMoveable
{
    Rigidbody rb { get; set; }
    NavMeshAgent agent { get; }
    void MoveEnemy(float speed, Transform target);
}