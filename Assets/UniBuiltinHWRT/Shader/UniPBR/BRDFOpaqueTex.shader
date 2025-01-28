Shader "UniBuiltinHWRT/UniPBR/BRDFOpaqueTex"
{
    Properties
    {
        _BaseTex("Base Texture", 2D) = "white" {}
        _Color("Base Color", Color) = (1, 1, 1, 1)
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _SpecularProbability("Specular Probability", Range(0, 1)) = 0.5
        _FresnelParameter("Fresnel Parameter", Vector) = (0.01, 0.01, 0.01, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        //用于editor下显示的光栅化pass
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldDir(v.normal);
                o.uv = v.uv;
                return o;
            }

            half4 _Color;
            sampler2D _BaseTex;

            fixed4 frag (v2f i) : SV_Target
            {
                float NDotL = saturate(dot(_WorldSpaceLightPos0.xyz, i.normal));
                fixed4 col = tex2D(_BaseTex, i.uv);
                return _Color * (NDotL * 0.5 + 0.5) * col;
            }
            ENDCG
        }

        Pass
        {
            Name "HWRayTracing"
            Tags{ "LightMode" = "HWRayTracing" }

            HLSLPROGRAM

            #pragma raytracing HitShader
            #include "UnityShaderVariables.cginc"
            #include "../Lib/BuiltinHWRTHelperLib.cginc"
            #include "../Lib/PBRHelperLib.cginc"

            half4 _Color;
            half4 _SpecularColor;//TODO:specular tex
            float _Smoothness;//TODO:_BaseTex的alpha通道
            float _SpecularProbability;
            float4 _FresnelParameter;
            Texture2D _BaseTex;
            SamplerState sampler_LinearRepeat;

            [shader("closesthit")]
            void ClosestHit(inout RayPayload rayPayload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
            {
                if (rayPayload.bounceTimes + 1 >= _MaxBounceCount)
                {
                    return;
                }
                //
                IntersectionVertex currentvertex;
                GetCurrentIntersectionVertex(attributeData, currentvertex);
                float3 rayOrigin = WorldRayOrigin();
                float3 rayDir = WorldRayDirection();
                float3 worldPos = rayOrigin + RayTCurrent() * rayDir;
                //
                float2 uv = currentvertex.texCoord0;
                float3 baseColor = _BaseTex.SampleLevel(sampler_LinearRepeat, uv, 0).xyz;
                //
                //注意到WorldToObject4x3是WorldToObject3x4的转置矩阵，因此不需要在这里用transpose()
                float3 worldNormal = normalize(mul((float3x3)WorldToObject4x3(), currentvertex.normalOS));//TODO:Bump map
                float3 diffuseDir = normalize(worldNormal + pcgRandomDirection(rayPayload.randomSeed));
                float3 specularDir = reflect(rayDir, worldNormal);
                float3 scatterRayDir = lerp(diffuseDir, specularDir, _Smoothness);
                //
                RayDesc rayDesc;
                rayDesc.Origin = worldPos;
                rayDesc.Direction = scatterRayDir;
                rayDesc.TMin = 0;
                rayDesc.TMax = _ProjectionParams.z;
                //
                RayPayload scatterRayPayload;
                scatterRayPayload.color = 0;
                scatterRayPayload.randomSeed = rayPayload.randomSeed;
                scatterRayPayload.bounceTimes = rayPayload.bounceTimes + 1;
                //
                TraceRay(_SceneAccelStruct, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, 0xFF, 0, 1, 0, rayDesc, scatterRayPayload);
                float3 fresnel = SchlickFresnelSpecularReflectionOpaque(dot(worldNormal, rayDir), _FresnelParameter.xyz);
                float specularProbability = lerp(_SpecularProbability, 1, fresnel * _Smoothness);
                bool isSpecularBounce = specularProbability >= pcgHash(rayPayload.randomSeed);
                rayPayload.color = lerp(baseColor * _Color.xyz, _SpecularColor.xyz, isSpecularBounce) * scatterRayPayload.color;
            }
            ENDHLSL
        }
    }
}
