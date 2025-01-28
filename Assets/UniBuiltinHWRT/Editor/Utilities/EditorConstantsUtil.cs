using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public static class EditorConstantsUtil
    {
        #region ----Menu----
        private const string PREFIX = "HWRT ";
        public const string MENU_TOOLSTITLE = "UniBuiltinHWRT/Editor/";
        public const string MENU_SCRIPTS = "Scripts/UniBuiltin/";
        public const string MENU_PREFIX_SCRIPTS = MENU_SCRIPTS + PREFIX;
        public const string MENU_ASSETS = "UniHWRT/HWRTAssets/New ";
        public const string MENU_CREAT_RTSHADER = "Assets/Create/UniHWRT/RT Shader/New ";
        public const string MENU_EDITORWINDOW = "UniHWRT/EditWindows/";
        #endregion

        #region ----Unity Editor Icons----
        //ref: https://github.com/halak/unity-editor-icons
        public const string EDIT_ICON_SHADER = "Shader Icon";
        #endregion

        #region ----File Extension----
        public const string FILE_EXTENSION_PNG = ".png";
        public const string FILE_EXTENSION_EXR = ".exr";
        #endregion
    }
#endif
}