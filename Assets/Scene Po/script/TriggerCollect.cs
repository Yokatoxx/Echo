using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity; // Ajout du namespace FMOD

public class TriggerCollect : MonoBehaviour
{
    public static event Action<string> OnCollected;
    public static List<string> allCollectibleNames = new List<string>();
    public static List<string> collectedNames = new List<string>();

    [Header("FMOD Audio")]
    public string collectSoundEvent = "event:/Zipper"; // Remplacez par votre chemin d'événement FMOD

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

            // Jouer le son FMOD
            if (!string.IsNullOrEmpty(collectSoundEvent))
            {
                RuntimeManager.PlayOneShot(collectSoundEvent, transform.position);
            }

            OnCollected?.Invoke(collectibleName);
            Destroy(other.gameObject);
        }
    }
}