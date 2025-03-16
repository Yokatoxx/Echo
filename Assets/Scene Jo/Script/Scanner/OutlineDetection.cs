using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using EPOOutline;

public class OutlineDetection : MonoBehaviour
{
    private Outlinable outlinable;

    private void Awake()
    {
        outlinable = GetComponent<Outlinable>();
        if (outlinable != null)
            outlinable.OutlineParameters.Enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Vérifier si l'objet entrant possède un composant Outlinable
        Outlinable targetOutlinable = other.GetComponentInParent<Outlinable>();
        if (targetOutlinable != null)
        {
            OutlineParam outlineParam = other.GetComponent<OutlineParam>();
            if (outlineParam != null)
            {
                outlineParam.PulseOutline();
            }
        }
    }
}
