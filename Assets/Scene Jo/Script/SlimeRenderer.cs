using UnityEngine;

/// <summary>
/// Renders slime particles as instanced spheres (debug mode) or metaballs
/// Works with both SlimeParticleManager (CPU) and SlimeParticleManagerGPU
/// </summary>
public class SlimeRenderer : MonoBehaviour
{
    [Header("Rendering Mode")]
    public RenderMode renderMode = RenderMode.InstancedSpheres;
    
    [Header("Instanced Spheres")]
    public Mesh sphereMesh;
    public Material sphereMaterial;
    [Range(0.5f, 2.0f)]
    public float sphereScale = 1.0f;
    
    [Header("Metaball Settings")]
    public Material metaballMaterial;
    [Range(0.1f, 2.0f)]
    public float metaballInfluenceRadius = 1.0f;
    [Range(0.1f, 1.0f)]
    public float metaballThreshold = 0.5f;
    public Color slimeColor = new Color(0.2f, 0.8f, 0.3f, 0.8f);
    
    private SlimeParticleManager particleManager;
    private SlimeParticleManagerGPU particleManagerGPU;
    private Matrix4x4[] matrices;
    private ComputeBuffer positionBuffer;
    private MaterialPropertyBlock propertyBlock;
    private bool useGPU = false;
    
    public enum RenderMode
    {
        InstancedSpheres,
        Metaballs
    }
    
    void Start()
    {
        particleManager = GetComponent<SlimeParticleManager>();
        particleManagerGPU = GetComponent<SlimeParticleManagerGPU>();
        
        if (particleManager == null && particleManagerGPU == null)
        {
            Debug.LogError("SlimeRenderer requires either SlimeParticleManager or SlimeParticleManagerGPU component!");
            enabled = false;
            return;
        }
        
        useGPU = (particleManagerGPU != null);
        propertyBlock = new MaterialPropertyBlock();
        
        // Create default sphere mesh if not assigned
        if (sphereMesh == null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(sphere);
        }
        
        // Create default material if not assigned
        if (sphereMaterial == null)
        {
            sphereMaterial = new Material(Shader.Find("Standard"));
            sphereMaterial.color = slimeColor;
            sphereMaterial.SetFloat("_Metallic", 0.2f);
            sphereMaterial.SetFloat("_Glossiness", 0.8f);
        }
    }
    
    void Update()
    {
        if (particleManager == null && particleManagerGPU == null) return;
        
        switch (renderMode)
        {
            case RenderMode.InstancedSpheres:
                RenderInstancedSpheres();
                break;
            case RenderMode.Metaballs:
                RenderMetaballs();
                break;
        }
    }
    
    void RenderInstancedSpheres()
    {
        var positions = useGPU ? particleManagerGPU.GetParticlePositions() : particleManager.GetParticlePositions();
        int count = positions.Count;
        
        if (count == 0) return;
        
        // Resize matrices array if needed
        if (matrices == null || matrices.Length != count)
        {
            matrices = new Matrix4x4[count];
        }
        
        // Update matrices
        float radius = (useGPU ? particleManagerGPU.particleRadius : particleManager.particleRadius) * sphereScale;
        for (int i = 0; i < count; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                positions[i],
                Quaternion.identity,
                Vector3.one * radius * 2.0f
            );
        }
        
        // Draw instanced
        Graphics.DrawMeshInstanced(
            sphereMesh,
            0,
            sphereMaterial,
            matrices,
            count,
            propertyBlock,
            UnityEngine.Rendering.ShadowCastingMode.On,
            true,
            gameObject.layer
        );
    }
    
    void RenderMetaballs()
    {
        // Metaball rendering would require a custom shader with raymarching or marching cubes
        // For now, fall back to instanced spheres with transparency
        var positions = useGPU ? particleManagerGPU.GetParticlePositions() : particleManager.GetParticlePositions();
        int count = positions.Count;
        
        if (count == 0) return;
        
        if (matrices == null || matrices.Length != count)
        {
            matrices = new Matrix4x4[count];
        }
        
        float radius = (useGPU ? particleManagerGPU.particleRadius : particleManager.particleRadius) * metaballInfluenceRadius;
        for (int i = 0; i < count; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                positions[i],
                Quaternion.identity,
                Vector3.one * radius * 2.0f
            );
        }
        
        Material mat = metaballMaterial != null ? metaballMaterial : sphereMaterial;
        
        if (mat != null)
        {
            propertyBlock.SetColor("_Color", slimeColor);
            propertyBlock.SetFloat("_Threshold", metaballThreshold);
        }
        
        Graphics.DrawMeshInstanced(
            sphereMesh,
            0,
            mat,
            matrices,
            count,
            propertyBlock,
            UnityEngine.Rendering.ShadowCastingMode.On,
            true,
            gameObject.layer
        );
    }
    
    void OnDestroy()
    {
        if (positionBuffer != null)
        {
            positionBuffer.Release();
            positionBuffer = null;
        }
    }
}
