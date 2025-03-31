Shader "Custom/HDRPPointCloud"
{
    Properties
    {
        _DefaultPointSize ("Default Point Size", Range(0.001, 0.1)) = 0.01
        _DefaultPointColor ("Default Point Color", Color) = (1, 1, 1, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="HDRenderPipeline" }
        Cull Off
        
        Pass
        {
            Name "PointCloudPass"
            
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            
            struct CloudPointData  // Renommé pour correspondre au compute shader
            {
                float3 position;
                float3 color;
                float size;
            };
            
            struct v2f
            {
                float4 position : SV_POSITION;
                float pSize : PSIZE;
                float4 color : COLOR;
            };
            
            StructuredBuffer<CloudPointData> _PointBuffer;
            float _DefaultPointSize;
            float4 _DefaultPointColor;
            
            v2f vert(uint vertexID : SV_VertexID)
            {
                v2f o;
                
                CloudPointData cloudData = _PointBuffer[vertexID];
                
                float4 worldPos = float4(cloudData.position, 1.0);
                o.position = TransformWorldToHClip(worldPos.xyz);
                
                // Calculer la taille du point en fonction de la distance et de la résolution d'écran
                float distanceScale = 600.0 / o.position.w; 
                o.pSize = (cloudData.size > 0 ? cloudData.size : _DefaultPointSize) * distanceScale;
                
                // Utiliser la couleur du point ou la couleur par défaut
                float3 pointColor = cloudData.color.r > 0 || cloudData.color.g > 0 || cloudData.color.b > 0 ? 
                              cloudData.color : _DefaultPointColor.rgb;
                o.color = float4(pointColor, 1.0);
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDHLSL
        }
    }
}