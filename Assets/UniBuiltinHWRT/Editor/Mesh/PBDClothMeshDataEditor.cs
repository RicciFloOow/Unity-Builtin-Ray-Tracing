using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    [CustomEditor(typeof(PBDClothMeshData))]
    public class PBDClothMeshDataEditor : UnityEditor.Editor
    {
        #region ----Private Properties----
        private PBDClothMeshData m_pbdClothMeshData;
        #endregion


        #region ----Unity----
        private void OnEnable()
        {
            m_pbdClothMeshData = (PBDClothMeshData)target;
        }

        public override void OnInspectorGUI()
        {
            //
            if (m_pbdClothMeshData.Vertices != null)
            {
                EditorGUILayout.LabelField("Vertices Count:", m_pbdClothMeshData.Vertices.Length.ToString());
            }
            if (m_pbdClothMeshData.Indices != null)
            {
                EditorGUILayout.LabelField("Triangles Count:", (m_pbdClothMeshData.Indices.Length / 3).ToString());
            }
            if (m_pbdClothMeshData.EdgesPerVertex != null)
            {
                EditorGUILayout.LabelField("Edges Count:", (m_pbdClothMeshData.AllVertexEdges.Length / 2).ToString());
            }
            if (m_pbdClothMeshData.SharedEdgesPerVertex != null)
            {
                EditorGUILayout.LabelField("Shared Edges Count:", (m_pbdClothMeshData.AllVertexSharedEdges.Length / 2).ToString());
            }
            if (m_pbdClothMeshData.VerticesFixedWeight != null)
            {
                EditorGUILayout.LabelField("Fixed Vertices Count:", (m_pbdClothMeshData.VerticesFixedWeight.Length).ToString());
            }
            else
            {
                EditorGUILayout.LabelField("Fixed Vertices Count:", "0");
            }
            //
            if (GUILayout.Button("编辑Fixed顶点"))
            {
                PBDFixedWeightEditWindow.InitWindow(m_pbdClothMeshData);
            }
        }
        #endregion
    }
#endif
}