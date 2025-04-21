Shader "Custom/IntersectionGlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _IntersectionColor ("Intersection Color", Color) = (1,0,0,1)
        _IntersectionPower ("Intersection Power", Range(0.01, 10.0)) = 1.0
        _IntersectionThreshold ("Intersection Threshold", Range(0.01, 1.0)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Cull Off
        
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
                float eyeDepth : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _IntersectionColor;
            float _IntersectionPower;
            float _IntersectionThreshold;
            sampler2D _CameraDepthTexture;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Obtenez la profondeur de la scène
                float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                
                // Calculez la différence entre les profondeurs
                float depthDifference = abs(sceneDepth - i.eyeDepth);
                
                // Si la différence est inférieure à un seuil, appliquez l'effet
                float intersection = 1 - saturate(depthDifference / _IntersectionThreshold);
                
                // Appliquez l'effet d'intersection
                intersection = pow(intersection, _IntersectionPower);
                
                fixed4 finalColor = lerp(col, _IntersectionColor, intersection * _IntersectionColor.a);
                
                return finalColor;
            }
            ENDCG
        }
    }
}