using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackDistanceCheck : MonoBehaviour
{

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player is in attack distance");
            enemy.SetAttackDistanceBool(true);
        }

        if (other.gameObject.CompareTag("RepeatSound"))
        {
            other.gameObject.GetComponent<SpawnScannerObject>().isOn = false;
            Debug.Log("Scanner is off");
        }
    }



}
