using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    
    public GameObject playerTarget { get; set; }
    private Enemy enemy;
    public GameObject scannerTarget { get; set; }

    private void Awake()
    {
        playerTarget = GameObject.FindGameObjectWithTag("Player");

        enemy = GetComponentInParent<Enemy>();

    }

    private void OnTriggerEnter(Collider other)
    {
        

        if (other.gameObject.CompareTag("Collectible") && other.gameObject.GetComponent<InPlace>().isInPlace)
        {
            enemy.SetPickUpDistanceBool(false);
        }


        if (other.gameObject.CompareTag("Collectible") && !other.gameObject.GetComponent<InPlace>().isInPlace)
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

    }

    private void OnTriggerStay(Collider other)
    {

        if (other.gameObject.CompareTag("Collectible") && other.gameObject.GetComponent<InPlace>().isInPlace)
        {
            enemy.SetPickUpDistanceBool(false);
        }


        if (other.gameObject.CompareTag("Collectible") && !other.gameObject.GetComponent<InPlace>().isInPlace)
        {

            enemy.SetPickUpDistanceBool(true);

        }
        if (other.gameObject == playerTarget)
        {
            if (other.gameObject.GetComponent<PlayerMovement>().isWalking)
            {
                enemy.SetAggroStatus(true);
            }
        }

        if (other.gameObject.CompareTag("Scanner"))
        {
            enemy.SetIsHitByScanner(true);
            enemy.scannerHit = other.gameObject;
        }

    }


}
