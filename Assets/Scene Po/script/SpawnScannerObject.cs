using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnScannerObject : MonoBehaviour
{
    public bool isOn = true;
    public GameObject TerrainScannerPrefab;
    public float duration = 1.5f;
    public float cooldown = 1f;
    public int numberOfScanners = 3;
    public float spawnDelay = 0.2f;

    [Header("Growth Settings")]
    public float growthDuration = 10f;
    public float minimumSize = 1f;
    public float maximumSize = 30f;
    public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float growthSpeed = 1f;
    public float alphaMax = 1f;
    public float alphaMin = 0f;
    public float fadeTransitionSpeed = 1f;

    private float timer = 0f;

    private void Update()
    {
        if (isOn)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                StartCoroutine(SpawnTerrainScanner());
                timer = cooldown;
            }
        }
        else if (!isOn)
        {
            StopAllCoroutines();
        }
    }

    IEnumerator SpawnTerrainScanner()
    {
        Vector3 spawnPosition = transform.position;

        for (int i = 0; i < numberOfScanners; i++)
        {
            GameObject terrainScanner = Instantiate(TerrainScannerPrefab, spawnPosition, Quaternion.identity);
            StartCoroutine(GrowAndFade(terrainScanner, growthDuration));
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    IEnumerator GrowAndFade(GameObject obj, float duration)
    {
        Vector3 initialScale = obj.transform.localScale;
        float elapsedTime = 0f;
        Renderer renderer = obj.GetComponent<Renderer>();

        while (elapsedTime < duration)
        {
            float curveValue = growthCurve.Evaluate(elapsedTime / duration);
            float scaleMultiplier = Mathf.Lerp(minimumSize, maximumSize, curveValue);

            if (obj != null) // Vérification que l'objet existe toujours
            {
                obj.transform.localScale = initialScale * scaleMultiplier;

                if (renderer != null)
                {
                    if (renderer.material.HasProperty("_IntersectionColorStart"))
                    {
                        Color currentColor = renderer.material.GetColor("_IntersectionColorStart");
                        currentColor.a = alphaMax;
                        renderer.material.SetColor("_IntersectionColorStart", currentColor);
                    }
                }
            }
            else
            {
                yield break; // Sortir si l'objet a été détruit
            }

            elapsedTime += Time.deltaTime * growthSpeed;
            yield return null;
        }

        if (obj != null)
        {
            obj.transform.localScale = initialScale * maximumSize;

            // fondu
            if (renderer != null)
            {
                if (renderer.material.HasProperty("_IntersectionColorStart"))
                {
                    Color currentColor = renderer.material.GetColor("_IntersectionColorStart");
                    float currentAlpha = alphaMax;
                    while (currentAlpha > alphaMin && obj != null)
                    {
                        currentAlpha = Mathf.MoveTowards(currentAlpha, alphaMin, fadeTransitionSpeed * Time.deltaTime);
                        currentColor.a = currentAlpha;
                        renderer.material.SetColor("_IntersectionColorStart", currentColor);
                        yield return null;
                    }

                    if (obj != null)
                    {
                        currentColor.a = alphaMin;
                        renderer.material.SetColor("_IntersectionColorStart", currentColor);
                    }
                }
            }

            Destroy(obj);
        }
    }
}
