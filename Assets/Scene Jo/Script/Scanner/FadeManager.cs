using System.Collections.Generic;
using UnityEngine;

public class FadeManagerEmission : MonoBehaviour
{
    public static FadeManagerEmission Instance { get; private set; }

    [Header("Fade Settings")]
    public float fadeStartDistance = 40f;  // Commencer le fade
    public float fadeEndDistance = 50f;    // Distance à laquelle l'objet est totalement invisible
    public bool autoFindObjects = true;    // Rechercher automatiquement les objets à fade

    private List<FadeObjectEmission> objectsToFade = new List<FadeObjectEmission>();
    private Transform playerCamera;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        playerCamera = Camera.main?.transform;

        // Auto-recherche des objets à fader
        if (autoFindObjects)
        {
            FindAllFadeObjects();
        }
    }

    public void FindAllFadeObjects()
    {
        FadeObjectEmission[] allObjects = FindObjectsOfType<FadeObjectEmission>();
        foreach (FadeObjectEmission obj in allObjects)
        {
            RegisterObject(obj);
        }
    }

    public void RegisterObject(FadeObjectEmission obj)
    {
        if (obj != null && !objectsToFade.Contains(obj))
            objectsToFade.Add(obj);
    }

    void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
            if (playerCamera == null) return;
        }

        Vector3 playerPos = playerCamera.position;

        // Nettoyer les objets null de la liste
        objectsToFade.RemoveAll(item => item == null);

        foreach (FadeObjectEmission obj in objectsToFade)
        {
            float dist = Vector3.Distance(playerPos, obj.transform.position);

            if (dist <= fadeStartDistance)
            {
                obj.SetAlphaAndEmission(1f); // Complètement visible
            }
            else if (dist >= fadeEndDistance)
            {
                obj.SetAlphaAndEmission(0f); // Complètement invisible
            }
            else
            {
                float t = (dist - fadeStartDistance) / (fadeEndDistance - fadeStartDistance);
                obj.SetAlphaAndEmission(1f - t); // Fade progressif
            }
        }
    }
}
