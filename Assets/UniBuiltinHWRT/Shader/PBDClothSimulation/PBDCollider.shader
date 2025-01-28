Shader "UniBuiltinHWRT/PBDClothSimulation/PBDCollider"
{
    Properties
    {

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off

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
                float3 normal : NORMAL;
                float3 viewDir : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                float4 wPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1));
                v2f o;
                o.uv = v.uv;
                o.vertex = mul(UNITY_MATRIX_VP, wPos);
                o.normal = mul(transpose(unity_WorldToObject), float4(v.normal.xyz, 0)).xyz;
                o.viewDir = _WorldSpaceCameraPos - wPos.xyz;
                return o;
            }

            float mod(float x, float y)
            {
                return x - y * floor(x / y);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.normal);
                float3 viewDir = normalize(i.viewDir);//TODO:SafeNormalize
                //
                i.uv = floor(i.uv * 64);
                float3 baseColor = lerp(float3(0.4, 0.7, 0.9), 0.9, mod((i.uv.x + i.uv.y), 2.0));//
                float NDotV = abs(dot(normal, viewDir));//需要用abs，因为背面的法线"相反"
                //
                return float4(baseColor * NDotV, 1);
            }
            ENDCG
        }

        Pass
        {
            Name "PBDClothCollision"
            Tags{ "LightMode" = "HWRayTracing" }

            HLSLPROGRAM

            #pragma raytracing HitShader

            #include "PBDClothInputs.cginc"
            #include "UnityRaytracingMeshUtils.cginc"

            #define INTERPOLATE_RAYTRACING_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

            // Fetch the intersetion vertex data for the target vertex
            void FetchIntersectionVertex(uint vertexIndex, out IntersectionVertex outVertex)
            {
                outVertex.positionOS = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributePosition);
                outVertex.normalOS = UnityRayTracingFetchVertexAttribute3(vertexIndex, kVertexAttributeNormal);
                outVertex.tangentOS = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeTangent);
                outVertex.texCoord0 = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord0);
                outVertex.texCoord1 = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord1);
                outVertex.texCoord2 = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord2);
                outVertex.texCoord3 = UnityRayTracingFetchVertexAttribute2(vertexIndex, kVertexAttributeTexCoord3);
                outVertex.color = UnityRayTracingFetchVertexAttribute4(vertexIndex, kVertexAttributeColor);
            }

            void GetCurrentIntersectionVertex(AttributeData attributeData, out IntersectionVertex outVertex)
            {
                // Fetch the indices of the currentr triangle
                uint3 triangleIndices = UnityRayTracingFetchTriangleIndices(PrimitiveIndex());

                // Fetch the 3 vertices
                IntersectionVertex v0, v1, v2;
                FetchIntersectionVertex(triangleIndices.x, v0);
                FetchIntersectionVertex(triangleIndices.y, v1);
                FetchIntersectionVertex(triangleIndices.z, v2);

                // Compute the full barycentric coordinates
                float3 barycentricCoordinates = float3(1.0 - attributeData.barycentrics.x - attributeData.barycentrics.y, attributeData.barycentrics.x, attributeData.barycentrics.y);

                // Interpolate all the data
                outVertex.positionOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.positionOS, v1.positionOS, v2.positionOS, barycentricCoordinates);
                outVertex.normalOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.normalOS, v1.normalOS, v2.normalOS, barycentricCoordinates);
                outVertex.tangentOS = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.tangentOS, v1.tangentOS, v2.tangentOS, barycentricCoordinates);
                outVertex.texCoord0 = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord0, v1.texCoord0, v2.texCoord0, barycentricCoordinates);
                outVertex.texCoord1 = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord1, v1.texCoord1, v2.texCoord1, barycentricCoordinates);
                outVertex.texCoord2 = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord2, v1.texCoord2, v2.texCoord2, barycentricCoordinates);
                outVertex.texCoord3 = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.texCoord3, v1.texCoord3, v2.texCoord3, barycentricCoordinates);
                outVertex.color = INTERPOLATE_RAYTRACING_ATTRIBUTE(v0.color, v1.color, v2.color, barycentricCoordinates);
            }

            [shader("closesthit")]
            void ClosestHit(inout RayPayload rayPayload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
            {
                IntersectionVertex currentvertex;
                GetCurrentIntersectionVertex(attributeData, currentvertex);
                //
                rayPayload.HitCurrent = RayTCurrent();
                rayPayload.Normal = normalize(mul((float3x3)WorldToObject4x3(), currentvertex.normalOS));
                rayPayload.IsHit = true;
            }
            ENDHLSL
        }
    }
}
