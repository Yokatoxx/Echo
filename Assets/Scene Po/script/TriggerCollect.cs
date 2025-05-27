using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class TriggerCollect : MonoBehaviour
{
    public static event Action<string> OnCollected;
    public static event Action OnAllCollectiblesCollected; // Nouvel événement
    public static List<string> allCollectibleNames = new List<string>();
    public static List<string> collectedNames = new List<string>();

    [Header("FMOD Audio")]
    public string collectSoundEvent = "event:/Zipper";
    public string completionSoundEvent = "event:/CompletionSound"; // Nouveau son de completion

    [Header("Completion Settings")]
    [Tooltip("GameObject à désactiver quand tous les collectibles sont ramassés")]
    public GameObject gameObjectToDisable;

    public GameObject[] collectibles;

    private static bool hasInitializedCount = false;
    private static bool allCollected = false; // Pour éviter de jouer plusieurs fois

    private void Awake()
    {
        collectibles = GameObject.FindGameObjectsWithTag("Collectible");

        if (!hasInitializedCount)
        {
            allCollectibleNames.Clear();
            collectedNames.Clear(); // Reset aussi la liste des collectés
            allCollected = false;

            foreach (GameObject collectible in collectibles)
            {
                allCollectibleNames.Add(collectible.name);
            }
            hasInitializedCount = true;

            Debug.Log($"TriggerCollect initialisé avec {allCollectibleNames.Count} collectibles");
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
                Debug.Log($"Collectible ramassé: {collectibleName} ({collectedNames.Count}/{allCollectibleNames.Count})");
            }

            // Jouer le son FMOD de collecte
            if (!string.IsNullOrEmpty(collectSoundEvent))
            {
                RuntimeManager.PlayOneShot(collectSoundEvent, transform.position);
            }

            OnCollected?.Invoke(collectibleName);
            Destroy(other.gameObject);

            // Vérifier si tous les collectibles ont été ramassés
            CheckAllCollected();
        }
    }

    private void CheckAllCollected()
    {
        if (!allCollected && allCollectibleNames.Count > 0 && collectedNames.Count >= allCollectibleNames.Count)
        {
            allCollected = true;
            Debug.Log("🎉 TOUS LES COLLECTIBLES ONT ÉTÉ RAMASSÉS !");

            // Désactiver le GameObject
            if (gameObjectToDisable != null)
            {
                gameObjectToDisable.SetActive(false);
                Debug.Log($"GameObject '{gameObjectToDisable.name}' désactivé !");
            }

            // Jouer l'événement FMOD de completion
            if (!string.IsNullOrEmpty(completionSoundEvent))
            {
                RuntimeManager.PlayOneShot(completionSoundEvent, transform.position);
                Debug.Log($"Événement FMOD de completion joué: {completionSoundEvent}");
            }

            // Déclencher l'événement pour d'autres scripts qui pourraient en avoir besoin
            OnAllCollectiblesCollected?.Invoke();
        }
    }

    // Méthodes utilitaires publiques
    public static bool AreAllCollectiblesCollected()
    {
        return allCollected;
    }

    public static string GetProgressionStatus()
    {
        return $"{collectedNames.Count}/{allCollectibleNames.Count} collectibles ramassés";
    }

    // Pour debug
    [ContextMenu("Debug - Afficher progression")]
    public void DebugShowProgression()
    {
        Debug.Log(GetProgressionStatus());
        Debug.Log($"Tous collectés: {allCollected}");
    }

    // Pour reset pendant les tests
    [ContextMenu("Debug - Reset collectibles")]
    public void DebugResetCollectibles()
    {
        collectedNames.Clear();
        allCollected = false;
        hasInitializedCount = false;
        Debug.Log("Collectibles reset !");
    }
}