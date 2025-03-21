Shader "Custom/CompatiblePointCloudShader"
{
    Properties
    {
        _PointSize ("Point Size", Range(0.001, 0.05)) = 0.01
        _PointDensity ("Point Density", Range(1, 20)) = 10
        _MainTex ("Texture", 2D) = "white" {}
        _ColorNear ("Color Near", Color) = (1,0,0,1)
        _ColorFar ("Color Far", Color) = (0,0,1,1)
        _DepthRange ("Depth Range", Range(1, 100)) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        Pass
        {
            // Enable point rendering mode
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2g
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };
            
            struct g2f
            {
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float depth : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _PointSize;
            float _PointDensity;
            float4 _ColorNear;
            float4 _ColorFar;
            float _DepthRange;
            
            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.normal = v.normal;
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }
            
            float random(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            [maxvertexcount(20)]
            void geom(triangle v2g input[3], inout PointStream<g2f> outStream)
            {
                float3 pos0 = input[0].vertex.xyz;
                float3 pos1 = input[1].vertex.xyz;
                float3 pos2 = input[2].vertex.xyz;
                
                float3 center = (pos0 + pos1 + pos2) / 3.0;
                float area = length(cross(pos1 - pos0, pos2 - pos0)) * 0.5;
                
                int points = clamp(int(area * _PointDensity * 200), 1, 20);
                
                for (int i = 0; i < points; i++)
                {
                    float r1 = random(float2(i, area * 1000));
                    float r2 = random(float2(i + 1, area * 2000));
                    
                    float sqrtR1 = sqrt(r1);
                    float u = 1.0 - sqrtR1;
                    float v = r2 * sqrtR1;
                    float w = 1.0 - u - v;
                    
                    float3 pos = pos0 * u + pos1 * v + pos2 * w;
                    float3 normal = normalize(input[0].normal * u + input[1].normal * v + input[2].normal * w);
                    float2 uv = input[0].uv * u + input[1].uv * v + input[2].uv * w;
                    float4 worldPos = input[0].worldPos * u + input[1].worldPos * v + input[2].worldPos * w;
                    
                    float depth = distance(worldPos.xyz, _WorldSpaceCameraPos);
                    
                    // Create a new vertex at the calculated position
                    g2f o;
                    float4 clipPos = UnityObjectToClipPos(float4(pos, 1.0));
                    
                    // Set final position - this creates an actual quad
                    o.pos = clipPos;
                    o.normal = UnityObjectToWorldNormal(normal);
                    o.uv = TRANSFORM_TEX(uv, _MainTex);
                    o.depth = depth;
                    
                    outStream.Append(o);
                }
            }
            
            fixed4 frag(g2f i) : SV_Target
            {
                // We'll create a circular point using fragment shader
                // Distance from center of the point
                float2 center = float2(0.5, 0.5);
                float2 uv = i.uv;
                
                // Base color from texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Blend between near and far colors based on depth
                fixed4 finalColor = lerp(_ColorNear, _ColorFar, saturate(i.depth / _DepthRange));
                
                return finalColor * col;
            }
            ENDCG
        }
    }
}