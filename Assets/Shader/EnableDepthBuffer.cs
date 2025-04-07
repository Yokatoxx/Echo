using UnityEngine;

[RequireComponent(typeof(Camera))]
public class EnableDepthBuffer : MonoBehaviour
{
    void Start()
    {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}