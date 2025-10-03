Shader "Custom/SlimeMetaball"
{
    Properties
    {
        _Color ("Color", Color) = (0.2, 0.8, 0.3, 0.8)
        _Metallic ("Metallic", Range(0,1)) = 0.2
        _Smoothness ("Smoothness", Range(0,1)) = 0.8
        _FresnelPower ("Fresnel Power", Range(0,5)) = 3.0
        _FresnelColor ("Fresnel Color", Color) = (0.5, 1.0, 0.5, 1.0)
        _Threshold ("Threshold", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        struct Input
        {
            float3 viewDir;
            float3 worldPos;
        };

        half4 _Color;
        half _Metallic;
        half _Smoothness;
        half _FresnelPower;
        half4 _FresnelColor;
        half _Threshold;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base color
            o.Albedo = _Color.rgb;
            
            // Metallic and smoothness
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            
            // Fresnel effect
            half fresnel = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            fresnel = pow(fresnel, _FresnelPower);
            
            // Mix fresnel color
            o.Emission = _FresnelColor.rgb * fresnel;
            
            // Alpha
            o.Alpha = _Color.a * (1.0 + fresnel * 0.2);
        }
        ENDCG
    }
    FallBack "Standard"
}
