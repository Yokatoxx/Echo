using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    [SerializeField] private bool lockYAxis = false; // Empêche la rotation sur l'axe Y si nécessaire

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
            Vector3 targetPosition = mainCamera.transform.position;

            if (lockYAxis)
            {
                targetPosition.y = transform.position.y;
            }

            // Orienter vers la caméra
            transform.LookAt(targetPosition);

            // Rotation pour que le texte soit lisible
            transform.Rotate(0, 180, 0);
        }
    }
}