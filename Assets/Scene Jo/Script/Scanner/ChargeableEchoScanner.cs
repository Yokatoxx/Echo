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
    public Color intersectionColor = new Color(0.0f, 1.0f, 1.0f, 1.0f);
    public float maxChargeTime = 2f;
    public Color intersectionColorEnd = new Color(1.0f, 0.2f, 0.0f, 1.0f);
    public AnimationCurve spawnCurve = new AnimationCurve(
        new Keyframe(0, 0, 0, 2),
        new Keyframe(0.3f, 0.5f, 1, 1),
        new Keyframe(1, 1, 0, 0)
    );
    public float echoCooldown = 0.25f;
    public int maxActiveEchos = 5;

    public Image chargeIndicatorFill;

    private float currentCharge = 0f;
    private bool isCharging = false;
    private float lastEchoTime = -10f;
    private List<GameObject> activeEchos = new List<GameObject>();

    void Start()
    {
        if (chargeIndicatorFill != null)
            chargeIndicatorFill.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && Time.time > lastEchoTime + echoCooldown)
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
            lastEchoTime = Time.time;

            if (chargeIndicatorFill != null)
                chargeIndicatorFill.gameObject.SetActive(false);
        }
    }

    IEnumerator SpawnEcholocationEffect(float chargeRatio)
    {
        if (activeEchos.Count >= maxActiveEchos)
        {
            if (activeEchos[0] != null)
            {
                Destroy(activeEchos[0]);
            }
            activeEchos.RemoveAt(0);
        }

        Vector3 spawnPosition = transform.position;

        GameObject echoSphere = Instantiate(EcholocationSpherePrefab, spawnPosition, Quaternion.identity);
        activeEchos.Add(echoSphere);

        if (echoSphere.GetComponent<Renderer>() != null)
        {
            Material mat = echoSphere.GetComponent<Renderer>().material;
            mat.SetColor("_MainColor", echoColor);
            mat.SetColor("_IntersectionColorStart", intersectionColor);
            mat.SetColor("_IntersectionColorEnd", intersectionColorEnd);

            float intensityMultiplier = Mathf.Lerp(0.8f, 1.5f, chargeRatio);
            mat.SetFloat("_IntersectionIntensity", intensityMultiplier * 2.0f);
            mat.SetFloat("_IntersectionWidth", Mathf.Lerp(1.0f, 2.0f, chargeRatio));

            mat.SetFloat("_PulseSpeed", Mathf.Lerp(1.0f, 3.0f, chargeRatio));
            mat.SetFloat("_PulseAmount", Mathf.Lerp(0.05f, 0.15f, chargeRatio));
        }

        StartCoroutine(AnimateEchoSphere(echoSphere, chargeRatio));

        yield return null;
    }

    IEnumerator AnimateEchoSphere(GameObject echoSphere, float chargeRatio)
    {
        float elapsedTime = 0f;
        Material material = echoSphere.GetComponent<Renderer>().material;
        float initialAlpha = echoColor.a;

        float targetRadius = Mathf.Lerp(minRadius, maxRadius, chargeRatio);
        float effectDuration = Mathf.Lerp(minDuration, maxDuration, chargeRatio);

        while (elapsedTime < effectDuration && echoSphere != null)
        {
            float progressRatio = elapsedTime / effectDuration;
            float curvedProgress = spawnCurve.Evaluate(progressRatio);
            float currentRadius = targetRadius * curvedProgress;

            if (echoSphere != null)
            {
                echoSphere.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);

                float fadeRatio = elapsedTime / effectDuration;
                float newAlpha = Mathf.Lerp(initialAlpha, 0, fadeOutSpeed * fadeRatio);

                if (material != null)
                {
                    Color currentColor = material.GetColor("_MainColor");
                    material.SetColor("_MainColor", new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha));
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (activeEchos.Contains(echoSphere))
        {
            activeEchos.Remove(echoSphere);
        }

        if (echoSphere != null)
        {
            Destroy(echoSphere);
        }
    }

    private void CleanupNullEchos()
    {
        for (int i = activeEchos.Count - 1; i >= 0; i--)
        {
            if (activeEchos[i] == null)
            {
                activeEchos.RemoveAt(i);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (GameObject echo in activeEchos)
        {
            if (echo != null)
                Destroy(echo);
        }
        activeEchos.Clear();
    }
}
