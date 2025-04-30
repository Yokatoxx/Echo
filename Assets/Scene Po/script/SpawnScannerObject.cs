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
            StopCoroutine(SpawnTerrainScanner());
        }
    }

    IEnumerator SpawnTerrainScanner()
    {
        Vector3 spawnPosition = transform.position;

        for (int i = 0; i < numberOfScanners; i++)
        {
            GameObject terrainScanner = Instantiate(TerrainScannerPrefab, spawnPosition, Quaternion.identity);

            Destroy(terrainScanner, duration + 1);
            yield return new WaitForSeconds(spawnDelay);
        }
    }
}
