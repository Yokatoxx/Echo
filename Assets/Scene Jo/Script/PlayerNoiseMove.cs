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

        // Vérifie si le joueur est en mouvement
        if (!playerMovement.isHiding && IsMoving())
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
            stepTimer = stepInterval;
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
