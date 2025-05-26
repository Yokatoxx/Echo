using UnityEngine;
using System.Collections.Generic;

public class ScannerTutorial : BaseTutorial
{
    private List<GameObject> scannerObjects = new List<GameObject>();
    private GameObject lastLookedAtScanner;
    private bool isTextVisible = false;

    protected override void OnInitialize()
    {
        // Trouver tous les objets avec le tag "RepeatSound"
        GameObject[] scanners = GameObject.FindGameObjectsWithTag("RepeatSound");
        scannerObjects.AddRange(scanners);

        Debug.Log($"ScannerTutorial: Trouvé {scannerObjects.Count} objets scanner");
    }

    protected override void OnStartTutorial()
    {
        // Le tutoriel scanner est passif, il se déclenche quand on regarde un scanner éteint
        Debug.Log("ScannerTutorial: Tutoriel démarré - Cherchez des objets scanner éteints !");
    }

    protected override void OnUpdateTutorial()
    {
        HandleScannerLookAt();
        CheckIfAnyScannerActivated();
    }

    private void HandleScannerLookAt()
    {
        bool shouldShowText = false;
        GameObject currentScanner = null;

        if (IsPlayerLookingAt("RepeatSound", tutorialData.raycastDistance, out RaycastHit hit))
        {
            SpawnScannerObject scannerObject = hit.collider.GetComponent<SpawnScannerObject>();
            if (scannerObject != null && !scannerObject.isOn)
            {
                shouldShowText = true;
                currentScanner = hit.collider.gameObject;

                // Si on regarde un nouveau scanner ou si le texte n'est pas visible
                if (currentScanner != lastLookedAtScanner || !isTextVisible)
                {
                    // Cacher l'ancien texte s'il existe
                    if (isTextVisible)
                    {
                        textManager.HideText("scanner");
                    }

                    // Calculer la position du texte
                    Vector3 textPosition = textManager.CalculateTextPosition(
                        currentScanner.transform.position,
                        tutorialData.scannerTextHeightOffset
                    );

                    // Créer ou mettre à jour le texte
                    textManager.CreateWorldText(
                        "scanner",
                        tutorialData.scannerText,
                        tutorialData.scannerTextColor,
                        tutorialData.scannerTextSize,
                        textPosition,
                        tutorialData.scannerTextScale
                    );

                    // Afficher le texte
                    textManager.ShowText("scanner");
                    isTextVisible = true;
                    lastLookedAtScanner = currentScanner;

                    Debug.Log($"ScannerTutorial: Texte affiché pour {currentScanner.name} (isOn: {scannerObject.isOn})");
                }
                else if (currentScanner == lastLookedAtScanner && isTextVisible)
                {
                    // Mettre à jour la position si on regarde toujours le même objet
                    Vector3 textPosition = textManager.CalculateTextPosition(
                        currentScanner.transform.position,
                        tutorialData.scannerTextHeightOffset
                    );
                    textManager.UpdateTextPosition("scanner", textPosition);
                }

                // Vérifier si le joueur appuie sur E
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("ScannerTutorial: Touche E pressée - Tutoriel terminé !");
                    CompleteTutorial();
                }
            }
        }

        // Cacher le texte si on ne regarde plus un scanner éteint
        if (!shouldShowText && isTextVisible)
        {
            textManager.HideText("scanner");
            isTextVisible = false;
            lastLookedAtScanner = null;
            Debug.Log("ScannerTutorial: Texte caché");
        }
    }

    private void CheckIfAnyScannerActivated()
    {
        foreach (GameObject scanner in scannerObjects)
        {
            if (scanner != null)
            {
                SpawnScannerObject scannerObject = scanner.GetComponent<SpawnScannerObject>();
                if (scannerObject != null && scannerObject.isOn)
                {
                    Debug.Log($"ScannerTutorial: Scanner {scanner.name} activé - Tutoriel terminé !");
                    CompleteTutorial();
                    return;
                }
            }
        }
    }

    protected override void OnCompleteTutorial()
    {
        if (isTextVisible)
        {
            textManager.HideText("scanner");
            isTextVisible = false;
        }
    }

    protected override void OnStopTutorial()
    {
        if (isTextVisible)
        {
            textManager.HideText("scanner");
            isTextVisible = false;
        }
    }

    // Méthodes de debug
    [ContextMenu("Debug - Force Show Scanner Text")]
    public void DebugForceShowText()
    {
        if (scannerObjects.Count > 0)
        {
            GameObject testScanner = scannerObjects[0];
            Vector3 textPosition = textManager.CalculateTextPosition(
                testScanner.transform.position,
                tutorialData.scannerTextHeightOffset
            );

            textManager.CreateWorldText(
                "scanner_debug",
                "DEBUG: " + tutorialData.scannerText,
                Color.red,
                tutorialData.scannerTextSize,
                textPosition,
                tutorialData.scannerTextScale
            );

            textManager.ShowText("scanner_debug");
            Debug.Log("Debug: Texte scanner forcé affiché");
        }
    }

    [ContextMenu("Debug - Show All Scanner States")]
    public void DebugShowScannerStates()
    {
        Debug.Log("=== ÉTAT DES SCANNERS ===");
        foreach (GameObject scanner in scannerObjects)
        {
            if (scanner != null)
            {
                SpawnScannerObject scannerObject = scanner.GetComponent<SpawnScannerObject>();
                if (scannerObject != null)
                {
                    Debug.Log($"Scanner {scanner.name}: isOn = {scannerObject.isOn}");
                }
                else
                {
                    Debug.Log($"Scanner {scanner.name}: PAS DE SpawnScannerObject!");
                }
            }
        }
    }
}