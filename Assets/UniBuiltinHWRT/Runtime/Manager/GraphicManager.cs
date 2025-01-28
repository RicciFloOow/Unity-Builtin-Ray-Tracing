using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBuiltinHWRT
{
    public class GraphicManager
    {
        #region ----Singleton----
        private static GraphicManager instance;
        public static GraphicManager Instance
        {
            get
            {
                return instance ??= new GraphicManager();
            }
        }
        #endregion

        #region ----Constructor----
        public GraphicManager()
        {
            m_UniRTRendererList = new List<UniRTRenderer>();
            m_InstanceRTRendererList = new List<InstanceRTRenderer>();
            m_PBDClothColliderList = new List<PBDClothCollider>();
        }
        #endregion

        #region ----UniRTRenderer----
        public IReadOnlyList<UniRTRenderer> UniRTRendererList => m_UniRTRendererList.AsReadOnly();
        private List<UniRTRenderer> m_UniRTRendererList;

        public void RegisterRenderer(UniRTRenderer renderer)
        {
            if (!m_UniRTRendererList.Contains(renderer))
            {
                m_UniRTRendererList.Add(renderer);
            }
        }

        public void UnregisterRenderer(UniRTRenderer renderer)
        {
            if (m_UniRTRendererList.Contains(renderer))
            {
                m_UniRTRendererList.Remove(renderer);
            }
        }

        public void UnregisterAllUniRTRenderers()
        {
            m_UniRTRendererList.Clear();
        }
        #endregion

        #region ----InstanceRTRenderer----
        public IReadOnlyList<InstanceRTRenderer> InstanceRTRendererList => m_InstanceRTRendererList.AsReadOnly();
        private List<InstanceRTRenderer> m_InstanceRTRendererList;

        public void RegisterRenderer(InstanceRTRenderer renderer)
        {
            if (!m_InstanceRTRendererList.Contains(renderer))
            {
                m_InstanceRTRendererList.Add(renderer);
            }
        }

        public void UnregisterRenderer(InstanceRTRenderer renderer)
        {
            if (m_InstanceRTRendererList.Contains(renderer))
            {
                m_InstanceRTRendererList.Remove(renderer);
            }
        }

        public void UnregisterAllInstanceRTRenderers()
        {
            m_InstanceRTRendererList.Clear();
        }
        #endregion

        #region ----PBD Cloth Simulation Collider----
        public IReadOnlyList<PBDClothCollider> PBDClothColliderList => m_PBDClothColliderList.AsReadOnly();
        private List<PBDClothCollider> m_PBDClothColliderList;

        public void RegisterCollider(PBDClothCollider collider)
        {
            if (!m_PBDClothColliderList.Contains(collider))
            {
                m_PBDClothColliderList.Add(collider);
            }
        }

        public void UnregisterCollider(PBDClothCollider collider)
        {
            if (m_PBDClothColliderList.Contains(collider))
            {
                m_PBDClothColliderList.Remove(collider);
            }
        }

        public void UnregisterAllPBDClothColliders()
        {
            m_PBDClothColliderList.Clear();
        }
        #endregion
    }
}