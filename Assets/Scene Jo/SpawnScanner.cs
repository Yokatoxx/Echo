using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnScanner : MonoBehaviour
{
    public GameObject TerrainScannerPrefab;
    public float duration = 10f;
    public float size = 500f;
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
        for (int i = 0; i < numberOfScanners; i++)
        {
            GameObject terrainScanner = Instantiate(TerrainScannerPrefab, transform.position, Quaternion.identity);
            ParticleSystem terrainScannerPS = terrainScanner.transform.GetChild(0).GetComponent<ParticleSystem>();

            if (terrainScannerPS != null)
            {
                var main = terrainScannerPS.main;
                main.startLifetime = duration;
                main.startSize = size;
            }

            Destroy(terrainScanner, duration + 1);
            yield return new WaitForSeconds(spawnDelay);
        }
    }
}
