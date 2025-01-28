using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using System.Security.Cryptography;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public class Dim1LUTGeneratorWindow : EditorWindow
    {
        #region ----Command----
        public static void InitWindow()
        {
            CurrentWin = GetWindowWithRect<Dim1LUTGeneratorWindow>(new Rect(0, 0, 340, 240));
            CurrentWin.titleContent = new GUIContent("Dim 1 LUT Generator", "利用Animation Curves生成一维的LUT");
            CurrentWin.Focus();
        }

        [MenuItem(EditorConstantsUtil.MENU_EDITORWINDOW + "Dim1LUT")]
        public static void OpenWindowFromMenu()
        {
            InitWindow();
        }
        #endregion

        #region ----Config----
        private const string k_DefaultExportTexPath = "Assets/ExportRes/Textures";
        private const string k_DefaultExportTexName = "Dim1LUT";
        private const int k_InlineGUIHeight = 20;
        private const int k_ChannelBlockStartHeight = 50;
        private const int k_ChannelBlockRHeight = k_ChannelBlockStartHeight + 5;
        private const int k_ChannelBlockGHeight = k_ChannelBlockRHeight + k_InlineGUIHeight + 5;
        private const int k_ChannelBlockBHeight = k_ChannelBlockGHeight + k_InlineGUIHeight + 5;
        private const int k_ChannelBlockAHeight = k_ChannelBlockBHeight + k_InlineGUIHeight + 5;
        #endregion

        #region ----Static Properties----
        public static Dim1LUTGeneratorWindow CurrentWin { get; private set; }

        private static TexFormat s_LUTFormat = TexFormat.RGBA32;
        private static TexSize s_LUTWidth = TexSize.L256;

        private static AnimationCurve s_channelRCurve;
        private static AnimationCurve s_channelGCurve;
        private static AnimationCurve s_channelBCurve;
        private static AnimationCurve s_channelACurve;
        private static string s_ExportTexPath = k_DefaultExportTexPath;
        private static string s_ExportTexName = "";
        #endregion

        #region ----Textures----
        private Texture2D m_previewTexR;
        private Texture2D m_previewTexG;
        private Texture2D m_previewTexB;
        private Texture2D m_previewTexA;

        private Texture2D m_Dim1LUT;

        private void ComputePreviewTexture(ref Texture2D tex, Vector4 colorChannel, AnimationCurve curve)
        {
            if (curve.keys.Length < 1)
            {
                return;
            }
            //
            Color32[] cols = new Color32[128];
            for (int i = 0; i < 128; i++)
            {
                float x = i / 127f;
                float y = curve.Evaluate(x);
                byte _c = (byte)Mathf.Round(Mathf.Clamp01(y) * 255f);
                cols[i] = new Color32(colorChannel.x > 0 ? _c : byte.MinValue, colorChannel.y > 0 ? _c : byte.MinValue, colorChannel.z > 0 ? _c : byte.MinValue, colorChannel.w > 0 ? _c : byte.MinValue);
            }
            //
            tex.SetPixels32(cols);
            tex.Apply();
        }

        private void SetupTextures()
        {
            //预览图不需要特别精细，因此我们就用128x1的rgba32格式的纹理
            m_previewTexR = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            m_previewTexG = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            m_previewTexB = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            m_previewTexA = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            //
            ComputePreviewTexture(ref m_previewTexR, new Vector4(1, 0, 0, 0), s_channelRCurve);
            ComputePreviewTexture(ref m_previewTexG, new Vector4(0, 1, 0, 0), s_channelGCurve);
            ComputePreviewTexture(ref m_previewTexB, new Vector4(0, 0, 1, 0), s_channelBCurve);
            ComputePreviewTexture(ref m_previewTexA, new Vector4(1, 1, 1, 1), s_channelACurve);
            //
            EditorGraphicsUtility.GetTextureFormat(s_LUTFormat, out TextureFormat textureFormat, out GraphicsFormat graphicsFormat);
            m_Dim1LUT = new Texture2D((int)s_LUTWidth, 1, textureFormat, false);
        }

        private void ReleaseTextures()
        {
            EditorGraphicsUtility.SafeReleaseTexture(ref m_previewTexR);
            EditorGraphicsUtility.SafeReleaseTexture(ref m_previewTexG);
            EditorGraphicsUtility.SafeReleaseTexture(ref m_previewTexB);
            EditorGraphicsUtility.SafeReleaseTexture(ref m_previewTexA);
            EditorGraphicsUtility.SafeReleaseTexture(ref m_Dim1LUT);
        }
        #endregion

        #region ----Export Texture----
        private void ExportLUT()
        {
            bool isSingleChannel = (s_LUTFormat == TexFormat.R8) || (s_LUTFormat == TexFormat.R16) || (s_LUTFormat == TexFormat.R32);
            bool isTwoChannel = (s_LUTFormat == TexFormat.R8G8) || (s_LUTFormat == TexFormat.R16G16) || (s_LUTFormat == TexFormat.R32G32);
            int _lutWidth = (int)s_LUTWidth;
            Color[] cols = new Color[_lutWidth];
            for (int i = 0; i < _lutWidth; i++)
            {
                float x = i / (float)_lutWidth;
                float r, g, b, a;
                r = s_channelRCurve.Evaluate(x);
                if (isSingleChannel)
                {
                    g = r;
                    b = r;
                    a = r;
                }
                else if (isTwoChannel)
                {
                    g = s_channelGCurve.Evaluate(x);
                    b = g;
                    a = g;
                }
                else
                {
                    g = s_channelGCurve.Evaluate(x);
                    b = s_channelBCurve.Evaluate(x);
                    a = s_channelACurve.Evaluate(x);
                }
                //
                cols[i] = new Color(r, g, b, a);
            }
            m_Dim1LUT.SetPixels(cols);
            m_Dim1LUT.Apply();
            //
            if (s_LUTFormat == TexFormat.RGBA32 || s_LUTFormat == TexFormat.R8)
            {
                EditorFileUtility.ExportTextureToPNG(m_Dim1LUT, ref s_ExportTexPath, ref s_ExportTexName, k_DefaultExportTexPath, k_DefaultExportTexName);
            }
            else
            {
                bool isFloat32 = (s_LUTFormat == TexFormat.R32G32B32A32) || (s_LUTFormat == TexFormat.R32G32) || (s_LUTFormat == TexFormat.R32);
                EditorFileUtility.ExportTextureToEXR(m_Dim1LUT, ref s_ExportTexPath, ref s_ExportTexName, k_DefaultExportTexPath, k_DefaultExportTexName, isFloat32);
            }
        }
        #endregion

        #region ----GUI----
        private void InitAnimationCurves()
        {
            if (s_channelRCurve == null)
            {
                s_channelRCurve = new AnimationCurve();
                s_channelRCurve.CopyFrom(AnimationCurve.Constant(0, 1, 1));
            }
            if (s_channelGCurve == null)
            {
                s_channelGCurve = new AnimationCurve();
                s_channelGCurve.CopyFrom(AnimationCurve.Constant(0, 1, 1));
            }
            if (s_channelBCurve == null)
            {
                s_channelBCurve = new AnimationCurve();
                s_channelBCurve.CopyFrom(AnimationCurve.Constant(0, 1, 1));
            }
            if (s_channelACurve == null)
            {
                s_channelACurve = new AnimationCurve();
                s_channelACurve.CopyFrom(AnimationCurve.Constant(0, 1, 1));
            }
        }

        private void DrawSettingPopup<T>(float startY, string settingLabel, ref T setting) where T : System.Enum
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.PrefixLabel(new Rect(10, startY, 20, k_InlineGUIHeight), new GUIContent(settingLabel));
            setting = (T)EditorGUI.EnumPopup(new Rect(70, startY, 150, k_InlineGUIHeight), setting);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawChannelAnimationCurve(float startY, string channelLabel, Vector4 colorChannel, ref AnimationCurve curve, ref Texture2D previewTex)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.PrefixLabel(new Rect(10, startY, 20, k_InlineGUIHeight), new GUIContent(channelLabel));
            EditorGUI.BeginChangeCheck();
            curve = EditorGUI.CurveField(new Rect(30, startY, 120, k_InlineGUIHeight), curve);
            if (EditorGUI.EndChangeCheck())
            {
                //重绘对应纹理
                ComputePreviewTexture(ref previewTex, colorChannel, curve);
            }
            EditorGUI.DrawPreviewTexture(new Rect(160, startY, 160, k_InlineGUIHeight), previewTex);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTextField(float startY, ref string text, string textLabel)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.PrefixLabel(new Rect(10, startY, 70, k_InlineGUIHeight), new GUIContent(textLabel));
            text = EditorGUI.TextField(new Rect(80, startY, 240, k_InlineGUIHeight), text);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            DrawSettingPopup<TexFormat>(5, "纹理格式: ", ref s_LUTFormat);
            DrawSettingPopup<TexSize>(30, "纹理宽度: ", ref s_LUTWidth);
            if (EditorGUI.EndChangeCheck())
            {
                EditorGraphicsUtility.GetTextureFormat(s_LUTFormat, out TextureFormat textureFormat, out GraphicsFormat graphicsFormat);
                m_Dim1LUT.Reinitialize((int)s_LUTWidth, 1, textureFormat, false);
            }
            switch (s_LUTFormat)
            {
                case TexFormat.RGBA32:
                case TexFormat.R16G16B16A16:
                case TexFormat.R32G32B32A32:
                    DrawChannelAnimationCurve(k_ChannelBlockRHeight, "R: ", new Vector4(1, 0, 0, 0), ref s_channelRCurve, ref m_previewTexR);
                    DrawChannelAnimationCurve(k_ChannelBlockGHeight, "G: ", new Vector4(0, 1, 0, 0), ref s_channelGCurve, ref m_previewTexG);
                    DrawChannelAnimationCurve(k_ChannelBlockBHeight, "B: ", new Vector4(0, 0, 1, 0), ref s_channelBCurve, ref m_previewTexB);
                    DrawChannelAnimationCurve(k_ChannelBlockAHeight, "A: ", new Vector4(1, 1, 1, 1), ref s_channelACurve, ref m_previewTexA);
                    break;
                case TexFormat.R8G8:
                case TexFormat.R16G16:
                case TexFormat.R32G32:
                    DrawChannelAnimationCurve(k_ChannelBlockRHeight, "R: ", new Vector4(1, 0, 0, 0), ref s_channelRCurve, ref m_previewTexR);
                    DrawChannelAnimationCurve(k_ChannelBlockGHeight, "G: ", new Vector4(0, 1, 0, 0), ref s_channelGCurve, ref m_previewTexG);
                    EditorGUI.BeginDisabledGroup(true);
                    DrawChannelAnimationCurve(k_ChannelBlockBHeight, "B: ", new Vector4(0, 0, 1, 0), ref s_channelBCurve, ref m_previewTexB);
                    DrawChannelAnimationCurve(k_ChannelBlockAHeight, "A: ", new Vector4(1, 1, 1, 1), ref s_channelACurve, ref m_previewTexA);
                    EditorGUI.EndDisabledGroup();
                    break;
                case TexFormat.R8:
                case TexFormat.R16:
                case TexFormat.R32:
                    DrawChannelAnimationCurve(k_ChannelBlockRHeight, "R: ", new Vector4(1, 0, 0, 0), ref s_channelRCurve, ref m_previewTexR);
                    EditorGUI.BeginDisabledGroup(true);
                    DrawChannelAnimationCurve(k_ChannelBlockGHeight, "G: ", new Vector4(0, 1, 0, 0), ref s_channelGCurve, ref m_previewTexG);
                    DrawChannelAnimationCurve(k_ChannelBlockBHeight, "B: ", new Vector4(0, 0, 1, 0), ref s_channelBCurve, ref m_previewTexB);
                    DrawChannelAnimationCurve(k_ChannelBlockAHeight, "A: ", new Vector4(1, 1, 1, 1), ref s_channelACurve, ref m_previewTexA);
                    EditorGUI.EndDisabledGroup();
                    break;
            }
            //
            DrawTextField(160, ref s_ExportTexPath, "导出路径:");
            DrawTextField(185, ref s_ExportTexName, "纹理名称:");
            //
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("导出纹理")))
            {
                //导出纹理
                ExportLUT();
            }
            EditorGUILayout.EndVertical();
        }
        #endregion


        #region ----Unity----
        private void OnEnable()
        {
            InitAnimationCurves();
            SetupTextures();
        }

        private void OnGUI()
        {
            DrawGUI();
        }

        private void OnDisable()
        {
            ReleaseTextures();
        }
        #endregion
    }
#endif
}