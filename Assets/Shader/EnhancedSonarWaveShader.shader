Shader "Custom/EnhancedSonarWaveShader"
{
    Properties
    {
        [Header(Base Settings)]
        _Color ("Base Color", Color) = (0,0,0,0)
        _WaveColor ("Wave Color", Color) = (0,1,0.8,1)
        
        [Header(Width Control)]
        [Space(10)]
        _WaveWidth ("Base Wave Width", Range(0.001, 1.0)) = 0.05
        [Toggle] _UseVariableWidth ("Use Width Modifier", Float) = 0
        _WidthGradient ("Width Modifier", Range(0.0, 2.0)) = 1.0
        _MinWidth ("Minimum Width", Range(0.001, 0.5)) = 0.01
        
        [Header(Core Wave Properties)]
        [Space(10)]
        _WaveSharpness ("Wave Sharpness", Range(1.0, 30.0)) = 3.0
        _Intensity ("Wave Intensity", Range(0.1, 5.0)) = 1.0
        _WaveFadeDistance ("Wave Fade Distance", Range(0.0, 1.0)) = 0.2
        
        [Header(Visual Noise)]
        [Space(10)]
        [Toggle] _UseNoise ("Use Noise", Float) = 1
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0.0, 1.0)) = 0.3
        _NoiseScale ("Noise Scale", Range(0.1, 10.0)) = 1.0
        _NoiseSpeed ("Noise Animation Speed", Range(0.0, 5.0)) = 0.5
        
        [Header(Depth Adjustment)]
        [Space(10)]
        _DepthOffset ("Depth Offset", Range(-1.0, 1.0)) = 0.0
        _RadialFalloff ("Radial Falloff", Range(0.0, 5.0)) = 0.0
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
                float3 objectPos : TEXCOORD4;
            };

            float4 _Color;
            float4 _WaveColor;
            float _WaveWidth;
            float _UseVariableWidth;
            float _WidthGradient;
            float _MinWidth;
            float _WaveSharpness;
            float _WaveFadeDistance;
            float _Intensity;
            float _UseEdgeHighlight;
            float _EdgeWidth;
            float _EdgeIntensity;
            float _UseNoise;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _NoiseStrength;
            float _NoiseScale;
            float _NoiseSpeed;
            float _DepthOffset;
            float _RadialFalloff;
            sampler2D _CameraDepthTexture;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.vertex);
                o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.objectPos = v.vertex.xyz;
                return o;
            }
            
            float calculateWaveWidth(float3 localPos)
            {
                if (_UseVariableWidth < 0.5)
                    return _WaveWidth;
                
                // Distance depuis le centre de l'objet
                float dist = length(localPos) * 2.0; // *2 car nos coords locales vont de -0.5 à 0.5 pour une sphère
                
                float width;
                
                // Version linéaire (plus large au centre ou à l'extérieur selon _WidthGradient)
                if (_WidthGradient < 1.0)
                    width = lerp(_WaveWidth, _MinWidth, dist * _WidthGradient);
                else
                    width = lerp(_WaveWidth, _MinWidth, (1.0 - dist) * (_WidthGradient - 1.0));
                
                return max(width, _MinWidth);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Couleur de base
                fixed4 col = _Color;
                
                // Obtenir la profondeur de la scène
                float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                
                // Calculer la différence de profondeur pour détecter les intersections
                float depthDifference = abs(sceneDepth - i.eyeDepth);
                
                // Appliquer un décalage de profondeur
                depthDifference += _DepthOffset;
                
                // Appliquer une atténuation radiale
                if (_RadialFalloff > 0.0) {
                    float radialDist = length(i.objectPos * 2.0);
                    depthDifference *= 1.0 + (radialDist * _RadialFalloff);
                }
                
                // Appliquer le bruit à la différence de profondeur
                if (_UseNoise > 0.5) {
                    // Créer des coordonnées UV
                    float2 noiseUV = i.worldPos.xz * _NoiseScale;
                    noiseUV += _Time.y * _NoiseSpeed;
                    
                    float noise = tex2D(_NoiseTex, noiseUV).r * 2.0 - 1.0;

                    float currentWidth = calculateWaveWidth(i.objectPos);

                    depthDifference += noise * _NoiseStrength * currentWidth;
                }
                
                // Déterminer la largeur de la vague en fonction de la position
                float waveWidth = calculateWaveWidth(i.objectPos);
                
                // Créer la vague principale
                float waveCenter = waveWidth * 0.5;
                float waveDist = abs(depthDifference - waveCenter);
                float normalizedDist = waveDist / (waveWidth * 0.5);
                float wavePeak = saturate(1.0 - normalizedDist);
                wavePeak = pow(wavePeak, _WaveSharpness) * _Intensity;
                
                // Appliquer le dégradé en fonction de la distance
                float distanceFactor = saturate(1.0 - (depthDifference / _WaveFadeDistance));
                wavePeak *= distanceFactor;
                
                // Effet de surbrillance
                float edgeGlow = 0;
                if (_UseEdgeHighlight > 0.5) {
                    float edgeDist = abs(normalizedDist - 1.0);
                    edgeGlow = saturate(1.0 - edgeDist / (_EdgeWidth / waveWidth));
                    edgeGlow = pow(edgeGlow, 3.0) * _EdgeIntensity * wavePeak;
                }

                float finalEffect = max(wavePeak, edgeGlow);
                
                fixed4 finalColor = lerp(col, _WaveColor, finalEffect);
                finalColor.a = finalEffect * _WaveColor.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
}