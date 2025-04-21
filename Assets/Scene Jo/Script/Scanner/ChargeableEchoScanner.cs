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
    public Color intersectionColorEnd = new Color(1.0f, 0.2f, 0.0f, 1.0f); // Orange/rouge pour la fin du gradient
    public AnimationCurve spawnCurve = new AnimationCurve(
        new Keyframe(0, 0, 0, 2),      // Départ lent
        new Keyframe(0.3f, 0.5f, 1, 1), // Accélération au milieu
        new Keyframe(1, 1, 0, 0)       // Ralentissement à la fin
    );
    public float echoCooldown = 0.25f; // Temps minimum entre deux échos
    public int maxActiveEchos = 5; // Nombre maximum d'échos actifs simultanément

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
        // Limite le nombre d'échos actifs
        if (activeEchos.Count >= maxActiveEchos)
        {
            // Supprime l'écho le plus ancien si le maximum est atteint
            if (activeEchos[0] != null)
            {
                Destroy(activeEchos[0]);
            }
            activeEchos.RemoveAt(0);
        }

        Vector3 spawnPosition = transform.position;

        GameObject echoSphere = Instantiate(EcholocationSpherePrefab, spawnPosition, Quaternion.identity);
        activeEchos.Add(echoSphere); // Ajoute le nouvel écho à la liste

        if (echoSphere.GetComponent<Renderer>() != null)
        {
            Material mat = echoSphere.GetComponent<Renderer>().material;
            // Configurer les couleurs du matériau
            mat.SetColor("_MainColor", echoColor);
            mat.SetColor("_IntersectionColorStart", intersectionColor);
            mat.SetColor("_IntersectionColorEnd", intersectionColorEnd);

            // Ajuster l'intensité de l'intersection en fonction de la charge
            float intensityMultiplier = Mathf.Lerp(0.8f, 1.5f, chargeRatio);
            mat.SetFloat("_IntersectionIntensity", intensityMultiplier * 2.0f);
            mat.SetFloat("_IntersectionWidth", Mathf.Lerp(1.0f, 2.0f, chargeRatio));

            // Paramètres de pulsation
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
            // Utilisation de la courbe pour l'expansion progressive
            float progressRatio = elapsedTime / effectDuration;
            float curvedProgress = spawnCurve.Evaluate(progressRatio);
            float currentRadius = targetRadius * curvedProgress;

            if (echoSphere != null)
            {
                echoSphere.transform.localScale = new Vector3(currentRadius, currentRadius, currentRadius);

                // Effet de fade-out progressif
                float fadeRatio = elapsedTime / effectDuration;
                float newAlpha = Mathf.Lerp(initialAlpha, 0, fadeOutSpeed * fadeRatio);

                // Mettre à jour l'alpha du matériau
                if (material != null)
                {
                    Color currentColor = material.GetColor("_MainColor");
                    material.SetColor("_MainColor", new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha));
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Supprime l'écho de la liste active
        if (activeEchos.Contains(echoSphere))
        {
            activeEchos.Remove(echoSphere);
        }

        if (echoSphere != null)
        {
            Destroy(echoSphere);
        }
    }

    // Méthode utilitaire pour nettoyer les échos nuls ou invalides
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
        // Nettoie les échos restants quand l'objet est détruit
        foreach (GameObject echo in activeEchos)
        {
            if (echo != null)
                Destroy(echo);
        }
        activeEchos.Clear();
    }
}