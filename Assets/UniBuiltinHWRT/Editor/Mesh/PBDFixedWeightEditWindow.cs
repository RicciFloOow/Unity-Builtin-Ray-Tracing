using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public partial class PBDFixedWeightEditWindow : EditorWindow
    {
        #region ----Command----
        public static void InitWindow(PBDClothMeshData meshData)
        {
            s_selectPBDClothMeshData = meshData;
            //
            CurrentWin = GetWindowWithRect<PBDFixedWeightEditWindow>(new Rect(0, 0, k_GUI_WindowWidth, k_GUI_WindowHeight));
            CurrentWin.titleContent = new GUIContent("PBD Fixed Weights Editor", "修改顶点Fixed的权重");
            CurrentWin.Focus();
        }
        #endregion

        #region ----GUI Constants----
        private const float k_GUI_WindowWidth = 1590;
        private const float k_GUI_WindowHeight = 730;
        private const float k_GUI_EditPanel_ScrollViewWidth = 290;

        private const int k_GUI_SceneViewTex_Width = 1280;
        private const int k_GUI_SceneViewTex_Height = 720;

        //ref: https://www.rapidtables.com/web/color/RGB_Color.html
        private const string k_Text_ColorDeepSkyBlueTag = "<color=#00BFFF>";
        private const string k_Text_ColorGreenYellowTag = "<color=#ADFF2F>";
        private const string k_Text_ColorPaleTurquoiseTag = "<color=#AFEEEE>";
        private const string k_Text_ColorChocolateTag = "<color=#AFEEEE>";
        private const string k_Text_ColorCrimsonTag = "<color=#DC143C>";
        private const string k_Text_ColorEndTag = "</color>";
        #endregion

        #region ----GUI Properties----
        private Vector2 m_ScrollPosition;
        private float m_SelectedFixedPointWeight;
        private bool m_IsFixedVerticesValueChanged;
        #endregion

        #region ----Static Properties----
        public static PBDFixedWeightEditWindow CurrentWin { get; private set; }

        private static PBDClothMeshData s_selectPBDClothMeshData;
        #endregion

        #region ----Scene View RTs----
        private RTHandle m_VCamColor_Handle;
        private RTHandle m_VCamDepth_Handle;

        private RTHandle m_VCamFixedPoints_Handle;

        private void SetupVirtualCameraHandles()
        {
            m_VCamColor_Handle = new RTHandle(k_GUI_SceneViewTex_Width, k_GUI_SceneViewTex_Height, 0, GraphicsFormat.R8G8B8A8_UNorm);
            m_VCamFixedPoints_Handle = new RTHandle(k_GUI_SceneViewTex_Width, k_GUI_SceneViewTex_Height, 0, GraphicsFormat.R8G8B8A8_UNorm, 0, true);
            m_VCamDepth_Handle = new RTHandle(k_GUI_SceneViewTex_Width, k_GUI_SceneViewTex_Height, GraphicsFormat.None, GraphicsFormat.D32_SFloat);
        }

        private void ReleaseVirtualCameraHandles()
        {
            m_VCamColor_Handle?.Release();
            m_VCamDepth_Handle?.Release();
            m_VCamFixedPoints_Handle?.Release();
            m_VCamColor_Handle = null;
            m_VCamDepth_Handle = null;
            m_VCamFixedPoints_Handle = null;
        }
        #endregion

        #region ----GUI----
        private string GetEditWindowControlDesc()
        {
            StringBuilder sb = new StringBuilder();
            //
            sb.Append(k_Text_ColorDeepSkyBlueTag);
            sb.Append("【操作说明】\n");
            sb.Append(k_Text_ColorEndTag);
            //
            sb.Append("1.按住");
            sb.Append(k_Text_ColorGreenYellowTag);
            sb.Append("【鼠标右键】");
            sb.Append(k_Text_ColorEndTag);
            sb.Append("以改变相机朝向\n");
            //
            sb.Append("2.按住");
            sb.Append(k_Text_ColorGreenYellowTag);
            sb.Append("【鼠标中键】");
            sb.Append(k_Text_ColorEndTag);
            sb.Append("以拖动相机\n");
            //
            sb.Append("3.按下");
            sb.Append(k_Text_ColorGreenYellowTag);
            sb.Append("【鼠标左键】");
            sb.Append(k_Text_ColorEndTag);
            sb.Append("以选择最近的顶点\n");
            //
            sb.Append("4.按住");
            sb.Append(k_Text_ColorGreenYellowTag);
            sb.Append("【W/A/S/D】");
            sb.Append(k_Text_ColorEndTag);
            sb.Append("以移动相机");
            return sb.ToString();
        }

        private void DrawEditPanel()
        {
            GUILayout.Space(5);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Width(k_GUI_EditPanel_ScrollViewWidth));
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            //
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                //介绍
                GUILayout.Space(5f);
                GUILayout.Label(GetEditWindowControlDesc(), EditorGUIUtility.RichTextLabel);
                GUILayout.Space(5f);
                //
                EditorGUILayout.EndVertical();
            }
            //
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                //相机参数
                DrawVirtualCameraSettingsGUI();
                //
                EditorGUILayout.EndVertical();
            }
            //
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (m_selectVertexIndex >= 0)
            {
                EditorGUI.BeginChangeCheck();
                m_SelectedFixedPointWeight = EditorGUILayout.Slider(new GUIContent("顶点权重:", "1:表示完全由骨骼确定; 0:表示完全不受骨骼直接影响"), m_SelectedFixedPointWeight, 0, 1);
                if (EditorGUI.EndChangeCheck())
                {
                    m_IsFixedVerticesValueChanged = true;
                    m_verticesFixedWeights[m_selectVertexIndex] = m_SelectedFixedPointWeight;
                    //
                    m_EditPBDClothFixedVertBuffer.SetData(m_verticesFixedWeights);
                }
            }
            //
            if (m_IsFixedVerticesValueChanged)
            {
                if (GUILayout.Button(new GUIContent("应用更改", "保存(全部)Fixed Vertices的权重")))
                {
                    m_IsFixedVerticesValueChanged = false;
                    //
                    var _pbdFixedWeights = new List<PBDFixedWeight>();
                    for (int i = 0; i < m_verticesFixedWeights.Length; i++)
                    {
                        var _w = m_verticesFixedWeights[i];
                        if (_w > 0)
                        {
                            _pbdFixedWeights.Add(new PBDFixedWeight(i, _w));
                        }
                    }
                    //
                    s_selectPBDClothMeshData.VerticesFixedWeight = _pbdFixedWeights.ToArray();
                    //
                    s_selectPBDClothMeshData.ForceSave();
                }
            }
            EditorGUILayout.EndVertical();
            //
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void DrawSceneView(out Rect sceneViewRect)
        {
            //
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MaxWidth(k_GUI_SceneViewTex_Width + 10), GUILayout.MaxHeight(k_GUI_SceneViewTex_Height + 10));
            sceneViewRect = GUILayoutUtility.GetRect(k_GUI_SceneViewTex_Width, k_GUI_SceneViewTex_Height, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
            EditorGUI.DrawPreviewTexture(sceneViewRect, m_VCamColor_Handle, null, ScaleMode.StretchToFill);
            EditorGUILayout.EndVertical();
        }

        private void DrawGUI()
        {
            if (s_selectPBDClothMeshData  == null)
            {
                return;
            }
            GUILayout.BeginHorizontal();
            DrawEditPanel();
            DrawSceneView(out Rect sceneViewRect);
            GUILayout.EndHorizontal();
            //
            OnControlVirtualCamera(sceneViewRect);
        }
        #endregion

        #region ----Unity----
        private void OnEnable()
        {
            SetupVirtualCameraHandles();
            SetupDefaultTransforms();
            SetupMaterials();
            LoadEditAdditionalRenderingRes();
            SetupMeshDataBuffers();
            //
            m_lastFrameTime = EditorApplication.timeSinceStartup;
            //
            m_selectVertexIndex = -1;
        }

        private void OnGUI()
        {
            DrawGUI();
        }

        private void Update()
        {
            OnRenderingEditorVirtualCamera();
        }

        private void OnDisable()
        {
            ReleaseVirtualCameraHandles();
            ReleaseMaterials();
            ReleaseEditAdditionalRenderingRes();
            ReleaseMeshDataBuffers();
        }
        #endregion
    }
#endif
}