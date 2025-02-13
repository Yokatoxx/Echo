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
        if (other.CompareTag("Outline"))
        {
            OutlineParam outlineParam = other.GetComponent<OutlineParam>();
            if (outlineParam != null)
            {
                outlineParam.PulseOutline();
            }
        }
    }
}
