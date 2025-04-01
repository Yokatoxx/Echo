Shader "Custom/IntersectionGlow"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,0,0,1)
        _GlowThreshold ("Glow Threshold", Range(0, 0.1)) = 0.05
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            sampler2D _CameraDepthTexture;
            float4 _MainColor;
            float4 _GlowColor;
            float _GlowThreshold;
            float _GlowIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calcul des coordonnées UV écran
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                // Récupération de la profondeur de la scène
                float sceneDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, screenUV).r);
                float objectDepth = LinearEyeDepth(i.screenPos.w);

                // Calcul de la différence de profondeur
                float depthDifference = objectDepth - sceneDepth;
                float glowFactor = 1 - smoothstep(0, _GlowThreshold, depthDifference);
                glowFactor *= _GlowIntensity;

                // Mélange des couleurs
                float4 finalColor = _MainColor;
                finalColor.rgb = lerp(finalColor.rgb, _GlowColor.rgb, glowFactor * _GlowColor.a);

                return finalColor;
            }
            ENDCG
        }
    }
}