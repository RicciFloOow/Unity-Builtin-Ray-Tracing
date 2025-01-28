using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public static class EditorFileUtility
    {


        #region ----Texture----
        public static void ExportTextureToPNG(Texture2D texture, ref string exportPath, ref string exportName, string defaultExportPath, string defaultExportName)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = defaultExportPath;
            }
            if (string.IsNullOrEmpty(exportName))
            {
                exportName = defaultExportName;
            }
            //
            System.IO.Directory.CreateDirectory(exportPath);
            string filePath = exportPath + "/" + exportName + EditorConstantsUtil.FILE_EXTENSION_PNG;
            System.IO.File.WriteAllBytes(filePath, texture.EncodeToPNG());
            AssetDatabase.Refresh();
            Debug.Log("成功导出至:" + filePath);
        }

        public static void ExportTextureToEXR(Texture2D texture, ref string exportPath, ref string exportName, string defaultExportPath, string defaultExportName, bool useFloat32 = false)
        {
            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = defaultExportPath;
            }
            if (string.IsNullOrEmpty(exportName))
            {
                exportName = defaultExportName;
            }
            //
            System.IO.Directory.CreateDirectory(exportPath);
            string filePath = exportPath + "/" + exportName + EditorConstantsUtil.FILE_EXTENSION_EXR;
            System.IO.File.WriteAllBytes(filePath, texture.EncodeToEXR(useFloat32 ? Texture2D.EXRFlags.OutputAsFloat : Texture2D.EXRFlags.None));
            AssetDatabase.Refresh();
            Debug.Log("成功导出至:" + filePath);
        }
        #endregion
    }
#endif
}