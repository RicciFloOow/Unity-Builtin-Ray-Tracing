using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBuiltinHWRT.Editor
{
#if UNITY_EDITOR
    public enum TexFormat
    {
        RGBA32,
        R16G16B16A16,
        R32G32B32A32,
        R8G8,
        R16G16,
        R32G32,
        R8,
        R16,
        R32
    }

    public enum TexSize
    {
        [InspectorName("4096")]
        L4096 = 4096,
        [InspectorName("2048")]
        L2048 = 2048,
        [InspectorName("1024")]
        L1024 = 1024,
        [InspectorName("512")]
        L512 = 512,
        [InspectorName("256")]
        L256 = 256,
        [InspectorName("128")]
        L128 = 128,
        [InspectorName("64")]
        L64 = 64,
        [InspectorName("32")]
        L32 = 32,
        [InspectorName("16")]
        L16 = 16,
        [InspectorName("8")]
        L8 = 8,
        [InspectorName("4")]
        L4 = 4,
        [InspectorName("2")]
        L2 = 2,
        [InspectorName("1")]
        L1 = 1
    }
#endif
}