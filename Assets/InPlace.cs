using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InPlace : MonoBehaviour
{
    public bool isInPlace = true;
    public Vector3 originalPosition;

    public bool GetInPlace()
    {
        return isInPlace;
    }

    public void SetIsInPlace(bool value)
    {
        isInPlace = value;
    }

    private void Awake()
    {
        originalPosition = transform.position;
    }
}
