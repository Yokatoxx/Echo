using UnityEngine;

public class EchoBillboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Copier exactement la rotation de la caméra
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}