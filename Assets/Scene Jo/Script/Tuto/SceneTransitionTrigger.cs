using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Configuration de la transition")]
    [SerializeField] private string sceneToLoad = "NextScene"; // Nom de la scène à charger
    [SerializeField] private float fadeDuration = 1.0f; // Durée du fondu en secondes
    [SerializeField] private bool useSceneIndex = false; // Utiliser l'index au lieu du nom
    [SerializeField] private int sceneIndex = 1; // Index de la scène si useSceneIndex est true
    
    [Header("Interface utilisateur")]
    [SerializeField] private Canvas fadeCanvas; // Canvas pour le fondu
    [SerializeField] private UnityEngine.UI.Image fadeImage; // Image blanche pour le fondu
    
    private bool isTransitioning = false;
    
    void Start()
    {
        // Vérifier que le collider est configuré comme trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("Le Collider doit être configuré comme Trigger pour fonctionner correctement!");
        }
        
        // Créer l'interface de fondu si elle n'existe pas
        if (fadeCanvas == null || fadeImage == null)
        {
            CreateFadeInterface();
        }
        
        // S'assurer que le fondu est transparent au début
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
            fadeCanvas.gameObject.SetActive(false);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur qui entre dans le trigger
        if (other.CompareTag("Player") && !isTransitioning)
        {
            Debug.Log("Joueur détecté dans le trigger - Lancement de la transition");
            StartCoroutine(FadeAndChangeScene());
        }
    }
    
    IEnumerator FadeAndChangeScene()
    {
        isTransitioning = true;
        
        // Activer le canvas de fondu
        if (fadeCanvas != null)
        {
            fadeCanvas.gameObject.SetActive(true);
        }
        
        // Fondu vers le blanc
        yield return StartCoroutine(FadeToWhite());
        
        // Charger la nouvelle scène
        LoadNextScene();
    }
    
    IEnumerator FadeToWhite()
    {
        if (fadeImage == null)
        {
            Debug.LogError("Aucune image de fondu trouvée!");
            yield break;
        }
        
        float elapsedTime = 0f;
        Color startColor = fadeImage.color;
        Color targetColor = new Color(1f, 1f, 1f, 1f); // Blanc opaque
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            
            // Interpolation smooth
            t = Mathf.SmoothStep(0f, 1f, t);
            
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        // S'assurer que le fondu est complètement blanc
        fadeImage.color = targetColor;
    }
    
    void LoadNextScene()
    {
        try
        {
            if (useSceneIndex)
            {
                Debug.Log($"Chargement de la scène par index: {sceneIndex}");
                SceneManager.LoadScene(sceneIndex);
            }
            else
            {
                Debug.Log($"Chargement de la scène par nom: {sceneToLoad}");
                SceneManager.LoadScene(sceneToLoad);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors du chargement de la scène: {e.Message}");
        }
    }
    
    void CreateFadeInterface()
    {
        // Créer un Canvas pour le fondu
        GameObject canvasObject = new GameObject("FadeCanvas");
        fadeCanvas = canvasObject.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // S'assurer qu'il est au-dessus de tout
        
        // Ajouter un CanvasScaler
        var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Ajouter un GraphicRaycaster
        canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Créer l'image de fondu
        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);
        
        fadeImage = imageObject.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(1f, 1f, 1f, 0f); // Blanc transparent
        
        // Configurer l'image pour qu'elle couvre tout l'écran
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Désactiver le canvas au début
        fadeCanvas.gameObject.SetActive(false);
        
        Debug.Log("Interface de fondu créée automatiquement");
    }
    
    // Méthode publique pour tester la transition manuellement
    [ContextMenu("Test Transition")]
    public void TestTransition()
    {
        if (!isTransitioning)
        {
            StartCoroutine(FadeAndChangeScene());
        }
    }
}