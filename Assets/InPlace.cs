using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InPlace : MonoBehaviour
{
    public bool isInPlace = true;
    public Transform originalPosition;

    private void Start()
    {
        originalPosition = transform.parent.transform;

    }

    private void Update()
    {
        //if (transform.position == originalPosition.position)
        //{
        //    isInPlace = true;
        //}
        //else
        //{
        //    isInPlace = false;
        //}


        //isInPlace = (transform.position == originalPosition.position);

    }
}
