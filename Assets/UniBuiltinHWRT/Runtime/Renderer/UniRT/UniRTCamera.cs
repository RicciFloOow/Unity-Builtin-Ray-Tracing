//这里我们只允许相机是可运动的，renderers都是静态的
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UniBuiltinHWRT
{
    public partial class UniRTCamera : MonoBehaviour
    {
        #region ----Builtin Unity Renderers' HWRT Rendering Pass----
        private const string k_SimpleHWRTPassName = "HWRayTracing";//必须要与目标shader中的Pass Name一致
        private const string k_SimpleHWRTRayGenName = "BuiltinRaygenShader";//本demo中所用的RTS的ray generation shader名相同
        private const CameraEvent k_BuiltinHWRTRenderingPassCamEvent = CameraEvent.AfterEverything;
        private CommandBuffer m_BuiltinHWRTRenderingPassBuffer;

        private void ExecuteBuiltinSimpleHWRT(ref CommandBuffer cmd, RayTracingShader rts)
        {
            cmd.SetRayTracingShaderPass(rts, k_SimpleHWRTPassName);
            cmd.SetRenderTarget(m_HWRTColor_Handle);
            cmd.SetRayTracingFloatParam(rts, k_ShaderProperty_Float_Zoom, Mathf.Tan(Mathf.Deg2Rad * m_renderCam.fieldOfView * 0.5f));
            cmd.SetRayTracingIntParam(rts, k_ShaderProperty_Uint_RTCurrentFrame, m_renderFrame);
            cmd.SetRayTracingAccelerationStructure(rts, k_ShaderProperty_RTAccStruct, m_RTAccStruct);
            cmd.SetRayTracingTextureParam(rts, k_ShaderProperty_Tex_SkyboxCubeTex, SkyboxTex);
            cmd.SetRayTracingTextureParam(rts, k_ShaderProperty_Tex_RW_ResultRT, m_HWRTColor_Handle);
            cmd.DispatchRays(rts, k_SimpleHWRTRayGenName, m_ScreenWidth, m_ScreenHeight, 1, m_renderCam);
            //
            m_renderFrame++;
        }

        private void ReleaseBuiltinHWRTRenderingPass()
        {
            if (m_renderCam != null)
            {
                if (m_BuiltinHWRTRenderingPassBuffer != null)
                {
                    m_renderCam.RemoveCommandBuffer(k_BuiltinHWRTRenderingPassCamEvent, m_BuiltinHWRTRenderingPassBuffer);
                    m_BuiltinHWRTRenderingPassBuffer.Release();
                    m_BuiltinHWRTRenderingPassBuffer = null;
                }
            }
        }

        private void SetupBuiltinHWRTRenderingPass()
        {
            ReleaseBuiltinHWRTRenderingPass();
            m_BuiltinHWRTRenderingPassBuffer = new CommandBuffer()
            {
                name = "Builtin HWRT Rendering Pass"
            };
            //
            ExecuteBuiltinSimpleHWRT(ref m_BuiltinHWRTRenderingPassBuffer, UniRTShader);
            //
            m_BuiltinHWRTRenderingPassBuffer.Blit(m_HWRTColor_Handle, BuiltinRenderTextureType.CameraTarget);
            m_renderCam.AddCommandBuffer(k_BuiltinHWRTRenderingPassCamEvent, m_BuiltinHWRTRenderingPassBuffer);
        }
        #endregion

        #region ----Unity----
        private void OnEnable()
        {
            SetupRenderCam();
            SetupRTHandles();
        }

        private void Start()
        {
            SetupRayTracingAccelStruct();
            m_renderFrame = 0;
        }

        private void Update()
        {
            if (transform.hasChanged)
            {
                m_renderFrame = 0;
                transform.hasChanged = false;
            }
        }

        private void OnPreRender()
        {
            SetupBuiltinHWRTRenderingPass();
        }

        private void OnDisable()
        {
            ReleaseRTHandles();
            ReleaseBuiltinHWRTRenderingPass();
            GraphicManager.Instance.UnregisterAllUniRTRenderers();
        }

        private void OnDestroy()
        {
            ReleaseRayTracingAccelStruct();
        }
        #endregion
    }
}