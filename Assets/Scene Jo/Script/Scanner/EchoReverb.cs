using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EchoReverb : MonoBehaviour
{
    public GameObject echoEffectPrefab;
    public float reverbIntensityFactor = 0.7f;
    public int maxReverbBounces = 3;
    public float minIntensityThreshold = 0.1f;
    public LayerMask echoLayerMask; // Layer de l'effet d'écho
    private bool isReverberating = false;
    private float detectionRadius = 0.5f; // Rayon de détection de collision

    void Update()
    {
        // Détecter si un effet d'écho est à proximité
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, echoLayerMask);

        if (hitColliders.Length > 0 && !isReverberating)
        {
            foreach (var hitCollider in hitColliders)
            {
                // Vérifier si c'est un scanner d'écho
                GameObject scanner = hitCollider.gameObject;

                // Si le scanner a un transform, on peut récupérer sa taille
                if (scanner != null)
                {
                    // Estimation de la taille basée sur l'échelle de l'objet
                    float incomingSize = scanner.transform.localScale.x;

                    // Estimation de la durée (peut nécessiter une référence au script SpawnScanner)
                    float incomingDuration = 5f; // Valeur par défaut, à ajuster

                    // Essayer de récupérer la durée depuis le parent si c'est un scanner
                    Transform parent = scanner.transform.parent;
                    if (parent != null)
                    {
                        SpawnScanner scannerScript = parent.GetComponent<SpawnScanner>();
                        if (scannerScript != null)
                        {
                            incomingDuration = scannerScript.duration;
                        }
                    }

                    // Démarrer l'effet de réverbération
                    StartCoroutine(CreateReverbEffect(transform.position, incomingSize, incomingDuration, 1));
                    break;
                }
            }
        }
    }

    IEnumerator CreateReverbEffect(Vector3 position, float size, float duration, int bounceCount)
    {
        if (bounceCount > maxReverbBounces || size * reverbIntensityFactor < minIntensityThreshold)
            yield break;

        isReverberating = true;

        // Créer l'effet de réverbération
        GameObject reverbEffect = Instantiate(echoEffectPrefab, position, Quaternion.identity);

        // Réduire la taille et la durée pour chaque rebond
        float newSize = size * reverbIntensityFactor;
        float newDuration = duration * reverbIntensityFactor;

        // Appliquer une échelle initiale minimale
        reverbEffect.transform.localScale = Vector3.one * 0.1f;

        // Faire grandir la sphère progressivement
        float elapsedTime = 0f;
        while (elapsedTime < newDuration)
        {
            float scale = Mathf.Lerp(0.1f, newSize, elapsedTime / newDuration);
            reverbEffect.transform.localScale = Vector3.one * scale;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Détruire l'effet
        Destroy(reverbEffect);

        // Attendre un peu avant de pouvoir réverbérer à nouveau
        yield return new WaitForSeconds(0.2f);

        // Créer la prochaine réverbération
        StartCoroutine(CreateReverbEffect(position, newSize, newDuration, bounceCount + 1));

        isReverberating = false;
    }
}
