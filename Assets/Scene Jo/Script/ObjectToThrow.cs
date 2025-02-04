using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToThrow : MonoBehaviour
{
    [Header("Object to Throw")]
    public GameObject objectPrefab;
    public Transform spawnPoint;

    [Header("Throw Settings")]
    public float throwForce = 20f;

    private Camera fpsCamera;

    void Start()
    {
        fpsCamera = GetComponentInChildren<Camera>();
        if (spawnPoint == null)
        {
            spawnPoint = fpsCamera.transform;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ThrowObject();
        }
    }

    void ThrowObject()
    {
        if (objectPrefab != null && spawnPoint != null)
        {
            GameObject obj = Instantiate(objectPrefab, spawnPoint.position, spawnPoint.rotation);
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(fpsCamera.transform.forward * throwForce, ForceMode.VelocityChange);
            }
            Destroy(obj, 2f);
        }
    }
}
