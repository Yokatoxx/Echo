using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyScanner : MonoBehaviour
{
    void Start()
    {
        float duration = 5f;
        ObjectToThrow objectToThrow = GetComponent<ObjectToThrow>();
        if (objectToThrow != null)
        {
            duration = objectToThrow.growthDuration;
        }
        Invoke(nameof(DestroyObject), duration-2f);
    }

    void DestroyObject()
    {
        Destroy(this.gameObject);
    }
}
