//在intersection shader中我们可以自行给出射线与指定AABB包围下的结构的相交结果
//所以光滑的球、圆锥、圆柱或是cluster都可以通过这种方案来检测
//但需要注意的是，正如下方链接所述的: Using intersection shaders instead of the build-in ray-triangle intersection is less efficient but offers far more flexibility.
//ref: https://microsoft.github.io/DirectX-Specs/d3d/Raytracing.html#intersection-shaders---procedural-primitive-geometry
//因此如果提交的AABB都是单个三角形的包围盒，那就实在是太蠢了
//此外，需要注意intersection shader提交给hit group的数据是有限制的: 不能超过32bytes(也就是两个float4)
//ref: https://learn.microsoft.com/en-us/windows/win32/direct3d12/intersection-attributes#axis-aligned-bounding-box-for-procedural-primitive-intersection
//由于这个只是一个演示Instance接口怎么用的demo，所以这里我们就绘制AABB
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBuiltinHWRT
{
    [Serializable]
    public struct InstanceAACuboidParam
    {
        public Color Color;
        public Color SpecularColor;
        public Color EmissionColor;
        public Vector4 FresnelParameter;
        public float EmissionStrength;
        public float Smoothness;
        public float SpecularProbability;

        public InstanceAACuboidParam(Color color, Color specularColor, Color emissionColor, Vector4 fresnelParameter, float emissionStrength, float smoothness, float specularProbability)
        {
            Color = color;
            SpecularColor = specularColor;
            EmissionColor = emissionColor;
            FresnelParameter = fresnelParameter;
            EmissionStrength = emissionStrength;
            Smoothness = smoothness;
            SpecularProbability = specularProbability;
        }
    }

    public class InstanceRTRenderer : MonoBehaviour
    {
        public InstanceAACuboidParam CuboidMaterialParameters;

        #region ----Unity----
        private void OnEnable()
        {
            GraphicManager.Instance.RegisterRenderer(this);
        }

        private void OnDisable()
        {
            GraphicManager.Instance.UnregisterRenderer(this);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            CuboidMaterialParameters = new InstanceAACuboidParam(Color.white, Color.white, Color.white, new Vector4(1.00f, 0.71f, 0.29f, 0), 0, 0.5f, 0.5f);
        }
#endif
        #endregion
    }
}