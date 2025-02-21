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

    [Header("Growth Settings")]
    public float growthDuration = 10f;
    public float minimumSize = 1f;
    public float maximumSize = 30f;
    public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float growthSpeed = 1f;
    public float alphaMax = 1f;
    public float alphaMin = 0f;
    public float fadeTransitionSpeed = 1f;

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

            // Transfert des paramètres growth à EchoObject
            EchoObject echo = obj.GetComponent<EchoObject>();
            if (echo != null)
            {
                echo.growthDuration = growthDuration;
                echo.minimumSize = minimumSize;
                echo.maximumSize = maximumSize;
                echo.growthCurve = growthCurve;
                echo.growthSpeed = growthSpeed;
                echo.alphaMax = alphaMax;
                echo.alphaMin = alphaMin;
                echo.fadeTransitionSpeed = fadeTransitionSpeed;
            }

            Destroy(obj, 2f);
        }
    }
}
