using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChargeableEchoScanner : MonoBehaviour
{
    public GameObject EcholocationSpherePrefab;
    public float minRadius = 10f;
    public float maxRadius = 50f;
    public float minDuration = 1.5f;
    public float maxDuration = 4f;
    public float propagationSpeed = 15f;
    public float fadeOutSpeed = 1.5f;
    public Color echoColor = new Color(0.0f, 0.5f, 1.0f, 0.5f);
    public float maxChargeTime = 2f;

    public Image chargeIndicatorFill;

    private float currentCharge = 0f;
    private bool isCharging = false;

    void Start()
    {
        if (chargeIndicatorFill != null)
            chargeIndicatorFill.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isCharging = true;
            currentCharge = 0f;

            if (chargeIndicatorFill != null)
            {
                chargeIndicatorFill.gameObject.SetActive(true);
                chargeIndicatorFill.fillAmount = 0f;
            }
        }

        if (isCharging && Input.GetKey(KeyCode.Space))
        {
            currentCharge += Time.deltaTime;
            currentCharge = Mathf.Clamp(currentCharge, 0f, maxChargeTime);

            if (chargeIndicatorFill != null)
            {
                float chargeRatio = currentCharge / maxChargeTime;
                chargeIndicatorFill.fillAmount = chargeRatio;
            }
        }

        if (isCharging && Input.GetKeyUp(KeyCode.Space))
        {
            float chargeRatio = currentCharge / maxChargeTime;
            StartCoroutine(SpawnEcholocationEffect(chargeRatio));
            isCharging = false;

            if (chargeIndicatorFill != null)
                chargeIndicatorFill.gameObject.SetActive(false);
        }
    }

    IEnumerator SpawnEcholocationEffect(float chargeRatio)
    {
        Vector3 spawnPosition = transform.position;

        GameObject echoSphere = Instantiate(EcholocationSpherePrefab, spawnPosition, Quaternion.identity);
        if (echoSphere.GetComponent<Renderer>() != null)
        {
            echoSphere.GetComponent<Renderer>().material.color = echoColor;
        }

        StartCoroutine(AnimateEchoSphere(echoSphere, chargeRatio));

        yield return null;
    }

    IEnumerator AnimateEchoSphere(GameObject echoSphere, float chargeRatio)
    {
        float elapsedTime = 0f;
        float initialAlpha = echoColor.a;
        Material material = echoSphere.GetComponent<Renderer>().material;

        float targetRadius = Mathf.Lerp(minRadius, maxRadius, chargeRatio);
        float effectDuration = Mathf.Lerp(minDuration, maxDuration, chargeRatio);

        while (elapsedTime < effectDuration)
        {
            float currentRadius = Mathf.Lerp(0, targetRadius, propagationSpeed * elapsedTime / targetRadius);
            echoSphere.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);

            Color currentColor = material.color;
            float newAlpha = Mathf.Lerp(initialAlpha, 0, fadeOutSpeed * elapsedTime / effectDuration);
            material.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(echoSphere);
    }
}
