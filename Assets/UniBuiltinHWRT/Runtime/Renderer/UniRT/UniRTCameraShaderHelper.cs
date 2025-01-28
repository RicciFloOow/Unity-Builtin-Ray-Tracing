using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBuiltinHWRT
{
    public partial class UniRTCamera : MonoBehaviour
    {
        #region ----Int/Uint/Float----
        private static readonly int k_ShaderProperty_Uint_RTCurrentFrame = Shader.PropertyToID("_RTCurrentFrame");
        private static readonly int k_ShaderProperty_Float_Zoom = Shader.PropertyToID("_Zoom");
        #endregion

        #region ----RT Accel Struct----
        private static readonly int k_ShaderProperty_RTAccStruct = Shader.PropertyToID("_SceneAccelStruct");
        #endregion

        #region ----Texs----
        private static readonly int k_ShaderProperty_Tex_SkyboxCubeTex = Shader.PropertyToID("_SkyboxCubeTex");
        private static readonly int k_ShaderProperty_Tex_RW_ResultRT = Shader.PropertyToID("RW_ResultRT");
        #endregion
    }
}