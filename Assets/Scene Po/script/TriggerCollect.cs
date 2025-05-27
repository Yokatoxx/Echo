using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerCollect : MonoBehaviour
{
    public static event Action<string> OnCollected; // Maintenant on passe le nom de l'objet
    public static List<string> allCollectibleNames = new List<string>();
    public static List<string> collectedNames = new List<string>();

    public GameObject[] collectibles;

    private static bool hasInitializedCount = false;

    private void Awake()
    {
        collectibles = GameObject.FindGameObjectsWithTag("Collectible");

        if (!hasInitializedCount)
        {
            allCollectibleNames.Clear();
            foreach (GameObject collectible in collectibles)
            {
                allCollectibleNames.Add(collectible.name);
            }
            hasInitializedCount = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Collectible")
        {
            string collectibleName = other.gameObject.name;

            if (!collectedNames.Contains(collectibleName))
            {
                collectedNames.Add(collectibleName);
            }

            OnCollected?.Invoke(collectibleName);
            Destroy(other.gameObject);
        }
    }
}