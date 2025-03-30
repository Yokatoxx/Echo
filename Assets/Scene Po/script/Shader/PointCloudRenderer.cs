using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudRenderer : MonoBehaviour
{
    public Material pointCloudMaterial;
    public float chaos = 0.0f;
    public float pointSize = 1.0f;

    Mesh mesh;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    void Update()
    {
        pointCloudMaterial.SetFloat("_Chaos", chaos);
        pointCloudMaterial.SetFloat("_PointSize", pointSize);

        Graphics.DrawMesh(mesh, transform.localToWorldMatrix, pointCloudMaterial, 0, null, 0, null, false, false, false);
    }
}
