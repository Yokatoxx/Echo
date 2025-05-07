using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerCollect : MonoBehaviour
{
    public static event Action OnCollected;
    public static int totalCollectibles;

    public GameObject[] collectibles;

    private static bool hasInitializedCount = false;

    private void Awake()
    {
        collectibles = GameObject.FindGameObjectsWithTag("Collectible");

        if (!hasInitializedCount)
        {
            totalCollectibles = collectibles.Length;
            hasInitializedCount = true;
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
