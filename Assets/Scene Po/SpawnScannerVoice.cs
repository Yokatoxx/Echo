using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnScannerVoice : MonoBehaviour
{
    public GameObject TerrainScannerPrefab;
    public FillFromMicrohpone FillFromMicrohpone;
    public float duration = 10f;
    public int numberOfScanners = 1;
    public float spawnDelay = 1f;
    public float minimumSize = 1;
    public float maximumSize = 30;

    void Update()
    {
        if (FillFromMicrohpone.loudness > 0)
        {
            StartCoroutine(SpawnTerrainScanner(Mathf.Lerp(minimumSize, maximumSize, FillFromMicrohpone.loudness)));
        }
    }

    IEnumerator SpawnTerrainScanner(float size)
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
