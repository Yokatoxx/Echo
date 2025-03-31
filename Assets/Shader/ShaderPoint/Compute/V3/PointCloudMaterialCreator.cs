using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class PointCloudMaterialCreator : MonoBehaviour
{
    [SerializeField] private Shader pointCloudShader;
    [SerializeField] private string materialName = "PointCloudMaterial";
    [SerializeField] private Color pointColor = Color.white;
    [SerializeField] private float pointSize = 0.01f;
    
    [ContextMenu("Create Point Cloud Material")]
    public void CreatePointCloudMaterial()
    {
        if (pointCloudShader == null)
        {
            Debug.LogError("Point Cloud Shader not assigned!");
            return;
        }
        
        Material material = new Material(pointCloudShader);
        material.SetColor("_DefaultPointColor", pointColor);
        material.SetFloat("_DefaultPointSize", pointSize);
        
        string path = "Assets/Materials/" + materialName + ".mat";
        
        // Créer le dossier Materials s'il n'existe pas
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        // Sauvegarder le matériau
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Point Cloud Material created at: " + path);
        
        // Sélectionner le matériau
        Selection.activeObject = material;
    }
}
#endif