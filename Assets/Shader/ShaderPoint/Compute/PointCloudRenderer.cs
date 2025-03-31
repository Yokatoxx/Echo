using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PointCloudRenderer : MonoBehaviour
{
    [Header("Références essentielles")]
    public ComputeShader computeShader;
    public Material pointMaterial;
    public MeshFilter sourceMesh;

    [Header("Paramètres")]
    [Range(100, 1000000)]
    public int pointCount = 10000;
    public float pointSize = 0.05f;
    public Color pointColor = Color.white;
    public bool generateOnAwake = true;
    
    [Header("Rendering")]
    [Range(0.0f, 0.1f)]
    public float normalOffset = 0.001f;
    public bool alwaysOnTop = false;
    
    [SerializeField, HideInInspector]
    private bool regeneratePoints = false;
    
    // Buffers et mesh
    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;
    private Mesh pointMesh;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    
    private void Awake()
    {
        if (Application.isPlaying && generateOnAwake && sourceMesh != null)
        {
            CreateQuadMesh();
            InitializeBuffers();
            GeneratePointsFromMesh();
        }
    }
    
    private void OnEnable()
    {
        CreateQuadMesh();
        InitializeBuffers();
        
        // Nous déplaçons la génération initiale à OnEnable uniquement si nous ne sommes pas en Awake
        if (!Application.isPlaying && generateOnAwake && sourceMesh != null)
            GeneratePointsFromMesh();
    }
    
    private void OnDisable()
    {
        ReleaseBuffers();
    }
    
    private void OnDestroy()
    {
        ReleaseBuffers();
        
        // Nettoyage supplémentaire pour éviter les fuites
        if (pointMesh != null && !Application.isPlaying)
        {
            DestroyImmediate(pointMesh);
            pointMesh = null;
        }
    }
    
    private void Update()
    {
        // Protection supplémentaire contre les références nulles
        if (this == null || !this.enabled)
            return;
            
        if (computeShader == null || pointMaterial == null || pointMesh == null)
            return;
            
        // Vérifions la régénération en utilisant une variable privée pour éviter les problèmes de sérialisation
        if (regeneratePoints && sourceMesh != null)
        {
            regeneratePoints = false;
            GeneratePointsFromMesh();
        }
            
        // Appliquer les paramètres et dessiner
        UpdateShaderParams();
        RenderPoints();
    }
    
    private void CreateQuadMesh()
    {
        // Protection contre les appels multiples
        if (pointMesh != null)
            return;
            
        pointMesh = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            1, 2, 3
        };
        
        pointMesh.vertices = vertices;
        pointMesh.uv = uvs;
        pointMesh.triangles = triangles;
        pointMesh.RecalculateNormals();
        pointMesh.RecalculateBounds();
    }
    
    private void InitializeBuffers()
    {
        ReleaseBuffers();
        
        if (pointCount <= 0)
            pointCount = 1000; // Valeur par défaut pour éviter les erreurs
        
        // Position buffer
        positionBuffer = new ComputeBuffer(pointCount, 4 * sizeof(float));
        
        // Args buffer
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        
        if (pointMesh != null)
        {
            args[0] = pointMesh.GetIndexCount(0);
            args[1] = (uint)pointCount;
            args[2] = pointMesh.GetIndexStart(0);
            args[3] = pointMesh.GetBaseVertex(0);
            args[4] = 0;
        }
        else
        {
            // Valeurs par défaut si le mesh n'est pas disponible
            args[0] = 0;
            args[1] = 0;
            args[2] = 0;
            args[3] = 0;
            args[4] = 0;
        }
        
        argsBuffer.SetData(args);
    }
    
    private void ReleaseBuffers()
    {
        if (positionBuffer != null)
        {
            positionBuffer.Release();
            positionBuffer = null;
        }
        
        if (argsBuffer != null)
        {
            argsBuffer.Release();
            argsBuffer = null;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Points from Mesh")]
#endif
    public void GeneratePointsFromMesh()
    {
        if (sourceMesh == null || sourceMesh.sharedMesh == null)
        {
            Debug.LogError("Source mesh is missing!");
            return;
        }
        
        // Assurons-nous que les buffers sont initialisés
        if (positionBuffer == null || argsBuffer == null)
        {
            InitializeBuffers();
        }
        
        Mesh mesh = sourceMesh.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals; // Récupérer les normales
        
        if (vertices.Length == 0 || triangles.Length == 0)
        {
            Debug.LogError("Mesh has no vertices or triangles!");
            return;
        }
        
        Vector4[] points = new Vector4[pointCount];
        
        // Calculer les aires des triangles
        float[] triangleAreas = new float[triangles.Length / 3];
        float totalArea = 0;
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (i + 2 >= triangles.Length) continue;
            
            int idx1 = triangles[i];
            int idx2 = triangles[i+1];
            int idx3 = triangles[i+2];
            
            // Protection contre les indices hors limites
            if (idx1 >= vertices.Length || idx2 >= vertices.Length || idx3 >= vertices.Length)
                continue;
                
            Vector3 v1 = vertices[idx1];
            Vector3 v2 = vertices[idx2];
            Vector3 v3 = vertices[idx3];
            
            float area = Vector3.Cross(v2 - v1, v3 - v1).magnitude * 0.5f;
            triangleAreas[i/3] = area;
            totalArea += area;
        }
        
        if (totalArea <= 0)
        {
            Debug.LogError("Mesh has zero surface area!");
            return;
        }
        
        // Distribution uniforme sur la surface
        for (int i = 0; i < pointCount; i++)
        {
            // Sélection d'un triangle pondéré par l'aire
            float randomValue = Random.Range(0, totalArea);
            float areaSum = 0;
            int triangleIndex = 0;
            
            for (int j = 0; j < triangleAreas.Length; j++)
            {
                areaSum += triangleAreas[j];
                if (randomValue <= areaSum)
                {
                    triangleIndex = j;
                    break;
                }
            }
            
            // Coordonnées barycentriques pour point aléatoire dans le triangle
            float r1 = Mathf.Sqrt(Random.value);
            float r2 = Random.value;
            
            float u = 1 - r1;
            float v = r1 * (1 - r2);
            float w = r1 * r2;
            
            // Protection contre les indices hors limites
            int baseIdx = triangleIndex * 3;
            if (baseIdx + 2 >= triangles.Length) continue;
            
            int idx1 = triangles[baseIdx];
            int idx2 = triangles[baseIdx + 1];
            int idx3 = triangles[baseIdx + 2];
            
            if (idx1 >= vertices.Length || idx2 >= vertices.Length || idx3 >= vertices.Length)
                continue;
            
            // Récupérer les sommets du triangle
            Vector3 v1 = vertices[idx1];
            Vector3 v2 = vertices[idx2];
            Vector3 v3 = vertices[idx3];
            
            // Calculer un point aléatoire dans le triangle
            Vector3 randomPoint = u * v1 + v * v2 + w * v3;
            
            // Calculer la normale interpolée
            Vector3 normal = Vector3.zero;
            if (normals != null && normals.Length == vertices.Length)
            {
                normal = u * normals[idx1] + v * normals[idx2] + w * normals[idx3];
                normal.Normalize();
                
                // Décaler légèrement le point dans la direction de la normale
                randomPoint += normal * normalOffset;
            }
            
            // Transformer en coordonnées monde
            Vector3 worldPos = sourceMesh.transform.TransformPoint(randomPoint);
            points[i] = new Vector4(worldPos.x, worldPos.y, worldPos.z, pointSize);
        }
        
        // Mettre à jour le buffer
        if (positionBuffer != null)
            positionBuffer.SetData(points);
            
        Debug.Log($"Generated {pointCount} points from mesh {sourceMesh.sharedMesh.name}");
    }
    
    private void UpdateShaderParams()
    {
        if (computeShader == null || positionBuffer == null) return;
        
        // Paramètres du compute shader
        int kernelIndex = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelIndex, "_PositionBuffer", positionBuffer);
        computeShader.SetFloat("_Time", Time.time);
        
        int threadGroupsX = Mathf.CeilToInt(pointCount / 64.0f);
        computeShader.Dispatch(kernelIndex, threadGroupsX, 1, 1);
        
        // Paramètres du shader de rendu
        if (pointMaterial != null && positionBuffer != null)
        {
            pointMaterial.SetBuffer("_PositionBuffer", positionBuffer);
            pointMaterial.SetColor("_PointColor", pointColor);
            pointMaterial.SetFloat("_PointSize", pointSize);
            pointMaterial.SetFloat("_DepthOffset", normalOffset);
            pointMaterial.SetFloat("_AlwaysOnTop", alwaysOnTop ? 1.0f : 0.0f);
        }
    }
    
    private void RenderPoints()
    {
        if (pointMesh == null || pointMaterial == null || argsBuffer == null) return;
        
        // Dessiner les points
        Graphics.DrawMeshInstancedIndirect(
            pointMesh,
            0,
            pointMaterial,
            new Bounds(transform.position, Vector3.one * 1000f),
            argsBuffer,
            0,
            null,
            ShadowCastingMode.Off,
            true,
            gameObject.layer
        );
    }
    
#if UNITY_EDITOR
    // Méthode spécifique à l'éditeur pour la régénération des points
    public void EditorRegeneratePoints()
    {
        regeneratePoints = true;
        
        // Force une mise à jour dans l'éditeur
        if (!Application.isPlaying && !EditorApplication.isCompiling)
            EditorApplication.QueuePlayerLoopUpdate();
    }
#endif
}

#if UNITY_EDITOR
// Classe d'éditeur personnalisée pour améliorer l'interface utilisateur et éviter les erreurs
[CustomEditor(typeof(PointCloudRenderer))]
public class PointCloudRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PointCloudRenderer renderer = (PointCloudRenderer)target;
        
        // Dessin de l'inspecteur par défaut
        DrawDefaultInspector();
        
        // Ajout d'un bouton de régénération
        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Points"))
        {
            renderer.EditorRegeneratePoints();
        }
    }
}
#endif