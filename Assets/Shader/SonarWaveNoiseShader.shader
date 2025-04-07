Shader "Custom/SonarWaveNoiseShader"
{
    Properties
    {
        _Color ("Base Color", Color) = (0,0,0,0)
        _WaveColor ("Wave Color", Color) = (0,1,0.8,1)
        
        // Contrôle de la vague
        _WaveWidth ("Wave Width", Range(0.001, 0.5)) = 0.05
        _WaveSharpness ("Wave Sharpness", Range(1.0, 10.0)) = 3.0
        _WaveFadeDistance ("Wave Fade Distance", Range(0.0, 1.0)) = 0.2
        
        // Propriétés du bruit
        [Toggle] _UseNoise ("Use Noise", Float) = 1
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0.0, 1.0)) = 0.3
        _NoiseScale ("Noise Scale", Range(0.1, 10.0)) = 1.0
        _NoiseSpeed ("Noise Animation Speed", Range(0.0, 5.0)) = 0.5
        
        // Effet de trainée
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
                float3 worldPos : TEXCOORD3;
            };

            float4 _Color;
            float4 _WaveColor;
            float _WaveWidth;
            float _WaveSharpness;
            float _WaveFadeDistance;
            float _UseNoise;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _NoiseStrength;
            float _NoiseScale;
            float _NoiseSpeed;
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
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
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
                
                // Appliquer le bruit à la différence de profondeur si activé
                if (_UseNoise > 0.5) {
                    // Créer des coordonnées UV animées pour le bruit
                    float2 noiseUV = i.worldPos.xz * _NoiseScale;
                    noiseUV += _Time.y * _NoiseSpeed; // Animation du bruit
                    
                    // Échantillonner la texture de bruit
                    float noise = tex2D(_NoiseTex, noiseUV).r * 2.0 - 1.0; // Normaliser à [-1, 1]
                    
                    // Appliquer le bruit à la différence de profondeur
                    depthDifference += noise * _NoiseStrength * _WaveWidth;
                }
                
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
                    // La trainée apparaît seulement derrière la vague principale
                    float trailFactor = saturate(1.0 - max(0, (depthDifference - waveCenter) / _TrailLength));
                    trail = pow(trailFactor, _TrailFade) * 0.5 * wavePeak;
                }
                
                // Combiner la vague principale et la trainée
                float finalEffect = max(wavePeak, trail);
                
                // Appliquer la couleur de la vague
                fixed4 finalColor = lerp(col, _WaveColor, finalEffect);
                finalColor.a = finalEffect * _WaveColor.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
}