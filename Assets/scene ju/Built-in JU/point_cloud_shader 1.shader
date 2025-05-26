// Safe Optimized Point Cloud Shader
Shader "Optimized/point_cloud_shader_safe"
{
    Properties
    {
        [HDR]_MainColor("MainColor", Color) = (0.9559748,0.1412918,0.1412918,0)
        _A_tex("A_tex", 2D) = "white" {}
        _Float0("Float 0", Float) = 0
        _Cutoff("Mask Clip Value", Float) = 0.5
        _size("size", Float) = 1
        _Noise_Scale("Noise_Scale", Float) = 1
        _EmissiveIntensity("Emissive Intensity", Range(0, 5)) = 1
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }

    SubShader
    {
        Tags{ "RenderType" = "TransparentCutout" "Queue" = "AlphaTest+0" "IsEmissive" = "true" }
        Cull Back
        CGPROGRAM
        #include "UnityShaderVariables.cginc"
        #pragma target 3.0
        #pragma surface surf Unlit keepalpha noshadow vertex:vertexDataFunc 
        
        struct Input
        {
            float3 worldPos;
            float2 uv_texcoord;
        };

        uniform float4 _MainColor;
        uniform float _Float0;
        uniform float _Noise_Scale;
        uniform sampler2D _A_tex;
        uniform float4 _A_tex_ST;
        uniform float _size;
        uniform float _Cutoff;
        uniform float _EmissiveIntensity;

        // Garde le snoise original mais avec quelques optimisations mineures
        float3 mod3D289(float3 x) { return x - floor(x * (1.0/289.0)) * 289.0; }
        float4 mod3D289(float4 x) { return x - floor(x * (1.0/289.0)) * 289.0; }
        float4 permute(float4 x) { return mod3D289((x * 34.0 + 1.0) * x); }
        float4 taylorInvSqrt(float4 r) { return 1.79284291400159 - r * 0.85373472095314; }

        float snoise(float3 v)
        {
            const float2 C = float2(1.0/6.0, 1.0/3.0);
            float3 i = floor(v + dot(v, C.yyy));
            float3 x0 = v - i + dot(i, C.xxx);
            float3 g = step(x0.yzx, x0.xyz);
            float3 l = 1.0 - g;
            float3 i1 = min(g.xyz, l.zxy);
            float3 i2 = max(g.xyz, l.zxy);
            float3 x1 = x0 - i1 + C.xxx;
            float3 x2 = x0 - i2 + C.yyy;
            float3 x3 = x0 - 0.5;
            i = mod3D289(i);
            float4 p = permute(permute(permute(i.z + float4(0.0, i1.z, i2.z, 1.0)) + i.y + float4(0.0, i1.y, i2.y, 1.0)) + i.x + float4(0.0, i1.x, i2.x, 1.0));
            float4 j = p - 49.0 * floor(p * (1.0/49.0));
            float4 x_ = floor(j * (1.0/7.0));
            float4 y_ = floor(j - 7.0 * x_);
            float4 x = (x_ * 2.0 + 0.5) * (1.0/7.0) - 1.0;
            float4 y = (y_ * 2.0 + 0.5) * (1.0/7.0) - 1.0;
            float4 h = 1.0 - abs(x) - abs(y);
            float4 b0 = float4(x.xy, y.xy);
            float4 b1 = float4(x.zw, y.zw);
            float4 s0 = floor(b0) * 2.0 + 1.0;
            float4 s1 = floor(b1) * 2.0 + 1.0;
            float4 sh = -step(h, 0.0);
            float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
            float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
            float3 g0 = float3(a0.xy, h.x);
            float3 g1 = float3(a0.zw, h.y);
            float3 g2 = float3(a1.xy, h.z);
            float3 g3 = float3(a1.zw, h.w);
            float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
            g0 *= norm.x;
            g1 *= norm.y;
            g2 *= norm.z;
            g3 *= norm.w;
            float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
            m = m * m;
            m = m * m;
            float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
            return 42.0 * dot(m, px);
        }

        void vertexDataFunc(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Exactement comme l'original
            float3 appendResult4 = float3((float2(-0.5, -0.5) + (v.texcoord.xy - float2(0, 0)) * (float2(0.5, 0.5) - float2(-0.5, -0.5)) / (float2(1, 1) - float2(0, 0))), 0.0);
            float3 normalizeResult9 = normalize(mul(float4(mul(float4(appendResult4, 0.0), UNITY_MATRIX_V).xyz, 0.0), unity_ObjectToWorld).xyz);
            v.vertex.xyz += normalizeResult9;
            v.vertex.w = 1;
        }

        inline half4 LightingUnlit(SurfaceOutput s, half3 lightDir, half atten)
        {
            return half4(0, 0, 0, s.Alpha);
        }

        void surf(Input i, inout SurfaceOutput o)
        {
            float3 ase_worldPos = i.worldPos;
            
            // Cache le calcul du temps
            float mulTime20 = _Time.y * _Float0;
            float4 appendResult24 = float4(ase_worldPos.x, ase_worldPos.y + mulTime20, ase_worldPos.z, 0.0);
            float simplePerlin3D22 = snoise(appendResult24.xyz * _Noise_Scale);
            
            // Application de l'intensité émissive
            o.Emission = (_MainColor * simplePerlin3D22 * _EmissiveIntensity).rgb;
            o.Alpha = 1;
            
            float2 uv_A_tex = i.uv_texcoord * _A_tex_ST.xy + _A_tex_ST.zw;
            clip(((tex2D(_A_tex, uv_A_tex).r * _size) * simplePerlin3D22) - _Cutoff);
        }

        ENDCG
    }
    CustomEditor "ASEMaterialInspector"
}