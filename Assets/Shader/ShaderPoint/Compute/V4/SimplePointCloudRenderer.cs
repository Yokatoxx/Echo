using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SimplePointCloudRenderer : MonoBehaviour
{
    // Public variables to assign in inspector
    public ComputeShader computeShader;
    public Material pointMaterial;
    [Range(1, 50)]
    public float pointDensity = 10f;
    [Range(0.001f, 0.1f)]
    public float pointSize = 0.01f;
    
    // Private variables
    private Mesh sourceMesh;
    private Mesh quadMesh;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer pointBuffer;
    private ComputeBuffer countBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private int kernelIndex;
    private bool initialized = false;
    
    // Structures to match the compute shader
    struct Triangle
    {
        public Vector3 pos0, pos1, pos2;
        public Vector3 normal0, normal1, normal2;
        public Vector2 uv0, uv1, uv2;
    }
    
    struct PointData
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
        public float depth;
    }
    
    void OnEnable()
    {
        Init();
    }
    
    void Init()
    {
        // Vérification des références
        if (!computeShader)
        {
            Debug.LogError("Compute Shader is not assigned!");
            enabled = false;
            return;
        }
        
        if (!pointMaterial)
        {
            Debug.LogError("Point Material is not assigned!");
            enabled = false;
            return;
        }
        
        // Récupérer le mesh source
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (!meshFilter || !meshFilter.sharedMesh)
        {
            Debug.LogError("No mesh found on MeshFilter!");
            enabled = false;
            return;
        }
        
        sourceMesh = meshFilter.sharedMesh;
        
        // Créer le quad mesh pour le rendu des points
        CreateQuadMesh();
        
        // Initialiser les buffers
        InitializeBuffers();
        
        // Trouver le kernel dans le compute shader
        kernelIndex = computeShader.FindKernel("GeneratePoints");
        if (kernelIndex < 0)
        {
            Debug.LogError("Kernel 'GeneratePoints' not found in compute shader!");
            enabled = false;
            return;
        }
        
        // Connecter les buffers au compute shader
        computeShader.SetBuffer(kernelIndex, "_Triangles", triangleBuffer);
        computeShader.SetBuffer(kernelIndex, "_Points", pointBuffer);
        computeShader.SetBuffer(kernelIndex, "_PointCount", countBuffer);
        
        initialized = true;
    }
    
    void CreateQuadMesh()
    {
        quadMesh = new Mesh();
        quadMesh.name = "PointQuad";
        
        // Créer un simple quad
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        
        int[] triangles = new int[6]
        {
            0, 1, 2,
            2, 1, 3
        };
        
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        quadMesh.vertices = vertices;
        quadMesh.triangles = triangles;
        quadMesh.uv = uvs;
        quadMesh.RecalculateNormals();
        quadMesh.RecalculateBounds();
    }
    
    void InitializeBuffers()
    {
        // Libérer les buffers existants
        ReleaseBuffers();
        
        // Extraire les données du mesh
        Vector3[] vertices = sourceMesh.vertices;
        Vector3[] normals = sourceMesh.normals;
        Vector2[] uvs = sourceMesh.uv.Length > 0 ? sourceMesh.uv : new Vector2[vertices.Length];
        int[] triangles = sourceMesh.triangles;
        
        // Préparer les données des triangles
        Triangle[] triangleData = new Triangle[triangles.Length / 3];
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int index = i / 3;
            
            // Vérifier les indices pour éviter les erreurs d'index out of range
            int idx0 = triangles[i];
            int idx1 = triangles[i + 1];
            int idx2 = triangles[i + 2];
            
            if (idx0 >= vertices.Length || idx1 >= vertices.Length || idx2 >= vertices.Length)
            {
                Debug.LogError("Invalid triangle indices!");
                continue;
            }
            
            triangleData[index].pos0 = vertices[idx0];
            triangleData[index].pos1 = vertices[idx1];
            triangleData[index].pos2 = vertices[idx2];
            
            // Vérifier si les normales existent
            if (normals.Length > 0)
            {
                triangleData[index].normal0 = idx0 < normals.Length ? normals[idx0] : Vector3.up;
                triangleData[index].normal1 = idx1 < normals.Length ? normals[idx1] : Vector3.up;
                triangleData[index].normal2 = idx2 < normals.Length ? normals[idx2] : Vector3.up;
            }
            else
            {
                // Calculer une normale simple si pas disponible
                Vector3 normal = Vector3.Cross(
                    vertices[idx1] - vertices[idx0], 
                    vertices[idx2] - vertices[idx0]).normalized;
                
                triangleData[index].normal0 = normal;
                triangleData[index].normal1 = normal;
                triangleData[index].normal2 = normal;
            }
            
            // Assigner les UVs
            triangleData[index].uv0 = idx0 < uvs.Length ? uvs[idx0] : new Vector2(0, 0);
            triangleData[index].uv1 = idx1 < uvs.Length ? uvs[idx1] : new Vector2(1, 0);
            triangleData[index].uv2 = idx2 < uvs.Length ? uvs[idx2] : new Vector2(0, 1);
        }
        
        // Créer les buffers
        int triangleCount = triangleData.Length;
        int maxPoints = triangleCount * 20; // Maximum 20 points par triangle
        
        // Calculer la taille des structures
        int sizeOfTriangle = 3 * 12 + 3 * 12 + 3 * 8; // 3 positions + 3 normals + 3 uvs
        int sizeOfPoint = 12 + 12 + 8 + 4; // position + normal + uv + depth
        
        // Créer les buffers
        triangleBuffer = new ComputeBuffer(triangleCount, sizeOfTriangle);
        pointBuffer = new ComputeBuffer(maxPoints, sizeOfPoint);
        countBuffer = new ComputeBuffer(1, 4); // Un uint pour le compteur
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        
        // Initialiser les buffers
        triangleBuffer.SetData(triangleData);
        
        uint[] count = { 0 };
        countBuffer.SetData(count);
        
        // Préparer les arguments pour le rendu instancié
        args[0] = quadMesh.GetIndexCount(0);
        args[1] = 0; // Sera mis à jour à chaque frame
        args[2] = quadMesh.GetIndexStart(0);
        args[3] = quadMesh.GetBaseVertex(0);
        args[4] = 0;
        argsBuffer.SetData(args);
    }
    
    void Update()
    {
        if (!initialized)
        {
            Init();
            if (!initialized) return;
        }
        
        GeneratePoints();
        RenderPoints();
    }
    
    void GeneratePoints()
    {
        // Réinitialiser le compteur
        uint[] count = { 0 };
        countBuffer.SetData(count);
        
        // Mise à jour des paramètres du compute shader
        computeShader.SetFloat("_PointDensity", pointDensity);
        
        // Utiliser la position actuelle de la caméra
        Vector3 cameraPos = Camera.main ? Camera.main.transform.position : new Vector3(0, 0, -10);
        computeShader.SetVector("_CameraPosition", transform.InverseTransformPoint(cameraPos));
        
        // Dispatcher le compute shader
        int threadGroupSize = 64;
        int triangleCount = (int)triangleBuffer.count;
        int threadGroups = Mathf.CeilToInt(triangleCount / (float)threadGroupSize);
        if (threadGroups > 0)
        {
            computeShader.Dispatch(kernelIndex, threadGroups, 1, 1);
        }
        
        // Récupérer le nombre de points générés
        countBuffer.GetData(count);
        uint pointCount = count[0];
        
        // Mettre à jour les arguments pour le rendu
        args[1] = pointCount;
        argsBuffer.SetData(args);
    }
    
    void RenderPoints()
    {
        if (!pointMaterial || pointBuffer == null || argsBuffer == null) return;
        
        // Assigner les buffers et paramètres au material
        pointMaterial.SetBuffer("_PointBuffer", pointBuffer);
        pointMaterial.SetFloat("_PointSize", pointSize);
        
        // Dessiner les points avec GPU instancing
        Graphics.DrawMeshInstancedIndirect(
            quadMesh,
            0,
            pointMaterial,
            new Bounds(transform.position, Vector3.one * 100f),
            argsBuffer
        );
    }
    
    void ReleaseBuffers()
    {
        if (triangleBuffer != null) triangleBuffer.Release();
        if (pointBuffer != null) pointBuffer.Release();
        if (countBuffer != null) countBuffer.Release();
        if (argsBuffer != null) argsBuffer.Release();
    }
    
    void OnDisable()
    {
        ReleaseBuffers();
        initialized = false;
    }
    
    void OnDestroy()
    {
        ReleaseBuffers();
        if (quadMesh) Destroy(quadMesh);
    }
}