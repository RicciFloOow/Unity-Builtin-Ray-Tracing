using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UniBuiltinHWRT
{
    public partial class UniRTCamera : MonoBehaviour
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
        public RayTracingShader UniRTShader;
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
            //在编辑器下，重新启用组件后获得的Screen.width, Screen.height可能并不是准确的
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

        private void SetupRayTracingAccelStruct()
        {
            RayTracingAccelerationStructure.RASSettings _settings = new RayTracingAccelerationStructure.RASSettings(RayTracingAccelerationStructure.ManagementMode.Automatic, RayTracingAccelerationStructure.RayTracingModeMask.Everything, ~0);
            m_RTAccStruct = new RayTracingAccelerationStructure(_settings);
            //
            for (int i = 0; i < GraphicManager.Instance.UniRTRendererList.Count; i++)
            {
                var _r = GraphicManager.Instance.UniRTRendererList[i];
                if (_r.RTSubMeshFlags == null)
                {
                    m_RTAccStruct.AddInstance(_r.UniRenderer, new RayTracingSubMeshFlags[] { RayTracingSubMeshFlags.Enabled });
                }
                else
                {
                    m_RTAccStruct.AddInstance(_r.UniRenderer, _r.RTSubMeshFlags);
                }
            }
            //
            m_RTAccStruct.Build();
        }

        private void ReleaseRayTracingAccelStruct()
        {
            m_RTAccStruct?.Release();
            m_RTAccStruct = null;
        }
        #endregion
    }
}