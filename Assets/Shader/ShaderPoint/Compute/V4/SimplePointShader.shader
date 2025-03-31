Shader "Custom/SimplePointShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PointSize ("Point Size", Range(0.001, 0.05)) = 0.01
        _ColorNear ("Color Near", Color) = (1,0,0,1)
        _ColorFar ("Color Far", Color) = (0,0,1,1)
        _DepthRange ("Depth Range", Range(1, 100)) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float depth : TEXCOORD1;
                float4 color : TEXCOORD2;
            };
            
            struct PointData
            {
                float3 position;
                float3 normal;
                float2 uv;
                float depth;
            };
            
            StructuredBuffer<PointData> _PointBuffer;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _PointSize;
            float4 _ColorNear;
            float4 _ColorFar;
            float _DepthRange;
            
            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                PointData pointData = _PointBuffer[instanceID];
                
                v2f o;
                
                // Calculate world position of instance
                float3 worldPos = pointData.position;
                
                // Calculate billboard vertices
                float3 localPos = v.vertex.xyz * _PointSize;
                float3 camRight = UNITY_MATRIX_IT_MV[0].xyz;
                float3 camUp = UNITY_MATRIX_IT_MV[1].xyz;
                float3 position = worldPos + camRight * localPos.x + camUp * localPos.y;
                
                o.pos = UnityWorldToClipPos(position);
                o.uv = v.uv;
                o.depth = pointData.depth;
                o.color = lerp(_ColorNear, _ColorFar, saturate(pointData.depth / _DepthRange));
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Circle mask
                float dist = distance(i.uv, float2(0.5, 0.5));
                clip(0.5 - dist);
                
                return i.color;
            }
            ENDCG
        }
    }
}