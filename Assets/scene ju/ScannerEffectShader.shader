Shader"Custom/ScannerEffectShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _HighlightColor ("Highlight Color", Color) = (1, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 1.0
        _ScanRadius ("Scan Radius", Range(0, 5)) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
#include "UnityCG.cginc"

struct appdata_t
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float3 worldPos : TEXCOORD0;
};

float4 _BaseColor;
float4 _HighlightColor;
float _EmissionStrength;
float _ScanRadius;
float3 _ScannerPosition;

v2f vert(appdata_t v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    return o;
}

fixed4 frag(v2f i) : SV_Target
{
    float distanceToScanner = length(i.worldPos - _ScannerPosition);
    float effectStrength = smoothstep(_ScanRadius, 0, distanceToScanner);

    float4 color = lerp(_BaseColor, _HighlightColor, effectStrength);
    color.rgb += effectStrength * _EmissionStrength; // Ajoute un effet d'émission

    return color;
}
            ENDCG
        }
    }
}
