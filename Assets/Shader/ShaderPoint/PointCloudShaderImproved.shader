Shader "Custom/SimplifiedPointCloudShader"
{
    Properties
    {
        _PointSize ("Point Size", Range(0.001, 0.1)) = 0.01
        _PointScale ("Point Scale", Range(0.1, 10.0)) = 1.0
        _PointDensity ("Point Density", Range(1, 20)) = 10
        _MainTex ("Texture", 2D) = "white" {}
        
        _ColorNear ("Color Near", Color) = (1,0,0,1)
        _ColorFar ("Color Far", Color) = (0,0,1,1)
        _DepthRange ("Depth Range", Range(1, 100)) = 10
        _UseVertexColor ("Use Vertex Colors", Range(0, 1)) = 0
        
        _PointJitter ("Point Position Jitter", Range(0, 1)) = 0.1
        
        _AnimSpeed ("Animation Speed", Range(0, 5)) = 1
        _AnimAmplitude ("Animation Amplitude", Range(0, 0.5)) = 0.05
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 5.0
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2g
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 worldPos : TEXCOORD1;
                float noise : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct g2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float depth : TEXCOORD1;
                float2 pointCenter : TEXCOORD2;
                float noise : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _PointSize;
            float _PointScale;
            float _PointDensity;
            float4 _ColorNear;
            float4 _ColorFar;
            float _DepthRange;
            float _UseVertexColor;
            float _PointJitter;
            float _AnimSpeed;
            float _AnimAmplitude;
            float _NoiseScale;
            
            float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
            float3 permute(float3 x) { return mod289(((x*34.0)+1.0)*x); }
            
            // Implémentation de bruit pour les animations
            float snoise(float2 v) {
                const float4 C = float4(0.211324865405187, 0.366025403784439,
                         -0.577350269189626, 0.024390243902439);
                float2 i  = floor(v + dot(v, C.yy));
                float2 x0 = v -   i + dot(i, C.xx);
                float2 i1;
                i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
                float4 x12 = x0.xyxy + C.xxzz;
                x12.xy -= i1;
                i = mod289(i);
                float3 p = permute(permute(i.y + float3(0.0, i1.y, 1.0))
                      + i.x + float3(0.0, i1.x, 1.0));
                float3 m = max(0.5 - float3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
                m = m*m;
                m = m*m;
                float3 x = 2.0 * frac(p * C.www) - 1.0;
                float3 h = abs(x) - 0.5;
                float3 ox = floor(x + 0.5);
                float3 a0 = x - ox;
                m *= 1.79284291400159 - 0.85373472095314 * (a0*a0 + h*h);
                float3 g;
                g.x  = a0.x  * x0.x  + h.x  * x0.y;
                g.yz = a0.yz * x12.xz + h.yz * x12.yw;
                return 130.0 * dot(m, g);
            }
            
            // Fonctions de génération de nombres pseudo-aléatoires
            float random(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float3 random3(float3 p) {
                return frac(sin(float3(
                    dot(p, float3(127.1, 311.7, 74.7)),
                    dot(p, float3(269.5, 183.3, 246.1)),
                    dot(p, float3(113.5, 271.9, 124.6))
                )) * 43758.5453123);
            }
            
            v2g vert(appdata v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                // Animation du vertex avec du bruit
                float noise = snoise(worldPos.xz * _NoiseScale + _Time.y * _AnimSpeed) * 0.5 + 0.5;
                float totalAnim = _AnimAmplitude * noise;
                v.vertex.xyz += v.normal * totalAnim;
                
                o.vertex = v.vertex;
                o.normal = v.normal;
                o.uv = v.uv;
                o.color = v.color;
                o.worldPos = worldPos;
                o.noise = noise;
                return o;
            }
            
            [maxvertexcount(20)]
            void geom(triangle v2g input[3], inout PointStream<g2f> outStream)
            {
                UNITY_SETUP_INSTANCE_ID(input[0]);

                // Calcul du centre et de l'aire du triangle
                float3 pos0 = input[0].vertex.xyz;
                float3 pos1 = input[1].vertex.xyz;
                float3 pos2 = input[2].vertex.xyz;
                
                float3 center = (pos0 + pos1 + pos2) / 3.0;
                float area = length(cross(pos1 - pos0, pos2 - pos0)) * 0.5;
                
                // Nombre de points à générer en fonction de l'aire
                int points = clamp(int(area * _PointDensity * 200), 1, 20);
                
                for (int i = 0; i < points; i++)
                {
                    // Technique de distribution des points par coordonnées barycentriques
                    float r1 = random(float2(i, area * 1000));
                    float r2 = random(float2(i + 1, area * 2000));
                    
                    float sqrtR1 = sqrt(r1);
                    float u = 1.0 - sqrtR1;
                    float v = r2 * sqrtR1;
                    float w = 1.0 - u - v;
                    
                    // Interpolation des attributs en utilisant les coordonnées barycentriques
                    float3 pos = pos0 * u + pos1 * v + pos2 * w;
                    float3 normal = normalize(input[0].normal * u + input[1].normal * v + input[2].normal * w);
                    float2 uv = input[0].uv * u + input[1].uv * v + input[2].uv * w;
                    float4 color = input[0].color * u + input[1].color * v + input[2].color * w;
                    float4 worldPos = input[0].worldPos * u + input[1].worldPos * v + input[2].worldPos * w;
                    float noise = input[0].noise * u + input[1].noise * v + input[2].noise * w;
                    
                    // Ajout d'un bruit de position
                    if (_PointJitter > 0) {
                        float3 jitter = random3(pos + i) * 2 - 1;
                        pos += jitter * _PointJitter * 0.02;
                    }
                    
                    float depth = distance(worldPos.xyz, _WorldSpaceCameraPos);
                    float depthFactor = saturate(depth / _DepthRange);
                    
                    // Calcul dynamique de la taille du point avec variations
                    float finalPointSize = _PointSize * _PointScale;
                    float depthScaling = lerp(1.1, 0.9, depthFactor);
                    float noiseScaling = lerp(0.95, 1.05, noise);
                    finalPointSize *= depthScaling * noiseScaling;
                    
                    g2f o;
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    
                    float4 clipPos = UnityObjectToClipPos(float4(pos, 1.0));
                    o.pointCenter = clipPos.xy / clipPos.w;
                    o.vertex = clipPos;
                    
                    o.normal = UnityObjectToWorldNormal(normal);
                    o.uv = TRANSFORM_TEX(uv, _MainTex);
                    o.color = color;
                    o.depth = depth;
                    o.noise = noise;
                    
                    outStream.Append(o);
                }
            }
            
            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Mélange de couleurs basé sur la profondeur
                fixed4 depthColor = lerp(_ColorNear, _ColorFar, saturate(i.depth / _DepthRange));
                fixed4 finalColor = lerp(depthColor, i.color, _UseVertexColor);
                
                // Variation de couleur basée sur le bruit
                finalColor = lerp(finalColor, finalColor * (0.8 + 0.4 * i.noise), 0.3);
                
                return finalColor * col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
