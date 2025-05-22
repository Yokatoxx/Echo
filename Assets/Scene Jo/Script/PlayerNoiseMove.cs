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
        StartCoroutine(GrowAndDestroy(noise, duration, targetScaleMultiplier));
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