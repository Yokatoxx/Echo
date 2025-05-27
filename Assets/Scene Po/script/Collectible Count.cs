using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleCount : MonoBehaviour
{
    TMPro.TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMPro.TMP_Text>();
    }

    private void Start()
    {
        UpdateDisplay();
    }

    void OnEnable()
    {
        TriggerCollect.OnCollected += OnCollectibleCollected;
    }

    void OnDisable()
    {
        TriggerCollect.OnCollected -= OnCollectibleCollected;
    }

    public void OnCollectibleCollected(string collectibleName)
    {
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        string displayText = "Liste pour partir : \n";

        foreach (string collectibleName in TriggerCollect.allCollectibleNames)
        {
            if (TriggerCollect.collectedNames.Contains(collectibleName))
            {
                // Texte barré pour les objets collectés
                displayText += $"<s>{collectibleName}</s>\n";
            }
            else
            {
                // Texte normal pour les objets non collectés
                displayText += $"{collectibleName}\n";
            }
        }

        text.text = displayText;
    }
}