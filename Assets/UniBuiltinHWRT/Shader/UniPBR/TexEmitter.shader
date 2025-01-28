Shader "UniBuiltinHWRT/UniPBR/TexEmitter"
{
    Properties
    {
        _EmissionTex ("Emission Texture", 2D) = "white" {}
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _EmissionTex;
            half4 _EmissionColor;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_EmissionTex, i.uv);
                return col * _EmissionColor;
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

            Texture2D _EmissionTex;
            SamplerState sampler_LinearRepeat;
            half4 _EmissionColor;
            float _EmissionStrength;

            [shader("closesthit")]
            void ClosestHit(inout RayPayload rayPayload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
            {
                IntersectionVertex currentvertex;
                GetCurrentIntersectionVertex(attributeData, currentvertex);
                float2 uv = currentvertex.texCoord0;
                float3 emiTexCol = _EmissionTex.SampleLevel(sampler_LinearRepeat, uv, 0).xyz;
                rayPayload.color += emiTexCol * _EmissionColor.xyz * _EmissionStrength;
            }
            ENDHLSL
        }
    }
}
