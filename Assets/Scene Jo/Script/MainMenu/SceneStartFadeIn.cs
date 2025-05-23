using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneStartFadeIn : MonoBehaviour
{
    [Header("Fade In Configuration")]
    [Tooltip("Image UI pour le fondu (doit être plein écran et noire).")]
    public Image fadeImage;
    
    [Tooltip("Durée du fade in (en secondes).")]
    public float fadeInDuration = 2.0f;
    
    [Tooltip("Délai avant de commencer le fade in (en secondes).")]
    public float delayBeforeStart = 0.1f;
    
    [Tooltip("Courbe d'animation pour le fade in.")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    void Start()
    {
        // Vérifier que l'image de fade est assignée
        if (fadeImage == null)
        {
            Debug.LogError("Aucune image de fade assignée ! Le fade in ne fonctionnera pas.", this);
            return;
        }

        // Commencer avec un écran noir opaque
        fadeImage.color = new Color(0, 0, 0, 1);
        
        // Démarrer le fade in
        StartCoroutine(FadeInCoroutine());
    }

    IEnumerator FadeInCoroutine()
    {
        // Attendre le délai initial si nécessaire
        if (delayBeforeStart > 0)
            yield return new WaitForSeconds(delayBeforeStart);

        float elapsed = 0f;
        Color startColor = new Color(0, 0, 0, 1); // Noir opaque
        Color endColor = new Color(0, 0, 0, 0);   // Transparent

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / fadeInDuration;
            
            // Utiliser la courbe d'animation pour un effet plus naturel
            float curveValue = fadeCurve.Evaluate(normalizedTime);
            
            // Interpoler entre noir opaque et transparent
            fadeImage.color = Color.Lerp(startColor, endColor, curveValue);
            
            yield return null;
        }

        // S'assurer que le fade est complètement terminé
        fadeImage.color = endColor;
        
        // Optionnel : désactiver l'image pour optimiser les performances
        fadeImage.gameObject.SetActive(false);
    }

    // Méthode publique pour relancer le fade in si nécessaire
    [ContextMenu("Test Fade In")]
    public void StartFadeIn()
    {
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0, 0, 0, 1);
            StartCoroutine(FadeInCoroutine());
        }
    }
}