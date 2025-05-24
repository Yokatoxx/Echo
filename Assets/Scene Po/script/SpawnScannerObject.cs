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
    private Coroutine spawnCoroutine; // Référence à la coroutine de spawn

    private void Update()
    {
        if (isOn)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                spawnCoroutine = StartCoroutine(SpawnTerrainScanner());
                timer = cooldown;
            }
        }
        else if (!isOn && spawnCoroutine != null)
        {
            // Arrêter seulement la coroutine de spawn, pas les GrowAndFade
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    IEnumerator SpawnTerrainScanner()
    {
        Vector3 spawnPosition = transform.position;

        for (int i = 0; i < numberOfScanners; i++)
        {
            // Vérifier si isOn est toujours true avant de créer un nouvel écho
            if (!isOn)
            {
                yield break; // Sortir de la coroutine si isOn devient false
            }

            GameObject terrainScanner = Instantiate(TerrainScannerPrefab, spawnPosition, Quaternion.identity);
            // Lancer GrowAndFade indépendamment - cette coroutine continuera même si isOn devient false
            StartCoroutine(GrowAndFade(terrainScanner, growthDuration));
            yield return new WaitForSeconds(spawnDelay);
        }

        spawnCoroutine = null; // Réinitialiser la référence quand terminé
    }

    IEnumerator GrowAndFade(GameObject obj, float duration)
    {
        Vector3 initialScale = obj.transform.localScale;
        float elapsedTime = 0f;
        Renderer renderer = obj.GetComponent<Renderer>();

        // Phase de croissance - continue même si isOn devient false
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

            // Phase de fondu - continue aussi même si isOn devient false
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