using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnScannerVoicePrefab : MonoBehaviour
{
    public GameObject TerrainScannerPrefab;
    public FillFromMicrohpone FillFromMicrohpone;
    public float duration = 10f;
    public int numberOfScanners = 1;
    public float spawnDelay = 1f;
    public float minimumSize = 1f;
    public float maximumSize = 30f;

    // Courbe pour régler la vitesse du prefab
    public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public float growthSpeed = 1f;

    // Variables alpha
    public float alphaMax = 1f;
    public float alphaMin = 0f;
    public float fadeTransitionSpeed = 1f;

    void Update()
    {
        if (FillFromMicrohpone.loudness > 0)
        {
            float sizeMultiplier = Mathf.Lerp(minimumSize, maximumSize, FillFromMicrohpone.loudness);
            StartCoroutine(SpawnTerrainScanner(sizeMultiplier));
        }
    }

    IEnumerator SpawnTerrainScanner(float sizeMultiplier)
    {
        Vector3 spawnPosition = transform.position;
        for (int i = 0; i < numberOfScanners; i++)
        {
            GameObject terrainScanner = Instantiate(TerrainScannerPrefab, spawnPosition, Quaternion.identity);
            yield return StartCoroutine(GrowAndFade(terrainScanner, duration, sizeMultiplier));
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    IEnumerator GrowAndFade(GameObject obj, float growthDuration, float scaleMultiplier)
    {
        Vector3 initialScale = obj.transform.localScale;
        float elapsedTime = 0f;
        Renderer renderer = obj.GetComponent<Renderer>();

        // Pour gérer le prefab qui grandit selon la courbe
        while (elapsedTime < growthDuration)
        {
            float curveValue = growthCurve.Evaluate(elapsedTime / growthDuration);
            obj.transform.localScale = initialScale * Mathf.Lerp(1f, scaleMultiplier, curveValue);

            if (renderer != null)
            {
                Color currentColor = renderer.material.GetColor("_IntersectionColor");
                currentColor.a = alphaMax;
                renderer.material.SetColor("_IntersectionColor", currentColor);
            }

            elapsedTime += Time.deltaTime * growthSpeed;
            yield return null;
        }
        obj.transform.localScale = initialScale * scaleMultiplier;

        if (renderer != null)
        {
            Color currentColor = renderer.material.GetColor("_IntersectionColor");
            float currentAlpha = alphaMax;
            while (currentAlpha > alphaMin)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, alphaMin, fadeTransitionSpeed * Time.deltaTime);
                currentColor.a = currentAlpha;
                renderer.material.SetColor("_IntersectionColor", currentColor);
                yield return null;
            }
            currentColor.a = alphaMin;
            renderer.material.SetColor("_IntersectionColor", currentColor);
        }

        Destroy(obj);
    }
}
