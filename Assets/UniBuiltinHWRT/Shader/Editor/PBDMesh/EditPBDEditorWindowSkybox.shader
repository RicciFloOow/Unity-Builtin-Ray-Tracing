Shader "UniBuiltinHWRT/Editor/EditPBDEditorWindowSkybox"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
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

            v2f vert (uint vertexID : SV_VertexID)
            {
                v2f o;
                o.vertex = GetFullScreenTriangleVertexPosition(vertexID);
                o.uv = GetFullScreenTriangleTexCoord(vertexID);
                return o;
            }

            float _Zoom;

            float4x4 _EditVirtualCamera_CameraToWorld;

            TextureCube<float4> _EditSkyboxCubeTex;
            SamplerState sampler_LinearClamp;

            fixed4 frag(v2f i) : SV_Target
            {
                float2 xy = i.uv * 2 - 1;
                xy *= _Zoom;
                float3 _viewDirLocal = normalize(float3(xy.x * 1.77777777777, xy.y, 1));
                float3 _viewDir = normalize(mul((float3x3)_EditVirtualCamera_CameraToWorld, _viewDirLocal));
                //
                fixed4 col = float4(_EditSkyboxCubeTex.SampleLevel(sampler_LinearClamp, _viewDir, 0).xyz, 1);
                return col;
            }
            ENDCG
        }
    }
}
