Shader "Custom/SonarWaveShader"
{
    Properties
    {
        _Color ("Base Color", Color) = (0,0,0,0)
        _WaveColor ("Wave Color", Color) = (0,1,0.8,1)
        
        // Contrôle de la vague
        _WaveWidth ("Wave Width", Range(0.001, 0.5)) = 0.05
        _WaveSharpness ("Wave Sharpness", Range(1.0, 10.0)) = 3.0
        
        // Dégradé de la vague
        _WaveFadeDistance ("Wave Fade Distance", Range(0.0, 1.0)) = 0.2
        
        // Effet de trainée optionnel
        [Toggle] _UseTrail ("Use Trail Effect", Float) = 0
        _TrailLength ("Trail Length", Range(0.0, 0.5)) = 0.1
        _TrailFade ("Trail Fade", Range(1.0, 5.0)) = 2.0
        
        // Ajustement d'intensité
        _Intensity ("Wave Intensity", Range(0.1, 5.0)) = 1.0
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

            float4 _Color;
            float4 _WaveColor;
            float _WaveWidth;
            float _WaveSharpness;
            float _WaveFadeDistance;
            float _UseTrail;
            float _TrailLength;
            float _TrailFade;
            float _Intensity;
            sampler2D _CameraDepthTexture;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Couleur de base (généralement transparente pour un sonar)
                fixed4 col = _Color;
                
                // Obtenir la profondeur de la scène
                float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                
                // Calculer la différence de profondeur pour détecter les intersections
                float depthDifference = abs(sceneDepth - i.eyeDepth);
                
                // Créer la vague principale
                float waveCenter = _WaveWidth * 0.5;
                float wavePeak = saturate(1.0 - abs(depthDifference - waveCenter) / (_WaveWidth * 0.5));
                wavePeak = pow(wavePeak, _WaveSharpness) * _Intensity;
                
                // Appliquer le dégradé en fonction de la distance
                float distanceFactor = saturate(1.0 - (depthDifference / _WaveFadeDistance));
                wavePeak *= distanceFactor;
                
                // Effet de trainée optionnel
                float trail = 0;
                if (_UseTrail > 0.5) {
                    // La trainée apparaît seulement derrière la vague principale (dans les valeurs de profondeur plus petites)
                    float trailFactor = saturate(1.0 - max(0, (depthDifference - waveCenter) / _TrailLength));
                    trail = pow(trailFactor, _TrailFade) * 0.5 * wavePeak;
                }
                
                // Combiner la vague principale et la trainée
                float finalEffect = max(wavePeak, trail);
                
                // Appliquer la couleur de la vague
                fixed4 finalColor = lerp(col, _WaveColor, finalEffect);
                // Ajuster l'alpha pour un meilleur contrôle de la transparence
                finalColor.a = finalEffect * _WaveColor.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
}