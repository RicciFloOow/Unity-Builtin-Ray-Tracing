using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UniBuiltinHWRT
{
    public struct AABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public AABB(Transform t)
        {
            var _center = t.position;
            var _extent = t.lossyScale * 0.5f;
            Min = _center - _extent;
            Max = _center + _extent;
        }
    }

    public partial class InstanceRTCamera : MonoBehaviour
    {
        #region ----Camera----
        private Camera m_renderCam;

        private void SetupRenderCam()
        {
            if (m_renderCam == null)
            {
                m_renderCam = GetComponent<Camera>();
                m_renderCam.allowHDR = false;
                m_renderCam.allowMSAA = false;
            }
        }
        #endregion

        #region ----RT Shader----
        public RayTracingShader InstanceRTShader;

        public Shader InstanceCuboidShader;
        #endregion

        #region ----Skybox----
        public Cubemap SkyboxTex;
        #endregion

        #region ----RT Handles----
        private RTHandle m_HWRTColor_Handle;

        private int m_renderFrame;

        private uint m_ScreenWidth;
        private uint m_ScreenHeight;

        private void SetupRTHandles()
        {
            Vector2Int _screenSize = new Vector2Int(m_renderCam.pixelWidth, m_renderCam.pixelHeight);
            m_ScreenWidth = (uint)_screenSize.x;
            m_ScreenHeight = (uint)_screenSize.y;
            //
            m_HWRTColor_Handle = new RTHandle(_screenSize.x, _screenSize.y, 0, GraphicsFormat.R32G32B32A32_SFloat, 0, true);
        }

        private void ReleaseRTHandles()
        {
            m_HWRTColor_Handle?.Release();
        }
        #endregion

        #region ----RT Acc Struct----
        private RayTracingAccelerationStructure m_RTAccStruct;

        private GraphicsBuffer m_InstanceRendererGraphicsBuffer;
        private ComputeBuffer m_InstanceMaterialComputeBuffer;

        private Material m_InstanceAACuboidMat;

        private void SetupRayTracingAccelStruct()
        {
            m_InstanceAACuboidMat = new Material(InstanceCuboidShader);
            //要注意，我们这里是要手动管理的(用RayTracingAccelerationStructure.ManagementMode.Manual)
            RayTracingAccelerationStructure.RASSettings _settings = new RayTracingAccelerationStructure.RASSettings(RayTracingAccelerationStructure.ManagementMode.Manual, RayTracingAccelerationStructure.RayTracingModeMask.Everything, ~0);
            m_RTAccStruct = new RayTracingAccelerationStructure(_settings);
            //
            if (GraphicManager.Instance.InstanceRTRendererList != null && GraphicManager.Instance.InstanceRTRendererList.Count > 0)
            {
                int instanceCount = GraphicManager.Instance.InstanceRTRendererList.Count;
                AABB[] _aabbs = new AABB[instanceCount];
                InstanceAACuboidParam[] _matParams = new InstanceAACuboidParam[instanceCount];
                for (int i = 0; i < instanceCount; i++)
                {
                    var _r = GraphicManager.Instance.InstanceRTRendererList[i];
                    _aabbs[i] = new AABB(_r.transform);
                    _matParams[i] = _r.CuboidMaterialParameters;
                }
                GraphicsUtility.AllocateGraphicsBuffer(ref m_InstanceRendererGraphicsBuffer, instanceCount, Marshal.SizeOf(typeof(AABB)));
                GraphicsUtility.AllocateComputeBuffer(ref m_InstanceMaterialComputeBuffer, instanceCount, Marshal.SizeOf(typeof(InstanceAACuboidParam)));
                //
                m_InstanceRendererGraphicsBuffer.SetData(_aabbs);
                m_InstanceMaterialComputeBuffer.SetData(_matParams);
                //
                MaterialPropertyBlock _matPropertyBlock = new MaterialPropertyBlock();
                _matPropertyBlock.SetBuffer(k_ShaderProperty_Buffer_InstanceCuboidBuffer, m_InstanceRendererGraphicsBuffer);//这里AABB就是cuboid的顶点计算条件
                m_RTAccStruct.AddInstance(m_InstanceRendererGraphicsBuffer, (uint)instanceCount, false, Matrix4x4.identity, m_InstanceAACuboidMat, true, _matPropertyBlock);
            }
            //
            m_RTAccStruct.Build();
        }

        private void ReleaseRayTracingAccelStruct()
        {
            if (m_InstanceAACuboidMat != null)
            {
                Destroy(m_InstanceAACuboidMat);
            }
            //
            m_RTAccStruct?.Release();
            m_RTAccStruct = null;
            //
            m_InstanceRendererGraphicsBuffer?.Release();
            m_InstanceRendererGraphicsBuffer = null;
            m_InstanceMaterialComputeBuffer?.Release();
            m_InstanceMaterialComputeBuffer = null;
        }
        #endregion
    }
}