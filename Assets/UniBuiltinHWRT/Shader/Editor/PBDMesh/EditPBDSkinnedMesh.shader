Shader "UniBuiltinHWRT/Editor/EditPBDSkinnedMesh"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off

            Name "Base Shape Rendering Pass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 viewDir : TEXCOORD1;
            };

            float3 _EditVirtualCamera_WorldSpacePos;

            float4x4 _EditVirtualCamera_VPMatrix;

            StructuredBuffer<float3> _EditPBDSkinnedMeshVerticesBuffer;
            StructuredBuffer<float3> _EditPBDSkinnedMeshNormalsBuffer;
            StructuredBuffer<float2> _EditPBDSkinnedMeshUVsBuffer;

            StructuredBuffer<int> _EditPBDSkinnedMeshIndicesBuffer;//未压缩的

            v2f vert (uint triangleVertID : SV_VertexID)
            {
                int vertIndex = _EditPBDSkinnedMeshIndicesBuffer[triangleVertID];
                float4 wPos = mul(unity_ObjectToWorld, float4(_EditPBDSkinnedMeshVerticesBuffer[vertIndex], 1));
                //
                v2f o;
                o.vertex = mul(_EditVirtualCamera_VPMatrix, wPos);
                o.uv = _EditPBDSkinnedMeshUVsBuffer[vertIndex];
                o.normal = mul(transpose(unity_WorldToObject), float4(_EditPBDSkinnedMeshNormalsBuffer[vertIndex], 0)).xyz;
                o.viewDir = _EditVirtualCamera_WorldSpacePos - wPos.xyz;
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
                float3 baseColor = lerp(0.5, 0.75, mod((i.uv.x + i.uv.y), 2.0));//
                float NDotV = abs(dot(normal, viewDir));//需要用abs，因为背面的法线"相反"
                //
                return float4(baseColor * NDotV, 1);
            }
            ENDCG
        }

        Pass
        {
            Name "Mesh Edges Rendering Pass"

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2g
            {
                float4 vertex : SV_POSITION;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
            };

            float _CamNearPlane;
            float4x4 _EditVirtualCamera_VPMatrix;

            StructuredBuffer<float3> _EditPBDSkinnedMeshVerticesBuffer;
            StructuredBuffer<int2> _EditPBDSkinnedMeshEdgesBuffer;

            v2g vert(uint triangleVertID : SV_VertexID)
            {
                int2 edges = _EditPBDSkinnedMeshEdgesBuffer[triangleVertID / 2];
                int vertIndex = (triangleVertID & 1) == 0 ? edges.x : edges.y;
                float3 vertex = _EditPBDSkinnedMeshVerticesBuffer[vertIndex];
                v2g o;
                o.vertex = mul(_EditVirtualCamera_VPMatrix, mul(unity_ObjectToWorld, float4(vertex, 1)));//
                return o;
            }

            //ref: https://atyuwen.github.io/posts/antialiased-line/
            [maxvertexcount(4)]
            void geom(line v2g IN[2], inout TriangleStream<g2f> OUT)
            {
                v2g P0 = IN[0];
                v2g P1 = IN[1];
                if (P0.vertex.w > P1.vertex.w)
                {
                    v2g temp = P0;
                    P0 = P1;
                    P1 = temp;
                }
                if (P0.vertex.w < _CamNearPlane)
                {
                    float ratio = (_CamNearPlane - P0.vertex.w) / (P1.vertex.w - P0.vertex.w);
                    P0.vertex = lerp(P0.vertex, P1.vertex, ratio);
                }

                float2 a = P0.vertex.xy / P0.vertex.w;//sspos
                float2 b = P1.vertex.xy / P1.vertex.w;
                float2 c = normalize(float2(a.y - b.y, b.x - a.x)) * float2(0.00078125, 0.00138888888888888) * 1.5;

                g2f g0;
                g0.vertex = float4(P0.vertex.xy + c * P0.vertex.w, P0.vertex.zw);
                g2f g1;
                g1.vertex = float4(P0.vertex.xy - c * P0.vertex.w, P0.vertex.zw);
                g2f g2;
                g2.vertex = float4(P1.vertex.xy + c * P1.vertex.w, P1.vertex.zw);
                g2f g3;
                g3.vertex = float4(P1.vertex.xy - c * P1.vertex.w, P1.vertex.zw);

                OUT.Append(g0);
                OUT.Append(g1);
                OUT.Append(g2);
                OUT.Append(g3);
                OUT.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return half4(0.2, 0.64, 0.85, 1);
            }
            ENDCG
        }
    }
}
