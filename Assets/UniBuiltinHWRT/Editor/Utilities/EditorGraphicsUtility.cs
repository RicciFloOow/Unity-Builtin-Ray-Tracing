using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public static class EditorGraphicsUtility
    {
        public static bool GetTextureFormat(TexFormat texFormat, out TextureFormat textureFormat, out GraphicsFormat graphicsFormat)
        {
            textureFormat = TextureFormat.RGBA32;
            graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            //
            switch (texFormat)
            {
                case TexFormat.RGBA32:
                    return false;
                case TexFormat.R16G16B16A16:
                    textureFormat = TextureFormat.RGBAHalf;
                    graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                    return true;
                case TexFormat.R32G32B32A32:
                    textureFormat = TextureFormat.RGBAFloat;
                    graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
                    return true;
                case TexFormat.R8G8:
                    textureFormat = TextureFormat.RG16;
                    graphicsFormat = GraphicsFormat.R8G8_UNorm;
                    return false;
                case TexFormat.R16G16:
                    textureFormat = TextureFormat.RGHalf;
                    graphicsFormat = GraphicsFormat.R16G16_SFloat;
                    return true;
                case TexFormat.R32G32:
                    textureFormat = TextureFormat.RGFloat;
                    graphicsFormat = GraphicsFormat.R32G32_SFloat;
                    return true;
                case TexFormat.R8:
                    textureFormat = TextureFormat.R8;
                    graphicsFormat = GraphicsFormat.R8_UNorm;
                    return false;
                case TexFormat.R16:
                    textureFormat = TextureFormat.RHalf;
                    graphicsFormat = GraphicsFormat.R16_SFloat;
                    return true;
                case TexFormat.R32:
                    textureFormat = TextureFormat.RFloat;
                    graphicsFormat = GraphicsFormat.R32_SFloat;
                    return true;
            }
            return false;
        }

        public static void SafeReleaseTexture(ref Texture2D tex)
        {
            if (tex != null)
            {
                UnityEngine.Object.DestroyImmediate(tex);
                tex = null;
            }
        }
    }
#endif
}