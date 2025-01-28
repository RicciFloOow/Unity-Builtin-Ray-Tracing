Shader "UniBuiltinHWRT/UniPBR/BRDFOpaqueColor"
{
    Properties
    {
        _Color("Base Color", Color) = (1, 1, 1, 1)
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _SpecularProbability("Specular Probability", Range(0, 1)) = 0.5
        _FresnelParameter ("Fresnel Parameter", Vector) = (0.01, 0.01, 0.01, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        //用于editor下显示的光栅化pass，删去就不会被光栅化了(当然，这就需要自行给editor的相机加个渲染这些网格的pass了)
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldDir(v.normal);
                return o;
            }

            half4 _Color;

            fixed4 frag (v2f i) : SV_Target
            {
                float NDotL = saturate(dot(_WorldSpaceLightPos0.xyz, i.normal));
                fixed4 col = _Color * (NDotL * 0.5 + 0.5);
                return col;
            }
            ENDCG
        }

        Pass
        {
            //CommandBuffer.SetRayTracingShaderPass()的参数中使用的就是这里的Name(同一个Pass的名字要一样)
            //此外，至少在2022.3.+版本下的AMD显卡中，如果存在多个(只要是项目中)Name不同的光追Pass，运行会立刻闪退
            Name "HWRayTracing"
            Tags{ "LightMode" = "HWRayTracing" }

            HLSLPROGRAM

            //表明是光追的pass
            #pragma raytracing HitShader
            #include "UnityShaderVariables.cginc"
            #include "../Lib/BuiltinHWRTHelperLib.cginc"
            #include "../Lib/PBRHelperLib.cginc"

            half4 _Color;
            half4 _SpecularColor;
            float _Smoothness;
            float _SpecularProbability;
            float4 _FresnelParameter;

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
                //注意到WorldToObject4x3是WorldToObject3x4的转置矩阵，因此不需要在这里用transpose()
                float3 worldNormal = normalize(mul((float3x3)WorldToObject4x3(), currentvertex.normalOS));
                float3 diffuseDir = normalize(worldNormal + pcgRandomDirection(rayPayload.randomSeed));
                float3 specularDir = reflect(rayDir, worldNormal);
                float3 scatterRayDir = lerp(diffuseDir, specularDir, _Smoothness);
                //
                RayDesc rayDesc;
                rayDesc.Origin = worldPos;
                rayDesc.Direction = scatterRayDir;
                rayDesc.TMin = 0;//新的射线的最小长度就应该从0检测
                rayDesc.TMax = _ProjectionParams.z;
                //
                RayPayload scatterRayPayload;
                scatterRayPayload.color = 0;
                scatterRayPayload.randomSeed = rayPayload.randomSeed;
                scatterRayPayload.bounceTimes = rayPayload.bounceTimes + 1;
                //
                TraceRay(_SceneAccelStruct, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, 0xFF, 0, 1, 0, rayDesc, scatterRayPayload);//
                float3 fresnel = SchlickFresnelSpecularReflectionOpaque(dot(worldNormal, rayDir), _FresnelParameter.xyz);
                float specularProbability = lerp(_SpecularProbability, 1, fresnel * _Smoothness);
                bool3 isSpecularBounce = specularProbability >= pcgHash(rayPayload.randomSeed);
                rayPayload.color = lerp(_Color.xyz, _SpecularColor.xyz, isSpecularBounce) * scatterRayPayload.color;
            }
            ENDHLSL
        }
    }
}
