using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public static class EditorGUIUtility
    {
        #region ----GUI Styles----
        public static GUIStyle RichTextLabel;
        #endregion

        static EditorGUIUtility()
        {
            RichTextLabel = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true, stretchWidth = true };
        }
    }
#endif
}