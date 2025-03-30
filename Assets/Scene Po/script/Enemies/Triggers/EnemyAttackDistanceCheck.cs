using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackDistanceCheck : MonoBehaviour
{

    public GameObject playerTarget { get; set; }
    private Enemy enemy;

    private void Awake()
    {
        playerTarget = GameObject.FindGameObjectWithTag("Player");
        enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == playerTarget)
        {
            enemy.SetAttackDistanceBool(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == playerTarget)
        {
            enemy.SetAttackDistanceBool(false);
        }
    }
}
