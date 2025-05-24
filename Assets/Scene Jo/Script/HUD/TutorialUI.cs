using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [Header("Prefabs UI")]
    public GameObject tutorialPanelPrefab;
    
    void Start()
    {
        CreateTutorialPanels();
    }
    
    void CreateTutorialPanels()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Aucun Canvas trouvé dans la scène !");
            return;
        }
        
        // Créer les panneaux de tutoriel
        CreateEchoTutorialPanel(canvas);
        CreateCollectibleTutorialPanel(canvas);
        CreateDepositTutorialPanel(canvas);
    }
    
    void CreateEchoTutorialPanel(Canvas canvas)
    {
        GameObject panel = CreateTutorialPanel(canvas, "EchoTutorialPanel");
        AddTutorialText(panel, "Appuyez sur ESPACE pour faire un écho\net découvrir votre environnement", 
                       new Vector2(0, 100), Color.white);
    }
    
    void CreateCollectibleTutorialPanel(Canvas canvas)
    {
        GameObject panel = CreateTutorialPanel(canvas, "CollectibleTutorialPanel");
        AddTutorialText(panel, "Appuyez sur E pour ramasser cet objet", 
                       new Vector2(0, -100), Color.yellow);
        panel.SetActive(false);
    }
    
    void CreateDepositTutorialPanel(Canvas canvas)
    {
        GameObject panel = CreateTutorialPanel(canvas, "DepositTutorialPanel");
        AddTutorialText(panel, "↓ Déposez les objets ici ↓", 
                       Vector2.zero, Color.green);
    }
    
    GameObject CreateTutorialPanel(Canvas canvas, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(canvas.transform, false);
        
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 100);
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.7f);
        
        return panel;
    }
    
    void AddTutorialText(GameObject parent, string text, Vector2 position, Color color)
    {
        GameObject textObj = new GameObject("TutorialText");
        textObj.transform.SetParent(parent.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = position;
        textRect.sizeDelta = new Vector2(380, 80);
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.color = color;
        textComponent.fontSize = 18;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.alignment = TextAlignmentOptions.Center;
    }
}