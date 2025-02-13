using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnScannerGO : MonoBehaviour
{
    public GameObject TerrainScannerPrefab;
    public float duration = 10f;
    public float targetScaleMultiplier = 2f;
    public float sizeChangeSpeed = 1f;
    public int numberOfScanners = 1;
    public float spawnDelay = 1f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(SpawnTerrainScanner());
        }
    }

    IEnumerator SpawnTerrainScanner()
    {
        Vector3 spawnPosition = transform.position;

        for (int i = 0; i < numberOfScanners; i++)
        {
            GameObject terrainScanner = Instantiate(TerrainScannerPrefab, spawnPosition, Quaternion.identity);
            StartCoroutine(GrowAndDestroy(terrainScanner, duration, targetScaleMultiplier));
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    IEnumerator GrowAndDestroy(GameObject obj, float duration, float scaleMultiplier)
    {
        Vector3 initialScale = obj.transform.localScale;
        Vector3 targetScale = initialScale * scaleMultiplier;
        float elapsedTime = 0f;

        while (obj.transform.localScale.x < targetScale.x)
        {
            obj.transform.localScale = Vector3.MoveTowards(obj.transform.localScale, targetScale, sizeChangeSpeed * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;

            if (elapsedTime >= duration)
            {
                break;
            }
        }

        obj.transform.localScale = targetScale;
        Destroy(obj, 1f);
    }
}
