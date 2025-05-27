using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class TutorialTextManager : MonoBehaviour
{
    [Header("References")]
    public Canvas tutorialCanvas;
    public bool autoCreateCanvas = true;

    private Material alwaysOnTopMaterial;
    private Material normalTextMaterial;
    private TutorialData tutorialData;

    // Cache des textes créés
    private Dictionary<string, GameObject> textObjects = new Dictionary<string, GameObject>();

    public void Initialize(TutorialData data)
    {
        tutorialData = data;

        try
        {
            CreateAlwaysOnTopMaterials();
            SetupTutorialCanvas();
            Debug.Log("TutorialTextManager initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize TutorialTextManager: {e.Message}");
            throw; // Re-throw pour que TutorialManager puisse gérer l'erreur
        }
    }

    void CreateAlwaysOnTopMaterials()
    {
        try
        {
            // Essayer de trouver les shaders avec plusieurs noms possibles
            Shader spriteShader = FindShaderSafely(new string[] {
                "Sprites/Default",
                "Legacy Shaders/Transparent/Diffuse",
                "Unlit/Transparent",
                "UI/Default"
            });

            Shader textShader = FindShaderSafely(new string[] {
                "TextMeshPro/Distance Field",
                "TextMeshPro/Mobile/Distance Field",
                "GUI/Text Shader",
                "Unlit/Text"
            });

            // Créer le matériau pour texte toujours visible
            if (spriteShader != null)
            {
                alwaysOnTopMaterial = new Material(spriteShader);
                alwaysOnTopMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                alwaysOnTopMaterial.SetFloat("_ZWrite", 0);
                alwaysOnTopMaterial.renderQueue = 4000;
                Debug.Log($"AlwaysOnTopMaterial created with shader: {spriteShader.name}");
            }
            else
            {
                Debug.LogWarning("Could not find sprite shader, using default material");
                alwaysOnTopMaterial = new Material(Shader.Find("Standard"));
            }

            // Créer le matériau normal pour les textes
            if (textShader != null)
            {
                normalTextMaterial = new Material(textShader);
                Debug.Log($"NormalTextMaterial created with shader: {textShader.name}");
            }
            else
            {
                Debug.LogWarning("Could not find text shader, using default material");
                normalTextMaterial = new Material(Shader.Find("Standard"));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating materials: {e.Message}");
            // Créer des matériaux de fallback
            alwaysOnTopMaterial = new Material(Shader.Find("Standard"));
            normalTextMaterial = new Material(Shader.Find("Standard"));
        }
    }

    // Méthode utilitaire pour trouver un shader de manière sécurisée
    Shader FindShaderSafely(string[] shaderNames)
    {
        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                Debug.Log($"Found shader: {shaderName}");
                return shader;
            }
        }

        Debug.LogWarning($"None of the shaders found: {string.Join(", ", shaderNames)}");
        return null;
    }

    void SetupTutorialCanvas()
    {
        try
        {
            if (tutorialCanvas == null && autoCreateCanvas)
            {
                GameObject canvasObj = new GameObject("TutorialCanvas");
                tutorialCanvas = canvasObj.AddComponent<Canvas>();

                tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                tutorialCanvas.sortingOrder = 32767;
                tutorialCanvas.overrideSorting = true;

                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                Debug.Log("Canvas de tutoriel créé en mode ScreenSpace-Overlay");
            }

            if (tutorialCanvas != null)
            {
                tutorialCanvas.sortingOrder = 32767;
                tutorialCanvas.overrideSorting = true;
                tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up tutorial canvas: {e.Message}");
            throw;
        }
    }

    public GameObject CreateScreenCenterText(string id, string text, Color color, float fontSize)
    {
        try
        {
            if (textObjects.ContainsKey(id))
            {
                UpdateTextContent(id, text);
                return textObjects[id];
            }

            GameObject textObj;

            if (tutorialCanvas != null)
            {
                // Créer le texte UI sur le canvas overlay
                textObj = new GameObject($"{id}_Text");
                textObj.transform.SetParent(tutorialCanvas.transform, false);

                TextMeshProUGUI textMesh = textObj.AddComponent<TextMeshProUGUI>();
                textMesh.text = text;
                textMesh.color = color;
                textMesh.fontSize = fontSize * 10f;
                textMesh.fontStyle = FontStyles.Bold;
                textMesh.alignment = TextAlignmentOptions.Center;

                RectTransform rectTransform = textObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(800, 200);
            }
            else
            {
                // Fallback : créer comme TextMeshPro 3D
                textObj = CreateAlwaysVisibleText($"{id}_Text", text, color, fontSize);
            }

            textObj.SetActive(false);
            textObjects[id] = textObj;
            return textObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating screen center text '{id}': {e.Message}");
            return null;
        }
    }

    public GameObject CreateWorldText(string id, string text, Color color, float fontSize, Vector3 position, float scale = 1f)
    {
        try
        {
            // Vérifier si le texte existe déjà
            if (textObjects.ContainsKey(id))
            {
                // Mettre à jour le texte existant
                UpdateTextPosition(id, position);
                UpdateTextContent(id, text);
                UpdateTextColor(id, color);
                return textObjects[id];
            }

            // Créer un nouveau texte
            GameObject textObj = new GameObject($"{id}_WorldText");
            textObj.transform.position = position;
            textObj.transform.localScale = Vector3.one * scale;

            TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.color = color;
            textMesh.fontSize = fontSize;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.rectTransform.sizeDelta = new Vector2(4, 1.5f);
            textMesh.sortingOrder = 950;

            // Ajouter le Billboard seulement si le composant existe dans le projet
            try
            {
                var billboardType = System.Type.GetType("Billboard");
                if (billboardType != null)
                {
                    textObj.AddComponent(billboardType);
                }
                else
                {
                    Debug.LogWarning("Billboard component not found. Text will not face camera automatically.");
                }
            }
            catch (System.Exception)
            {
                Debug.LogWarning("Could not add Billboard component. Text will not face camera automatically.");
            }

            textObj.SetActive(false);
            textObjects[id] = textObj;
            return textObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating world text '{id}': {e.Message}");
            return null;
        }
    }

    GameObject CreateAlwaysVisibleText(string name, string text, Color color, float fontSize)
    {
        try
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.localScale = Vector3.one;

            TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.color = color;
            textMesh.fontSize = fontSize;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.rectTransform.sizeDelta = new Vector2(12, 4);

            // Appliquer le matériau spécial seulement s'il existe
            MeshRenderer meshRenderer = textObj.GetComponent<MeshRenderer>();
            if (meshRenderer != null && alwaysOnTopMaterial != null)
            {
                meshRenderer.material = alwaysOnTopMaterial;
                meshRenderer.material.color = color;
            }

            textObj.layer = 31;
            return textObj;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating always visible text '{name}': {e.Message}");
            return null;
        }
    }

    public GameObject CreateBackgroundPanel(string id, Vector2 size, Color color, float cornerRadius = 20f)
    {
        try
        {
            if (tutorialCanvas == null) return null;

            string panelId = $"{id}_Background";
            if (textObjects.ContainsKey(panelId))
            {
                return textObjects[panelId];
            }

            GameObject panel = new GameObject(panelId);
            panel.transform.SetParent(tutorialCanvas.transform, false);

            Image backgroundImage = panel.AddComponent<Image>();

            // Créer une texture simple si la texture arrondie échoue
            try
            {
                Texture2D roundedTexture = CreateRoundedTexture(200, 100, (int)cornerRadius);
                Sprite roundedSprite = Sprite.Create(roundedTexture, new Rect(0, 0, roundedTexture.width, roundedTexture.height), new Vector2(0.5f, 0.5f));
                backgroundImage.sprite = roundedSprite;
            }
            catch (System.Exception)
            {
                Debug.LogWarning("Could not create rounded texture, using simple color");
                // Utiliser juste la couleur sans sprite personnalisé
            }

            backgroundImage.color = color;
            backgroundImage.type = Image.Type.Sliced;

            RectTransform bgRectTransform = panel.GetComponent<RectTransform>();
            bgRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            bgRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bgRectTransform.anchoredPosition = Vector2.zero;
            bgRectTransform.sizeDelta = size;

            panel.SetActive(false);
            textObjects[panelId] = panel;
            return panel;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating background panel '{id}': {e.Message}");
            return null;
        }
    }

    Texture2D CreateRoundedTexture(int width, int height, int cornerRadius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distanceToCorner = 0f;

                if (x < cornerRadius && y < cornerRadius)
                {
                    distanceToCorner = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius));
                }
                else if (x >= width - cornerRadius && y < cornerRadius)
                {
                    distanceToCorner = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, cornerRadius));
                }
                else if (x < cornerRadius && y >= height - cornerRadius)
                {
                    distanceToCorner = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, height - cornerRadius - 1));
                }
                else if (x >= width - cornerRadius && y >= height - cornerRadius)
                {
                    distanceToCorner = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, height - cornerRadius - 1));
                }

                if ((x < cornerRadius || x >= width - cornerRadius) &&
                    (y < cornerRadius || y >= height - cornerRadius) &&
                    distanceToCorner > cornerRadius)
                {
                    colors[y * width + x] = Color.clear;
                }
                else
                {
                    colors[y * width + x] = Color.white;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    public void ShowText(string id)
    {
        if (textObjects.ContainsKey(id) && textObjects[id] != null)
        {
            textObjects[id].SetActive(true);
        }
    }

    public void HideText(string id)
    {
        if (textObjects.ContainsKey(id) && textObjects[id] != null)
        {
            textObjects[id].SetActive(false);
        }
    }

    public void UpdateTextContent(string id, string newText)
    {
        if (textObjects.ContainsKey(id) && textObjects[id] != null)
        {
            GameObject textObj = textObjects[id];
            TextMeshProUGUI uiText = textObj.GetComponent<TextMeshProUGUI>();
            if (uiText != null)
            {
                uiText.text = newText;
                return;
            }

            TextMeshPro worldText = textObj.GetComponent<TextMeshPro>();
            if (worldText != null)
            {
                worldText.text = newText;
            }
        }
    }

    public void UpdateTextPosition(string id, Vector3 position)
    {
        if (textObjects.ContainsKey(id) && textObjects[id] != null)
        {
            textObjects[id].transform.position = position;
        }
    }

    public void UpdateTextColor(string id, Color color)
    {
        if (textObjects.ContainsKey(id) && textObjects[id] != null)
        {
            GameObject textObj = textObjects[id];
            TextMeshProUGUI uiText = textObj.GetComponent<TextMeshProUGUI>();
            if (uiText != null)
            {
                uiText.color = color;
                return;
            }

            TextMeshPro worldText = textObj.GetComponent<TextMeshPro>();
            if (worldText != null)
            {
                worldText.color = color;
            }
        }
    }

    public Vector3 CalculateTextPosition(Vector3 objectPosition, float heightOffset)
    {
        Vector3 textPosition = objectPosition;

        try
        {
            Collider objCollider = GetColliderAtPosition(objectPosition);

            if (objCollider != null)
            {
                if (heightOffset >= 0)
                {
                    textPosition.y = objCollider.bounds.max.y + heightOffset;
                }
                else
                {
                    textPosition.y = objCollider.bounds.center.y + heightOffset;
                }
            }
            else
            {
                textPosition.y += heightOffset;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error calculating text position: {e.Message}");
            textPosition.y += heightOffset;
        }

        return textPosition;
    }

    Collider GetColliderAtPosition(Vector3 position)
    {
        try
        {
            Collider[] colliders = Physics.OverlapSphere(position, 0.5f);

            if (colliders.Length > 0)
            {
                foreach (Collider col in colliders)
                {
                    if (!col.isTrigger)
                    {
                        return col;
                    }
                }
                return colliders[0];
            }

            colliders = Physics.OverlapSphere(position, 2f);
            if (colliders.Length > 0)
            {
                return colliders[0];
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error getting collider at position: {e.Message}");
        }

        return null;
    }

    public void DestroyText(string id)
    {
        if (textObjects.ContainsKey(id))
        {
            if (textObjects[id] != null)
            {
                Destroy(textObjects[id]);
            }
            textObjects.Remove(id);
        }
    }

    public void DestroyAllTexts()
    {
        foreach (var kvp in textObjects)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        textObjects.Clear();
    }
}