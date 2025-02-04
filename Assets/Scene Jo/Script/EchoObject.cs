using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EchoObject : MonoBehaviour
{
    [SerializeField]
    private GameObject collisionPrefab;

    private void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Instantiate(collisionPrefab, contact.point, Quaternion.identity);
        }
    }
}
