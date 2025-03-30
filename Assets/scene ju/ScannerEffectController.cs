using UnityEngine;

public class ScannerEffectController : MonoBehaviour
{
    public string scannerTag = "Scanner";
    private Material objectMaterial;

    void Start()
    {
        objectMaterial = GetComponent<Renderer>().material;
    }

    void Update()
    {
        GameObject scanner = GameObject.Find("SCANOUI"); // Récupère SCANOUI dans la scène
        if (scanner != null)
        {
            Vector3 scannerPos = scanner.transform.position;
            float scannerSize = scanner.transform.localScale.x; // On suppose un scale uniforme

            objectMaterial.SetVector("_ScannerPosition", scannerPos);
            objectMaterial.SetFloat("_ScannerSize", scannerSize);

            Debug.Log("Scanner Position: " + scannerPos + " | Size: " + scannerSize);
        }
    }

}
