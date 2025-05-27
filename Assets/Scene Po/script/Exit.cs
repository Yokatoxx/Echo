using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Exit : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeDuration = 1.0f;
    public GameObject fadePanel; // Panel UI blanc pour le fondu

    private Image fadeImage;
    private bool isTransitioning = false;

    private void Start()
    {
        // Si aucun panel n'est assigné, créer un automatiquement
        if (fadePanel == null)
        {
            CreateFadePanel();
        }

        fadeImage = fadePanel.GetComponent<Image>();

        // S'assurer que le panel est transparent au début
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player" && !isTransitioning)
        {
            StartCoroutine(FadeAndChangeScene());
        }
    }

    private IEnumerator FadeAndChangeScene()
    {
        isTransitioning = true;

        // Fondu vers le blanc
        yield return StartCoroutine(FadeToWhite());

        // Charger la nouvelle scène
        SceneManager.LoadScene("SceneMenuPrincipal");
    }

    private IEnumerator FadeToWhite()
    {
        if (fadeImage == null) yield break;

        float elapsedTime = 0f;
        Color startColor = fadeImage.color;
        Color targetColor = new Color(1f, 1f, 1f, 1f); // Blanc opaque

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;

            fadeImage.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }

        fadeImage.color = targetColor;
    }

    private void CreateFadePanel()
    {
        // Créer un Canvas s'il n'existe pas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("FadeCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // S'assurer qu'il est au-dessus de tout

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Créer le panel de fondu
        GameObject panel = new GameObject("FadePanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f); // Blanc transparent

        fadePanel = panel;
    }
}