using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerCollect : MonoBehaviour
{
    public static event Action OnCollected;
    public static int totalCollectibles;

    public GameObject[] collectibles;


    private void Awake()
    {
        collectibles = GameObject.FindGameObjectsWithTag("Collectible");

        foreach (GameObject collectible in collectibles)
        {
            totalCollectibles++;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Collectible")
        {
            OnCollected?.Invoke();
            Destroy(other.gameObject);
        }
    }
}
