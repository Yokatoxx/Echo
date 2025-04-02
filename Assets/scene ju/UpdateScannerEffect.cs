using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class UpdateScannerEffect : MonoBehaviour
{
    public Material material;  // Assigne ton matériau Shader Graph ici
    private List<Transform> scanners = new List<Transform>();

    void Start()
    {
        // Trouver toutes les sphères au démarrage
        FindScanners();
    }

    void Update()
    {
        if (material != null)
        {
            Vector3 closestScanner = FindClosestScanner();
            material.SetVector("_Sphere_Position", closestScanner);
        }
    }

    void FindScanners()
    {
        // Récupère tous les objets avec le tag "Scanner"
        GameObject[] scannerObjects = GameObject.FindGameObjectsWithTag("Scanner");
        scanners.Clear();
        foreach (GameObject scanner in scannerObjects)
        {
            scanners.Add(scanner.transform);
        }
    }

    Vector3 FindClosestScanner()
    {
        Vector3 closestPosition = Vector3.zero;
        float closestDistance = Mathf.Infinity;

        foreach (Transform scanner in scanners)
        {
            float distance = Vector3.Distance(scanner.position, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPosition = scanner.position;
            }
        }

        return closestPosition;
    }
}
