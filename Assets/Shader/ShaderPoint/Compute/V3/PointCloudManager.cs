using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PointCloudManager : MonoBehaviour
{
    [System.Serializable]
    public class PointCloudGroup
    {
        public string groupName;
        public List<MeshFilter> meshFilters = new List<MeshFilter>();
        public bool enableOnStart = false;
        public Color pointColor = Color.white;
        public float pointSize = 0.01f;
        [HideInInspector] public HDRPPointCloudSystem pointCloudSystem;
    }
    
    [Header("Configuration")]
    [SerializeField] private ComputeShader pointCloudCompute;
    [SerializeField] private Shader pointCloudShader;
    [SerializeField] private bool createMaterialsAtRuntime = true;
    
    [Header("Groupes de Point Cloud")]
    [SerializeField] private List<PointCloudGroup> pointCloudGroups = new List<PointCloudGroup>();
    
    [Header("Contrôles")]
    [SerializeField] private KeyCode toggleAllKey = KeyCode.P;
    [SerializeField] private KeyCode nextGroupKey = KeyCode.N;
    
    private int currentGroupIndex = -1;
    private bool allActive = false;
    
    private void Start()
    {
        // Créer des systèmes de point cloud pour chaque groupe
        foreach (var group in pointCloudGroups)
        {
            GameObject groupObj = new GameObject(group.groupName + "_PointCloud");
            groupObj.transform.parent = transform;
            
            HDRPPointCloudSystem system = groupObj.AddComponent<HDRPPointCloudSystem>();
            system.enabled = false; // Désactiver temporairement
            
            // Configurer
            if (createMaterialsAtRuntime)
            {
                Material material = new Material(pointCloudShader);
                material.SetColor("_DefaultPointColor", group.pointColor);
                material.SetFloat("_DefaultPointSize", group.pointSize);
                
                // Assigner le material
                var field = typeof(HDRPPointCloudSystem).GetField("pointCloudMaterial", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(system, material);
            }
            
            // Assigner le compute shader
            var computeField = typeof(HDRPPointCloudSystem).GetField("pointCloudCompute", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            computeField?.SetValue(system, pointCloudCompute);
            
            // Assigner les mesh filters
            var meshField = typeof(HDRPPointCloudSystem).GetField("meshesToConvert", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            meshField?.SetValue(system, group.meshFilters);
            
            // Stocker la référence au système
            group.pointCloudSystem = system;
            
            // Activer si nécessaire
            system.enabled = true;
            if (group.enableOnStart)
            {
                system.TogglePointCloud();
            }
        }
    }
    
    private void Update()
    {
        // Toggle tous les groupes
        if (Input.GetKeyDown(toggleAllKey))
        {
            ToggleAllGroups();
        }
        
        // Passer au groupe suivant
        if (Input.GetKeyDown(nextGroupKey))
        {
            ToggleNextGroup();
        }
    }
    
    public void ToggleAllGroups()
    {
        allActive = !allActive;
        
        foreach (var group in pointCloudGroups)
        {
            if (group.pointCloudSystem != null)
            {
                bool isActive = group.pointCloudSystem.IsPointCloudActive();
                if (isActive != allActive)
                {
                    group.pointCloudSystem.TogglePointCloud();
                }
            }
        }
    }
    
    public void ToggleNextGroup()
    {
        // Désactiver le groupe actuel
        if (currentGroupIndex >= 0 && currentGroupIndex < pointCloudGroups.Count)
        {
            var currentSystem = pointCloudGroups[currentGroupIndex].pointCloudSystem;
            if (currentSystem != null && currentSystem.IsPointCloudActive())
            {
                currentSystem.TogglePointCloud();
            }
        }
        
        // Passer au groupe suivant
        currentGroupIndex = (currentGroupIndex + 1) % pointCloudGroups.Count;
        
        // Activer le nouveau groupe
        var nextSystem = pointCloudGroups[currentGroupIndex].pointCloudSystem;
        if (nextSystem != null && !nextSystem.IsPointCloudActive())
        {
            nextSystem.TogglePointCloud();
        }
    }
    
    public void ToggleGroupByName(string groupName)
    {
        var group = pointCloudGroups.FirstOrDefault(g => g.groupName == groupName);
        if (group != null && group.pointCloudSystem != null)
        {
            group.pointCloudSystem.TogglePointCloud();
        }
    }
}