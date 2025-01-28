Shader "UniBuiltinHWRT/UniPBR/ColorEmitter"
{
    Properties
    {
        _EmissionColor("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionStrength("EmissionStrength", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            //用于editor下显示的光栅化pass
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
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 _EmissionColor;

            fixed4 frag (v2f i) : SV_Target
            {
                return _EmissionColor;
            }
            ENDCG
        }

        Pass
        {
            Name "HWRayTracing"
            Tags{ "LightMode" = "HWRayTracing" }

            HLSLPROGRAM

            #pragma raytracing HitShader

            #include "../Lib/BuiltinHWRTHelperLib.cginc"

            half4 _EmissionColor;
            float _EmissionStrength;

            [shader("closesthit")]
            void ClosestHit(inout RayPayload rayPayload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
            {
                rayPayload.color += _EmissionColor.xyz * _EmissionStrength;
            }
            ENDHLSL
        }
    }
}
