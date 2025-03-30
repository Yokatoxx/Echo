using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class SpawnScannerObject : MonoBehaviour
{
    public bool isOn = true;
    public GameObject TerrainScannerPrefab;
    public float duration = 1.5f;
    public float size = 20f;
    public int numberOfScanners = 3;
    public float spawnDelay = 0.2f;
    public float cooldown = 1f;

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
