using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNoiseMove : MonoBehaviour
{
    public GameObject noisePrefab;
    public Transform noiseSpawnPoint;
    public float stepInterval = 0.5f;

    public float duration = 10f;
    public float targetScaleMultiplier = 2f;
    public float sizeChangeSpeed = 1f;

    [Header("Surface Detection")]
    [SerializeField] private bool enableSurfaceDetection = true; // Variable bool pour activer la détection de surface
    [SerializeField] private float woodSurfaceScaleModifier = 1.05f; // +5% pour WoodSurface
    [SerializeField] private float tileSurfaceScaleModifier = 1.10f; // +10% pour TileSurface
    [SerializeField] private float carpetSurfaceScaleModifier = 0.90f; // -10% pour CarpetSurface

    private PlayerMovement playerMovement;
    private float stepTimer = 0f;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (noiseSpawnPoint == null)
        {
            noiseSpawnPoint = transform;
        }
    }

    void Update()
    {
        if (playerMovement == null || noisePrefab == null)
            return;

        // L'echo passif ne fonctionne que si :
        // - Le joueur n'est pas caché
        // - Le joueur n'est pas en sneak
        // - L'echo passif est actif (pas dans le délai après sneak)
        // - Le joueur bouge
        if (!playerMovement.isHiding &&
            !playerMovement.IsSneaking &&
            playerMovement.IsEchoPassifActive &&
            IsMoving())
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                SpawnNoise();
                stepTimer = 0f;
            }
        }
        else
        {
            // Reset le timer si les conditions ne sont pas remplies
            stepTimer = 0f;
        }
    }

    bool IsMoving()
    {
        float moveForward = Input.GetAxis("Vertical");
        float moveSide = Input.GetAxis("Horizontal");
        return Mathf.Abs(moveForward) > 0.1f || Mathf.Abs(moveSide) > 0.1f;
    }

    void SpawnNoise()
    {
        GameObject noise = Instantiate(noisePrefab, noiseSpawnPoint.position, Quaternion.identity);

        // Appliquer la modification d'échelle selon la surface si activée
        if (enableSurfaceDetection && AudioManager.Instance != null)
        {
            ApplySurfaceScaleModification(noise);
        }

        StartCoroutine(GrowAndDestroy(noise, duration, targetScaleMultiplier));
    }

    /// <summary>
    /// Applique la modification d'échelle selon le type de surface détecté par l'AudioManager
    /// </summary>
    /// <param name="spawnedObject">L'objet instancié à modifier</param>
    private void ApplySurfaceScaleModification(GameObject spawnedObject)
    {
        if (AudioManager.Instance == null) return;

        float scaleModifier = 1.0f;
        string surfaceType = "None";
        int currentSurfaceType = AudioManager.Instance.GetCurrentSurfaceType();

        // Déterminer le modificateur selon le type de surface de l'AudioManager
        switch (currentSurfaceType)
        {
            case 0: // WoodSurface
                scaleModifier = woodSurfaceScaleModifier;
                surfaceType = "Wood";
                break;
            case 1: // TileSurface
                scaleModifier = tileSurfaceScaleModifier;
                surfaceType = "Tile";
                break;
            case 2: // CarpetSurface
                scaleModifier = carpetSurfaceScaleModifier;
                surfaceType = "Carpet";
                break;
            default:
                scaleModifier = woodSurfaceScaleModifier; // Par défaut
                surfaceType = "Default (Wood)";
                break;
        }

        // Appliquer la modification d'échelle si différente de 1.0
        if (scaleModifier != 1.0f)
        {
            Vector3 originalScale = spawnedObject.transform.localScale;
            Vector3 newScale = originalScale * scaleModifier;
            spawnedObject.transform.localScale = newScale;

            Debug.Log($"PlayerNoiseMove - Surface détectée: {surfaceType}. Échelle du prefab modifiée de {originalScale} à {newScale} (facteur: {scaleModifier})");
        }
    }

    /// <summary>
    /// Active ou désactive la détection de surface
    /// </summary>
    /// <param name="enable">True pour activer, false pour désactiver</param>
    public void SetSurfaceDetectionEnabled(bool enable)
    {
        enableSurfaceDetection = enable;
        Debug.Log($"PlayerNoiseMove - Détection de surface {(enable ? "activée" : "désactivée")}");
    }

    IEnumerator GrowAndDestroy(GameObject obj, float duration, float scaleMultiplier)
    {
        Vector3 initialScale = obj.transform.localScale;
        Vector3 targetScale = initialScale * scaleMultiplier;
        float elapsedTime = 0f;

        while (obj.transform.localScale.x < targetScale.x && elapsedTime < duration)
        {
            obj.transform.localScale = Vector3.MoveTowards(obj.transform.localScale, targetScale, sizeChangeSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        obj.transform.localScale = targetScale;
        Destroy(obj);
    }
}