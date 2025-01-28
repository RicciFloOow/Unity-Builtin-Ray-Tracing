using System.IO;
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using UnityEngine;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public class UniBuiltinHWRTShaderCreator : UnityEditor.Editor
    {
        #region ----Templates Name & Path----
        internal const string UniHWRTShaderTemplatePath_UniRenderer = "Assets/UniBuiltinHWRT/Editor/TemplateHelper/TemplateRes/NewHWRTShader.txt";
        internal const string UniHWRTShaderTemplateName_UniRenderer = "NewHWRTShader";
        #endregion

        internal static string TemplateName;

        [MenuItem(EditorConstantsUtil.MENU_CREAT_RTSHADER + "Unity Renderer RTS", false, -1)]
        public static void CreateHWRTShaderUniRenderer()
        {
            TemplateName = UniHWRTShaderTemplateName_UniRenderer;
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,
            CreateInstance<UniBuiltinHWRTShaderCreator_Asset>(),
            GetSelectedPathOrFallback() + "/" + UniHWRTShaderTemplateName_UniRenderer + ".shader",
            (Texture2D)UnityEditor.EditorGUIUtility.Load(EditorConstantsUtil.EDIT_ICON_SHADER),//注意，在这里EditorGUIUtility.FindTexture()并不能获取到built-in editor的纹理
            UniHWRTShaderTemplatePath_UniRenderer);
        }

        internal static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }

        internal class UniBuiltinHWRTShaderCreator_Asset : EndNameEditAction
        {
            internal static UnityEngine.Object CreateURPShaderTemplate(string pathName, string resourceFile, string tempName)
            {
                //读取文件
                StreamReader reader = new StreamReader(resourceFile);
                string shaderText = reader.ReadToEnd();
                reader.Close();
                //不需要检测是否有重名的情况
                //替换shader名
                string fileName = Path.GetFileNameWithoutExtension(pathName);
                shaderText = Regex.Replace(shaderText, "Hidden/" + tempName, "UniBuiltinHWRT/Custom/" + fileName);
                //写入文件
                UTF8Encoding encoding = new UTF8Encoding(true, false);
                StreamWriter writer = new StreamWriter(Path.GetFullPath(pathName), false, encoding);//
                writer.Write(shaderText);
                writer.Close();
                //
                AssetDatabase.ImportAsset(pathName);
                return AssetDatabase.LoadAssetAtPath(pathName, typeof(UnityEngine.Object));
            }

            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                UnityEngine.Object o = CreateURPShaderTemplate(pathName, resourceFile, TemplateName);
                ProjectWindowUtil.ShowCreatedAsset(o);
            }
        }
    }
#endif
}