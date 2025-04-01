using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
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
            enemy.SetAggroStatus(true);
        }

        if (other.gameObject.CompareTag("Colletible") && !other.gameObject.GetComponent<InPlace>().isInPlace)
        {
            
            enemy.SetPickUpDistanceBool(true);
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == playerTarget)
        {
            enemy.SetAggroStatus(false);
        }

        if (other.gameObject.CompareTag("Colletible"))
        {
            enemy.SetPickUpDistanceBool(false);
        }
    }

}
