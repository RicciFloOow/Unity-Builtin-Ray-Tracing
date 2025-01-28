Shader "UniBuiltinHWRT/PBDClothSimulation/PBDCloth"
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
            #include "PBDClothInputs.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 viewDir : TEXCOORD1;
            };

            float4x4 _ClothWorld2ObjectMat;

            v2f vert (uint triangleVertID : SV_VertexID)
            {
                int vertIndex = _Mesh_IndicesBuffer[triangleVertID];
                float4 wPos = float4(_Mesh_Dynamic_VerticesBuffer[vertIndex], 1);
                //
                v2f o;
                o.uv = _Mesh_UVBuffer[vertIndex];
                o.vertex = mul(UNITY_MATRIX_VP, wPos);
                o.normal = mul(_ClothWorld2ObjectMat, float4(_Mesh_Dynamic_NormalsBuffer[vertIndex], 0)).xyz;
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
                float3 baseColor = lerp(float3(0.9, 0.4, 0.2), float3(0.75, 1, 0.4), mod((i.uv.x + i.uv.y), 2.0));//
                float NDotV = abs(dot(normal, viewDir)) * 0.5 + 0.5;//需要用abs，因为背面的法线"相反"
                //
                return float4(baseColor * NDotV, 1);
            }
            ENDCG
        }
    }
}
