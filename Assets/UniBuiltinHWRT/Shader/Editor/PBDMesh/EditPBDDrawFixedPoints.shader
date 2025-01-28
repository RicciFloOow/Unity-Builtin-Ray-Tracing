Shader "UniBuiltinHWRT/Editor/EditPBDDrawFixedPoints"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            Name "Blit Unselected Fixed Points Pass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "../../Lib/UtilLib.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(uint vertexID : SV_VertexID)
            {
                v2f o;
                o.vertex = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            sampler2D _VirtualCameraFixedPoints_RT;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_VirtualCameraFixedPoints_RT, i.uv);
                return col;
            }
            ENDCG
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            //这里我们让选定的顶点不需要考虑深度，永远显示
            Name "Draw Select Point Pass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "../../Lib/UtilLib.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _SelectPointScreenSpacePos;

            v2f vert(uint vertexID : SV_VertexID)
            {
                uint vertIndex = lerp(vertexID, (vertexID < 4 ? 0 : vertexID - 2), vertexID / 3);
                float2 uv = float2((vertIndex << 1) & 2, vertIndex & 2);
                //
                v2f o;
                o.vertex = float4((uv * 2 - 1) * float2(8 / 1280.0, 8 / 720.0) * _SelectPointScreenSpacePos.w + _SelectPointScreenSpacePos.xy, _SelectPointScreenSpacePos.zw);
                o.uv = uv - 0.5;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float alpha = smoothstep(0.5, 0.45, length(i.uv));
                return half4(0.75, 1, 0.2, alpha);
            }
            ENDCG
        }
    }
}
