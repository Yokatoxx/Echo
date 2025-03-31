Shader "Custom/PointCloud" {
    Properties {
        _Size ("Point Size", Range(0.001, 0.1)) = 0.05
        _Color ("Color", Color) = (1,1,1,1)
    }

    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            StructuredBuffer<float3> _Positions;
            float _Size;
            fixed4 _Color;

            struct v2f {
                float4 pos : SV_POSITION;
                float size : PSIZE;
            };

            v2f vert (uint id : SV_VertexID) {
                v2f o;
                o.pos = UnityObjectToClipPos(float4(_Positions[id], 1));
                o.size = _Size;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return _Color;
            }
            ENDCG
        }
    }
}