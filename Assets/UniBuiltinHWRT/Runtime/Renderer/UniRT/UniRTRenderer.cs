using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UniBuiltinHWRT
{
    public class UniRTRenderer : MonoBehaviour
    {
        #region ----Rendering Settings----
        public RayTracingSubMeshFlags[] RTSubMeshFlags;
        #endregion

        public Renderer UniRenderer => m_uniRenderer;
        private Renderer m_uniRenderer;

        #region ----Unity----
        private void OnEnable()
        {
            GraphicManager.Instance.RegisterRenderer(this);
            //
            if (m_uniRenderer == null)
            {
                m_uniRenderer = GetComponent<Renderer>();
            }
        }

        private void OnDisable()
        {
            GraphicManager.Instance.UnregisterRenderer(this);
        }
        #endregion
    }
}