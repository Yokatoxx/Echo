using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnScanner : MonoBehaviour
{
    public GameObject EcholocationSpherePrefab;
    public float duration = 3f;
    public float maxRadius = 50f;
    public float propagationSpeed = 15f;
    public float fadeOutSpeed = 1.5f;
    public Color echoColor = new Color(0.0f, 0.5f, 1.0f, 0.5f);
    public int numberOfPulses = 1;
    public float pulseDelay = 1f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(SpawnEcholocationEffect());
        }
    }

    IEnumerator SpawnEcholocationEffect()
    {
        Vector3 spawnPosition = transform.position;

        for (int i = 0; i < numberOfPulses; i++)
        {
            GameObject echoSphere = Instantiate(EcholocationSpherePrefab, spawnPosition, Quaternion.identity);
            if (echoSphere.GetComponent<Renderer>() != null)
            {
                echoSphere.GetComponent<Renderer>().material.color = echoColor;
            }

            StartCoroutine(AnimateEchoSphere(echoSphere));

            yield return new WaitForSeconds(pulseDelay);
        }
    }

    IEnumerator AnimateEchoSphere(GameObject echoSphere)
    {
        float elapsedTime = 0f;
        float initialAlpha = echoColor.a;
        Material material = echoSphere.GetComponent<Renderer>().material;

        // Animation de croissance et de fade
        while (elapsedTime < duration)
        {
            float currentRadius = Mathf.Lerp(0, maxRadius, propagationSpeed * elapsedTime / maxRadius);
            echoSphere.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);

            Color currentColor = material.color;
            float newAlpha = Mathf.Lerp(initialAlpha, 0, fadeOutSpeed * elapsedTime / duration);
            material.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(echoSphere);
    }
}
