using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TestGameViewPoints : MonoBehaviour
{
    public Material pointMaterial;
    public int pointCount = 1000;
    public float size = 0.5f;
    public Color pointColor = Color.yellow;
    public bool generateOnStart = true;
    public float gridSize = 10f;

    private ComputeBuffer _pointBuffer;
    private Mesh _quadMesh;
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private Vector3 _lastScale;
    private bool _needsUpdate = false;

    // Structure correspondant à celle dans le shader
    struct TestPoint
    {
        public Vector3 position;
        public Vector3 color;
        public float size;
    }

    void OnEnable()
    {
        CreateQuadMesh();
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
        _lastScale = transform.localScale;
        if (generateOnStart)
            GeneratePointsGrid();
    }

    void CreateQuadMesh()
    {
        _quadMesh = new Mesh();
        _quadMesh.vertices = new Vector3[4] {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        _quadMesh.uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        _quadMesh.triangles = new int[6] { 0, 1, 2, 2, 1, 3 };
        _quadMesh.RecalculateBounds();
    }

    public void GenerateRandomPoints()
    {
        // Nettoyer l'ancien buffer s'il existe
        if (_pointBuffer != null)
        {
            _pointBuffer.Release();
            _pointBuffer = null;
        }

        // Créer un nouveau buffer
        _pointBuffer = new ComputeBuffer(pointCount, 28); // 28 = sizeof(PointData)

        // Génération des points de test
        var points = new TestPoint[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            Vector3 pos = Random.insideUnitSphere * (gridSize * 0.5f);
            Vector3 col = new Vector3(
                Random.value * pointColor.r,
                Random.value * pointColor.g,
                Random.value * pointColor.b
            );

            points[i] = new TestPoint
            {
                // Points en position locale, pas en position monde
                position = pos,
                color = col,
                size = size
            };
        }
        _pointBuffer.SetData(points);

        Debug.Log($"Generated {pointCount} random test points");

        // Sauvegarde de la transformation actuelle
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
        _lastScale = transform.localScale;
        _needsUpdate = false;
    }

    // Grille de points très visible pour tester le rendu
    public void GeneratePointsGrid()
    {
        // Nettoyer l'ancien buffer
        if (_pointBuffer != null)
        {
            _pointBuffer.Release();
            _pointBuffer = null;
        }

        _pointBuffer = new ComputeBuffer(pointCount, 28);
        var points = new TestPoint[pointCount];

        // Calculer le nombre de points par côté
        int sideCount = Mathf.CeilToInt(Mathf.Pow(pointCount, 1f / 3f));
        float spacing = gridSize / sideCount;
        float offset = -gridSize / 2;

        int index = 0;

        for (int x = 0; x < sideCount && index < pointCount; x++)
        {
            for (int y = 0; y < sideCount && index < pointCount; y++)
            {
                for (int z = 0; z < sideCount && index < pointCount; z++)
                {
                    Vector3 pos = new Vector3(
                        offset + x * spacing,
                        offset + y * spacing,
                        offset + z * spacing
                    );

                    Vector3 col = new Vector3(
                        (float)x / sideCount,
                        (float)y / sideCount,
                        (float)z / sideCount
                    );

                    points[index] = new TestPoint
                    {
                        // Points en position locale
                        position = pos,
                        color = col,
                        size = size
                    };
                    index++;
                }
            }
        }

        _pointBuffer.SetData(points);
        Debug.Log($"Generated {index} grid test points");

        // Sauvegarde de la transformation actuelle
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
        _lastScale = transform.localScale;
        _needsUpdate = false;
    }

    // Générer une sphère de points
    public void GenerateSpherePoints()
    {
        if (_pointBuffer != null)
        {
            _pointBuffer.Release();
            _pointBuffer = null;
        }

        _pointBuffer = new ComputeBuffer(pointCount, 28);
        var points = new TestPoint[pointCount];

        // Générer des points sur une sphère
        float radius = gridSize / 2f;
        float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
        float angleIncrement = Mathf.PI * 2 * goldenRatio;

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / pointCount;
            float inclination = Mathf.Acos(1 - 2 * t);
            float azimuth = angleIncrement * i;

            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);

            Vector3 pos = new Vector3(x, y, z) * radius;

            // Couleur basée sur la position normalisée
            Vector3 col = new Vector3(
                (x + 1) * 0.5f,
                (y + 1) * 0.5f,
                (z + 1) * 0.5f
            );

            points[i] = new TestPoint
            {
                position = pos,
                color = Vector3.Scale(col, new Vector3(pointColor.r, pointColor.g, pointColor.b)),
                size = size
            };
        }

        _pointBuffer.SetData(points);
        Debug.Log($"Generated {pointCount} sphere test points");

        // Sauvegarde de la transformation actuelle
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
        _lastScale = transform.localScale;
        _needsUpdate = false;
    }

    void OnDisable()
    {
        if (_pointBuffer != null)
        {
            _pointBuffer.Release();
            _pointBuffer = null;
        }
    }

    void Update()
    {
        // Si l'objet a bougé, marquer comme nécessitant une mise à jour
        if (transform.position != _lastPosition ||
            transform.rotation != _lastRotation ||
            transform.localScale != _lastScale)
        {
            _needsUpdate = true;
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
            _lastScale = transform.localScale;
        }

        if (_pointBuffer == null || pointMaterial == null)
            return;

        // Vérifier le mesh
        if (_quadMesh == null)
            CreateQuadMesh();

        // Si besoin de mise à jour, créer la matrice de transformation
        Matrix4x4 transformMatrix = Matrix4x4.identity;
        if (_needsUpdate)
        {
            transformMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            pointMaterial.SetMatrix("_TransformMatrix", transformMatrix);
            _needsUpdate = false;
        }
        else
        {
            // Assurer que la matrice est définie même sans mouvement
            transformMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            pointMaterial.SetMatrix("_TransformMatrix", transformMatrix);
        }

        // Définir les autres propriétés matériau
        pointMaterial.SetBuffer("_PointBuffer", _pointBuffer);
        pointMaterial.SetFloat("_PointSize", size);
        pointMaterial.SetColor("_Color", pointColor);

        // Dessiner avec méthode explicite pour éviter le culling
        Graphics.DrawMeshInstancedProcedural(
            _quadMesh,
            0,
            pointMaterial,
            new Bounds(transform.position, Vector3.one * gridSize * 2),
            pointCount,
            null,
            UnityEngine.Rendering.ShadowCastingMode.Off,
            true,
            gameObject.layer,
            Camera.main
        );
    }

    // Pour voir les points dans la scène
    void OnDrawGizmos()
    {
        // Dessiner juste le volume occupé par les points
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * gridSize);
    }

    void OnDrawGizmosSelected()
    {
        // Dessiner quelques points si buffer actif
        if (_pointBuffer != null && pointCount <= 100) // Limité pour des raisons de performance
        {
            Gizmos.color = Color.green;
            TestPoint[] points = new TestPoint[pointCount];
            _pointBuffer.GetData(points);

            Matrix4x4 transformMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

            foreach (var point in points)
            {
                Vector3 worldPos = transformMatrix.MultiplyPoint3x4(point.position);
                Gizmos.DrawSphere(worldPos, 0.1f);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TestGameViewPoints))]
public class TestGameViewPointsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TestGameViewPoints generator = (TestGameViewPoints)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generate Test Points", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate Grid Points"))
        {
            generator.GeneratePointsGrid();
        }

        if (GUILayout.Button("Generate Random Points"))
        {
            generator.GenerateRandomPoints();
        }

        if (GUILayout.Button("Generate Sphere Points"))
        {
            generator.GenerateSpherePoints();
        }

        EditorGUILayout.HelpBox(
            "Ce script génère des points de test qui suivent le mouvement et la rotation de l'objet.",
            MessageType.Info);
    }
}
#endif