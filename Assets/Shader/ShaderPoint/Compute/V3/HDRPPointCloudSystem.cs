using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class HDRPPointCloudSystem : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private ComputeShader pointCloudCompute;
    [SerializeField] private Material pointCloudMaterial;
    [SerializeField] private Shader pointCloudShader;

    [Header("Paramètres de Point Cloud")]
    [SerializeField] private float pointSize = 0.01f;
    [SerializeField] private Color pointColor = Color.white;
    [SerializeField] private float samplingRate = 1f; // 1 = tous les points, 2 = un point sur deux, etc.

    [Header("Gestion des objets")]
    [SerializeField] private List<MeshFilter> meshesToConvert = new List<MeshFilter>();
    [SerializeField] private bool convertAllMeshesInScene = false;
    [SerializeField] private bool hideOriginalMeshes = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.P;

    // Buffers et données internes
    private ComputeBuffer pointBuffer;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer normalBuffer;
    private ComputeBuffer uvBuffer;
    private ComputeBuffer indexBuffer;

    private int kernelId;
    private int totalPoints = 0;
    private bool isPointCloudActive = false;
    private Dictionary<Renderer, bool> originalRendererState = new Dictionary<Renderer, bool>();

    // Constantes pour les tailles des buffers
    private const int POINT_STRIDE = sizeof(float) * 7; // position (3) + color (3) + size (1)

    private void Start()
    {
        // S'assurer que nous avons le compute shader et le material
        if (pointCloudCompute == null)
        {
            Debug.LogError("Point Cloud Compute Shader is not assigned!");
            return;
        }

        // Créer le material si nécessaire
        if (pointCloudMaterial == null && pointCloudShader != null)
        {
            pointCloudMaterial = new Material(pointCloudShader);
        }

        if (pointCloudMaterial == null)
        {
            Debug.LogError("Point Cloud Material is not assigned!");
            return;
        }

        // Trouver le kernel principal
        kernelId = pointCloudCompute.FindKernel("GeneratePointCloud");

        // Récupérer les meshes de la scène si demandé
        if (convertAllMeshesInScene)
        {
            meshesToConvert.Clear();
            meshesToConvert.AddRange(FindObjectsOfType<MeshFilter>());
        }

        // Stocker l'état initial des renderers
        StoreOriginalRendererStates();
    }

    private void OnDestroy()
    {
        // Libérer les ressources
        ReleaseBuffers();
    }

    private void Update()
    {
        // Toggle le point cloud avec la touche spécifiée
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePointCloud();
        }
    }

    public void TogglePointCloud()
    {
        isPointCloudActive = !isPointCloudActive;

        if (isPointCloudActive)
        {
            InitializePointCloud();
            if (hideOriginalMeshes)
            {
                SetMeshRenderersVisible(false);
            }
        }
        else
        {
            ReleaseBuffers();
            RestoreOriginalRendererStates();
        }
    }

    // Voici l'implémentation manquante
    private void StoreOriginalRendererStates()
    {
        originalRendererState.Clear();
        foreach (var meshFilter in meshesToConvert)
        {
            if (meshFilter != null && meshFilter.TryGetComponent<Renderer>(out var renderer))
            {
                originalRendererState[renderer] = renderer.enabled;
            }
        }
    }

    private void RestoreOriginalRendererStates()
    {
        foreach (var item in originalRendererState)
        {
            if (item.Key != null)
            {
                item.Key.enabled = item.Value;
            }
        }
    }

    private void SetMeshRenderersVisible(bool visible)
    {
        foreach (var meshFilter in meshesToConvert)
        {
            if (meshFilter != null && meshFilter.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.enabled = visible;
            }
        }
    }

    private void InitializePointCloud()
    {
        // Libérer les anciens buffers
        ReleaseBuffers();

        // Calculer le nombre total de points
        totalPoints = 0;
        foreach (var meshFilter in meshesToConvert)
        {
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                totalPoints += meshFilter.sharedMesh.vertexCount;
            }
        }

        if (totalPoints == 0)
        {
            Debug.LogWarning("No vertices found in the meshes to convert!");
            return;
        }

        // Créer les buffers
        pointBuffer = new ComputeBuffer(totalPoints, POINT_STRIDE);

        // Traiter tous les meshes
        int pointOffset = 0;
        foreach (var meshFilter in meshesToConvert)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            ProcessMesh(meshFilter, pointOffset);
            pointOffset += meshFilter.sharedMesh.vertexCount;
        }
    }

    private void ProcessMesh(MeshFilter meshFilter, int pointOffset)
    {
        Mesh mesh = meshFilter.sharedMesh;

        // Récupérer les données du mesh
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        int[] indices = mesh.triangles;

        // Créer les buffers pour le compute shader
        vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3);
        vertexBuffer.SetData(vertices);

        normalBuffer = new ComputeBuffer(normals.Length, sizeof(float) * 3);
        normalBuffer.SetData(normals);

        uvBuffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
        uvBuffer.SetData(uvs);

        indexBuffer = new ComputeBuffer(indices.Length, sizeof(int));
        indexBuffer.SetData(indices);

        // Configurer le compute shader
        pointCloudCompute.SetBuffer(kernelId, "_Vertices", vertexBuffer);
        pointCloudCompute.SetBuffer(kernelId, "_Normals", normalBuffer);
        pointCloudCompute.SetBuffer(kernelId, "_UVs", uvBuffer);
        pointCloudCompute.SetBuffer(kernelId, "_Indices", indexBuffer);
        pointCloudCompute.SetBuffer(kernelId, "_Points", pointBuffer);

        pointCloudCompute.SetMatrix("_LocalToWorldMatrix", meshFilter.transform.localToWorldMatrix);
        pointCloudCompute.SetFloat("_PointSize", pointSize);
        pointCloudCompute.SetFloat("_SamplingRate", samplingRate);
        pointCloudCompute.SetVector("_PointColor", pointColor);
        pointCloudCompute.SetInt("_VertexCount", vertices.Length);
        pointCloudCompute.SetInt("_IndexCount", indices.Length);

        // Dispatcher le compute shader
        int threadGroups = Mathf.CeilToInt(vertices.Length / 64f);
        pointCloudCompute.Dispatch(kernelId, threadGroups, 1, 1);

        // Libérer les buffers temporaires
        vertexBuffer.Release();
        normalBuffer.Release();
        uvBuffer.Release();
        indexBuffer.Release();

        vertexBuffer = null;
        normalBuffer = null;
        uvBuffer = null;
        indexBuffer = null;
    }

    private void OnRenderObject()
    {
        if (isPointCloudActive && pointBuffer != null && pointCloudMaterial != null)
        {
            // Configurer le material
            pointCloudMaterial.SetBuffer("_PointBuffer", pointBuffer);
            pointCloudMaterial.SetFloat("_DefaultPointSize", pointSize);
            pointCloudMaterial.SetColor("_DefaultPointColor", pointColor);

            // Dessiner les points
            pointCloudMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, totalPoints);
        }
    }

    private void ReleaseBuffers()
    {
        if (pointBuffer != null)
        {
            pointBuffer.Release();
            pointBuffer = null;
        }

        if (vertexBuffer != null)
        {
            vertexBuffer.Release();
            vertexBuffer = null;
        }

        if (normalBuffer != null)
        {
            normalBuffer.Release();
            normalBuffer = null;
        }

        if (uvBuffer != null)
        {
            uvBuffer.Release();
            uvBuffer = null;
        }

        if (indexBuffer != null)
        {
            indexBuffer.Release();
            indexBuffer = null;
        }
    }

    // Accesseurs publics
    public bool IsPointCloudActive() => isPointCloudActive;
    public ComputeBuffer GetPointBuffer() => pointBuffer;
    public int GetPointCount() => totalPoints;

    public void SetPointSize(float size)
    {
        pointSize = size;
        if (isPointCloudActive && pointCloudMaterial != null)
        {
            pointCloudMaterial.SetFloat("_DefaultPointSize", pointSize);
        }
    }

    public void SetPointColor(Color color)
    {
        pointColor = color;
        if (isPointCloudActive && pointCloudMaterial != null)
        {
            pointCloudMaterial.SetColor("_DefaultPointColor", pointColor);
        }
    }
}