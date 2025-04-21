Shader "Custom/EcholocationShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.0, 0.5, 1.0, 0.5)
        _IntersectionColorStart ("Intersection Color Start", Color) = (0.0, 1.0, 1.0, 1.0)
        _IntersectionColorEnd ("Intersection Color End", Color) = (1.0, 0.2, 0.0, 1.0)
        _IntersectionWidth ("Intersection Width", Range(0, 5)) = 1.0
        _IntersectionIntensity ("Intersection Intensity", Range(1, 10)) = 2.0
        _IntersectionFalloff ("Intersection Falloff", Range(0.1, 5)) = 1.5
        _EdgeFalloff ("Edge Falloff", Range(0.1, 5)) = 1.5
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0.1
        _GradientOffset ("Gradient Offset", Range(0, 1)) = 0.5
        _GradientPower ("Gradient Power", Range(0.1, 5)) = 1.0
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
                float3 objectPos : TEXCOORD4;
                UNITY_FOG_COORDS(5)
            };
            
            float4 _MainColor;
            float4 _IntersectionColorStart;
            float4 _IntersectionColorEnd;
            float _IntersectionWidth;
            float _IntersectionIntensity;
            float _IntersectionFalloff;
            float _EdgeFalloff;
            float _EdgeWidth;
            float _GradientOffset;
            float _GradientPower;
            float _PulseSpeed;
            float _PulseAmount;
            sampler2D _CameraDepthTexture;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.eyeDepth = -UnityObjectToViewPos(v.vertex).z;
                o.objectPos = v.vertex.xyz; // Position locale pour calculer les bords
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            float4 frag (v2f i) : SV_Target
            {
                // Récupère la profondeur depuis le depth buffer
                float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                
                // Calcule la différence de profondeur
                float depthDiff = abs(sceneDepth - i.eyeDepth);
                
                // Effet de pulsation légère sur l'intersection
                float pulseFactor = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float pulseModifier = lerp(1.0 - _PulseAmount, 1.0 + _PulseAmount, pulseFactor);
                
                // Effet d'intersection
                float intersectRaw = saturate(1.0 - depthDiff / (_IntersectionWidth * pulseModifier));
                float intersect = pow(intersectRaw, _IntersectionFalloff) * _IntersectionIntensity;
                
                // Calcul du gradient
                float gradientFactor = pow(saturate(intersectRaw + _GradientOffset), _GradientPower);
                float4 intersectionColor = lerp(_IntersectionColorStart, _IntersectionColorEnd, gradientFactor);
                
                // Calcul de l'effet de bord
                float distFromCenter = length(i.objectPos);
                float edgeEffect = smoothstep(0.5 - _EdgeWidth, 0.5, distFromCenter);
                edgeEffect = pow(edgeEffect, _EdgeFalloff);
                
                // Combiner les couleurs
                float4 finalColor = _MainColor;
                
                // Ajouter l'effet d'intersection avec gradient
                finalColor = lerp(finalColor, intersectionColor, saturate(intersect));
                
                // Amplifier les bords
                finalColor.rgb = lerp(finalColor.rgb, _IntersectionColorStart.rgb, edgeEffect * 0.5);
                
                // Ajuster l'opacité
                finalColor.a = _MainColor.a * (1.0 - edgeEffect * 0.7);
                finalColor.a = max(finalColor.a, intersect * max(_IntersectionColorStart.a, _IntersectionColorEnd.a));
                
                // Réduction subtile de l'opacité en fonction de la distance de l'intersection
                finalColor.a *= lerp(0.85, 1.0, min(1.0, intersect * 2.0)); 
                
                // Appliquer le fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}