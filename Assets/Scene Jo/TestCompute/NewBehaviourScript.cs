using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class PointCloudGeneratorV6 : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material pointMaterial;
    [Range(1, 10)] public float density = 1.0f;

    private ComputeBuffer _positionsBuffer;
    private ComputeBuffer _verticesBuffer;
    private int _kernelHandle;
    private Mesh _mesh;
    private int _pointCount;

    void Start()
    {
        _mesh = GetComponent<MeshFilter>().mesh;
        SetupPointCloud();
    }

    void SetupPointCloud()
    {
        // Vérifier que le mesh est lisible
        if (!_mesh.isReadable)
        {
            Debug.LogError("Activer 'Read/Write' dans les import settings du mesh!");
            return;
        }

        // Calculer le nombre de points basé sur la densité
        _pointCount = Mathf.CeilToInt(_mesh.vertexCount * density);

        InitializeBuffers();
        SetupShader();
    }

    void InitializeBuffers()
    {
        _positionsBuffer = new ComputeBuffer(_pointCount, sizeof(float) * 3);
        _verticesBuffer = new ComputeBuffer(_mesh.vertices.Length, sizeof(float) * 3);
        _verticesBuffer.SetData(_mesh.vertices);
    }

    void SetupShader()
    {
        _kernelHandle = computeShader.FindKernel("CSMain");

        computeShader.SetBuffer(_kernelHandle, "Positions", _positionsBuffer);
        computeShader.SetBuffer(_kernelHandle, "Vertices", _verticesBuffer);
        computeShader.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
        computeShader.SetInt("_VertexCount", _mesh.vertices.Length);
        computeShader.SetInt("_PointCount", _pointCount);

        computeShader.Dispatch(_kernelHandle, Mathf.CeilToInt(_pointCount / 64f), 1, 1);
    }

    void OnRenderObject()
    {
        pointMaterial.SetBuffer("_Positions", _positionsBuffer);
        pointMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, _pointCount);
    }

    void OnDestroy()
    {
        _positionsBuffer?.Release();
        _verticesBuffer?.Release();
    }
}