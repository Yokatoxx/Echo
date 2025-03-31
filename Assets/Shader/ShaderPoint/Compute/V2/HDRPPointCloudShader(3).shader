Shader "Custom/HDRPPointCloud"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 0.05
        _Color ("Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0.1, 5.0)) = 1.5
        [Toggle] _AlwaysVisible("Always Visible", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="HDRenderPipeline" "Queue"="AlphaTest" }
        
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            struct PointData
            {
                float3 position;
                float3 color;
                float size;
            };
            
            struct vertIn
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };
            
            struct vertOut
            {
                float4 positionCS : SV_POSITION;
                float3 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            StructuredBuffer<PointData> _PointBuffer;
            float _PointSize;
            float4 _Color;
            float _Brightness;
            float _AlwaysVisible;
            float4x4 _TransformMatrix; // Matrice pour suivre l'objet
            
            static const float2 quadPositions[4] = 
            {
                float2(-0.5, -0.5),
                float2(0.5, -0.5),
                float2(-0.5, 0.5),
                float2(0.5, 0.5)
            };
            
            static const float2 quadUVs[4] = 
            {
                float2(0, 0),
                float2(1, 0),
                float2(0, 1),
                float2(1, 1)
            };

            vertOut vert(vertIn input)
            {
                vertOut output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                uint pointId = input.instanceID;
                uint vertexId = input.vertexID;
                
                PointData pointData = _PointBuffer[pointId];
                
                // Appliquer la transformation pour suivre l'objet
                float3 pointPos = mul(_TransformMatrix, float4(pointData.position, 1.0)).xyz;
                float pointSize = pointData.size * _PointSize * 2.0;
                
                float3 camPosWS = GetCurrentViewPosition();
                float3 forward = normalize(camPosWS - pointPos);
                float3 right = normalize(cross(float3(0, 1, 0), forward));
                float3 up = normalize(cross(forward, right));
                
                float2 quadPos = quadPositions[vertexId];
                float3 vertexPos = pointPos + (quadPos.x * right + quadPos.y * up) * pointSize;
                
                output.positionCS = TransformWorldToHClip(vertexPos);
                
                if (_AlwaysVisible > 0.5)
                {
                    output.positionCS.z = 0.1;
                }
                
                output.color = pointData.color * _Color.rgb;
                output.uv = quadUVs[vertexId];
                output.positionWS = vertexPos;
                
                return output;
            }
            
            float4 frag(vertOut input) : SV_Target
            {
                float dist = length(input.uv - 0.5) * 2.0;
                if (dist > 1.0) discard;
                
                float rim = smoothstep(0.5, 1.0, dist);
                float3 color = lerp(input.color * _Brightness, input.color * (_Brightness * 0.5), rim);
                
                return float4(color, 1.0);
            }
            ENDHLSL
        }
        
        // Passe supplémentaire pour assurer la visibilité
        Pass
        {
            Name "DepthForwardOnly"
            Tags { "LightMode"="DepthForwardOnly" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            
            struct PointData
            {
                float3 position;
                float3 color;
                float size;
            };
            
            struct vertIn
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };
            
            struct vertOut
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            StructuredBuffer<PointData> _PointBuffer;
            float _PointSize;
            float4x4 _TransformMatrix;
            
            static const float2 quadPositions[4] = 
            {
                float2(-0.5, -0.5),
                float2(0.5, -0.5),
                float2(-0.5, 0.5),
                float2(0.5, 0.5)
            };
            
            static const float2 quadUVs[4] = 
            {
                float2(0, 0),
                float2(1, 0),
                float2(0, 1),
                float2(1, 1)
            };

            vertOut vert(vertIn input)
            {
                vertOut output;
                
                uint pointId = input.instanceID;
                uint vertexId = input.vertexID;
                
                PointData pointData = _PointBuffer[pointId];
                float3 pointPos = mul(_TransformMatrix, float4(pointData.position, 1.0)).xyz;
                float pointSize = pointData.size * _PointSize;
                
                float3 camPosWS = GetCurrentViewPosition();
                float3 forward = normalize(camPosWS - pointPos);
                float3 right = normalize(cross(float3(0, 1, 0), forward));
                float3 up = normalize(cross(forward, right));
                
                float2 quadPos = quadPositions[vertexId];
                float3 vertexPos = pointPos + (quadPos.x * right + quadPos.y * up) * pointSize;
                
                output.positionCS = TransformWorldToHClip(vertexPos);
                output.uv = quadUVs[vertexId];
                
                return output;
            }
            
            float4 frag(vertOut input) : SV_Target
            {
                float dist = length(input.uv - 0.5) * 2.0;
                if (dist > 1.0) discard;
                return float4(0, 0, 0, 1);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/InternalErrorShader"
}