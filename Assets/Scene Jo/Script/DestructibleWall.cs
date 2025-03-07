using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructibleWall : MonoBehaviour
{
    [SerializeField] private int hitsToDestroy = 3;
    private int currentHits = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scanner"))
        {
            currentHits++;

            if (currentHits >= hitsToDestroy)
            {
                Destroy(gameObject);
            }
        }
    }
}
