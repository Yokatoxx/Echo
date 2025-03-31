Shader "Custom/SimplePointCloudHDRP"
{
    Properties
    {
        _PointSize("Point Size", Range(0.001, 1.0)) = 0.05
        _PointColor("Point Color", Color) = (1, 1, 1, 1)
        _DepthOffset("Depth Offset", Range(0.0, 0.1)) = 0.001
        [Toggle] _AlwaysOnTop("Always On Top", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "HighDefinitionPipeline" "Queue" = "Transparent" }
        
        Pass
        {
            Tags { "LightMode" = "ForwardOnly" }
            
            ZWrite On
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha // Mode transparent
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            struct Attributes
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            StructuredBuffer<float4> _PositionBuffer;
            float _PointSize;
            float4 _PointColor;
            float _DepthOffset;
            float _AlwaysOnTop;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Récupérer la position du point depuis le buffer
                float4 pointData = _PositionBuffer[input.instanceID];
                float3 position = pointData.xyz;
                
                // Créer un billboard (quad toujours orienté vers la caméra)
                float3 right = normalize(UNITY_MATRIX_V[0].xyz);
                float3 up = normalize(UNITY_MATRIX_V[1].xyz);
                float3 forward = normalize(UNITY_MATRIX_V[2].xyz);
                
                // Ajouter un petit décalage dans la direction de la normale pour éviter le z-fighting
                position += forward * _DepthOffset;
                
                // Positionner le vertex
                float3 positionWS = position 
                                   + right * input.position.x * _PointSize
                                   + up * input.position.y * _PointSize;
                
                // Transformer en clip space
                output.positionCS = TransformWorldToHClip(positionWS);
                
                // Si Always On Top est activé, placer au premier plan
                if (_AlwaysOnTop > 0.5)
                    output.positionCS.z = lerp(output.positionCS.z, 0.0, 0.9);
                
                output.uv = input.uv;
                output.color = _PointColor;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Créer un point rond
                float dist = length(input.uv - 0.5) * 2;
                clip(1 - dist);
                
                float alpha = _PointColor.a * smoothstep(1.0, 0.8, dist);
                return float4(_PointColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}