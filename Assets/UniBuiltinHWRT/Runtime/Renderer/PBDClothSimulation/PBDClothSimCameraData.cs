using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UniBuiltinHWRT
{
    public partial class PBDClothSimCamera : MonoBehaviour
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

        #region ----Shaders----
        public RayTracingShader PBDCollisionRTS;
        public ComputeShader PBDSimulationCS;
        #endregion

        #region ----RT Acc Struct----
        private RayTracingAccelerationStructure m_RTAccStruct;

        private void SetupRayTracingAccelStruct()
        {
            RayTracingAccelerationStructure.RASSettings _settings = new RayTracingAccelerationStructure.RASSettings(RayTracingAccelerationStructure.ManagementMode.Automatic, RayTracingAccelerationStructure.RayTracingModeMask.Everything, ~0);
            m_RTAccStruct = new RayTracingAccelerationStructure(_settings);
            //
            for (int i = 0; i < GraphicManager.Instance.PBDClothColliderList.Count; i++)
            {
                var collider = GraphicManager.Instance.PBDClothColliderList[i];
                m_RTAccStruct.AddInstance(collider.UniRenderer, collider.RTSubMeshFlags);
            }
            //
            m_RTAccStruct.Build();
        }

        private void UpdateRTAS()
        {
            for (int i = 0; i < GraphicManager.Instance.PBDClothColliderList.Count; i++)
            {
                var collider = GraphicManager.Instance.PBDClothColliderList[i];
                m_RTAccStruct.UpdateInstanceTransform(collider.UniRenderer);
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

        #region ----PBD Cloth----
        public PBDClothRenderer TargetPBDCloth;
        #endregion
    }
}