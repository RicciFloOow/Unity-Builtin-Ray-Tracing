Shader "UniBuiltinHWRT/UniPBR/PBRSimpleGlass"
{
    Properties
    {
        _Color("Base Color", Color) = (1, 1, 1, 1)
        _IoR("IoR", Range(0, 5)) = 1.4
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags {"Queue" = "Transparent"}
        //用于editor下显示的光栅化pass
        Pass
        {
            Tags { "IgnoreProjector" = "True" "RenderType" = "Transparent"}
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "../Lib/PBRHelperLib.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 viewDir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                float4 wpos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));
                v2f o;
                o.vertex = mul(UNITY_MATRIX_VP, wpos);
                o.normal = UnityObjectToWorldDir(v.normal);
                o.viewDir = _WorldSpaceCameraPos - wpos.xyz;
                return o;
            }

            half4 _Color;
            float _IoR;

            fixed4 frag(v2f i) : SV_Target
            {
                i.viewDir = normalize(i.viewDir);//TODO: safe normalize
                i.normal = normalize(i.normal);
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float cosTheta = dot(i.normal, i.viewDir);
                float3 reflectVec = reflect(-i.viewDir, i.normal);
                half4 rgbm = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflectVec, 0);
                float3 reflCol = DecodeHDR(rgbm, unity_SpecCube0_HDR);
                //
                float reflProb = SchlickFresnelSpecularReflection(cosTheta, _IoR);
                return lerp(half4(_Color.xyz, 0), half4(reflCol, 1), saturate(reflProb));
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
            float _IoR;
            float _Smoothness;

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
                float3 worldNormal = normalize(mul((float3x3)WorldToObject4x3(), currentvertex.normalOS));
                //
                bool isFromAir = HitKind() == HIT_KIND_TRIANGLE_FRONT_FACE;//我们默认三角形的"定向"是指向"外部的"，也即指向"空气一侧"的
                float3 surfaceNormal = isFromAir ? worldNormal : -worldNormal;
                float3 diffuseDir = normalize(surfaceNormal + pcgRandomDirection(rayPayload.randomSeed));
                surfaceNormal = lerp(diffuseDir, surfaceNormal, _Smoothness);
                float _relaIoR = isFromAir ? _IoR : 1 / _IoR;
                float3 refrDir = refract(rayDir, surfaceNormal, 1 / _relaIoR);//注意，在(相对)高折射率介质内，如果入射角过大就会发生全反射，这时候返回的是零向量(需要处理)
                //此外，一个比较重要的一点是，refract的第三个参数是:source material's IoR / destination material's IoR
                bool isTIR = dot(refrDir, refrDir) < 0.00001;
                float3 reflDir = reflect(rayDir, surfaceNormal);
                //
                float cosTheta = -dot(rayDir, surfaceNormal);//需要注意ray与surfaceNormal的方向，这两个向量的夹角是入射光的补角，有cos(pi-x)=-cos(x)，因此有负号
                float reflProb = SchlickFresnelSpecularReflection(cosTheta, _relaIoR);
                //
                float3 newRayDir = ((pcgHash(rayPayload.randomSeed) < reflProb) || isTIR) ? reflDir : refrDir;
                //
                RayDesc rayDesc;
                rayDesc.Origin = worldPos;
                rayDesc.Direction = newRayDir;
                rayDesc.TMin = 0.00001;//
                rayDesc.TMax = _ProjectionParams.z;
                //
                RayPayload scatterRayPayload;
                scatterRayPayload.color = 0;
                scatterRayPayload.randomSeed = rayPayload.randomSeed;
                scatterRayPayload.bounceTimes = rayPayload.bounceTimes + 1;
                //
                TraceRay(_SceneAccelStruct, RAY_FLAG_NONE, 0xFF, 0, 1, 0, rayDesc, scatterRayPayload);//TODO:基于是折射还是反射(计算_newRayDir的条件)，区分需要剔除正面还是背面，即，用RAY_FLAG_CULL_BACK_FACING_TRIANGLES还是RAY_FLAG_CULL_FRONT_FACING_TRIANGLES
                rayPayload.color = _Color.xyz * scatterRayPayload.color;
            }

            ENDHLSL
        }
    }
}
