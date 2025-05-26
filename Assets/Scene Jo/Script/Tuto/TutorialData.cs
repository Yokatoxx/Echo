using UnityEngine;

[CreateAssetMenu(fileName = "TutorialSettings", menuName = "Tutorial System/Tutorial Settings")]
public class TutorialData : ScriptableObject
{
    [Header("⭐ TEXTES PERSONNALISABLES ⭐")]
    [TextArea(2, 4)]
    public string echoTutorialText = "Appuyez sur ESPACE pour faire un écho";
    [TextArea(2, 4)]
    public string depositText = "↓ Déposez ici ↓";
    [TextArea(2, 4)]
    public string collectibleText = "Appuyez sur E";
    [TextArea(2, 4)]
    public string stealthTutorialText = "Maintenez SHIFT pour vous déplacer en mode furtif";
    [TextArea(2, 4)]
    public string scannerText = "Appuyez sur E pour activer";

    [Header("🎨 COULEURS PERSONNALISABLES 🎨")]
    public Color echoTextColor = Color.cyan;
    public Color depositTextColor = Color.green;
    public Color collectibleTextColor = Color.yellow;
    public Color stealthTextColor = Color.white;
    public Color scannerTextColor = Color.magenta;
    public Color stealthBackgroundColor = new Color(0, 0, 0, 0.7f);

    [Header("📏 PARAMÈTRES TEXTE DÉPÔT")]
    [Range(-2f, 5f)] // ✅ Changé pour permettre des valeurs négatives
    public float depositTextHeightOffset = 0.5f;
    [Range(0.5f, 10f)]
    public float depositTextSize = 2f;
    [Range(0.1f, 3f)]
    public float depositTextScale = 1f;
    [Range(5f, 50f)]
    public float depositTextDisplayDistance = 15f;
    public bool alwaysShowDepositTexts = false;

    [Header("📏 PARAMÈTRES TEXTE COLLECTIBLE")]
    [Range(-2f, 5f)] // ✅ Changé pour permettre des valeurs négatives
    public float collectibleTextHeightOffset = 0.2f; // ✅ Valeur par défaut plus basse
    [Range(0.5f, 10f)]
    public float collectibleTextSize = 1.8f;
    [Range(0.1f, 3f)]
    public float collectibleTextScale = 0.8f;

    [Header("📏 PARAMÈTRES TEXTE SCANNER")]
    [Range(-2f, 5f)] // ✅ Changé pour permettre des valeurs négatives
    public float scannerTextHeightOffset = 0.2f; // ✅ Valeur par défaut plus basse
    [Range(0.5f, 10f)]
    public float scannerTextSize = 1.8f;
    [Range(0.1f, 3f)]
    public float scannerTextScale = 0.8f;

    [Header("📏 PARAMÈTRES TEXTE ECHO")]
    [Range(0.5f, 10f)]
    public float echoTextSize = 3f;
    [Range(0.1f, 3f)]
    public float echoTextScale = 1.2f;
    [Range(1f, 10f)]
    public float echoTextDistance = 5f;
    [Range(-2f, 2f)]
    public float echoTextVerticalOffset = 0f;

    [Header("⭐ PARAMÈTRES STEALTH ⭐")]
    [Range(5f, 50f)]
    public float stealthTriggerDistance = 15f;
    [Range(1f, 10f)]
    public float stealthTextDistance = 3f;
    [Range(0.5f, 10f)]
    public float stealthTextSize = 3f;
    [Range(0.1f, 3f)]
    public float stealthTextScale = 1.2f;
    [Range(-2f, 2f)]
    public float stealthTextVerticalOffset = 0f;

    [Header("🎨 FOND STEALTH 🎨")]
    public bool showStealthBackground = true;
    [Range(10f, 100f)]
    public float stealthBackgroundPadding = 50f;
    [Range(0f, 50f)]
    public float stealthBackgroundCornerRadius = 20f;
    public bool stealthBackgroundShadow = true;

    [Header("⚙️ AUTRES PARAMÈTRES ⚙️")]
    [Range(0.1f, 2f)]
    public float collectibleCheckInterval = 0.5f;
    public float raycastDistance = 5f;
    public LayerMask collectibleLayer = -1;
    [Range(0.01f, 10f)]
    public float blendshapeRestingTolerance = 0.1f;
}