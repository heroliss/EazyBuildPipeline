using System.Collections.Generic;
using UnityEngine;

namespace EazyBuildPipeline.AssetPreprocessor_old.Editor
{
    public static class StringMaps
    {
        public static Dictionary<string, TextureFormat> TextureMaps = new Dictionary<string, TextureFormat>
        {
            { "RGB PVRTC 2bits",TextureFormat.PVRTC_RGB2 },
            { "RGBA PVRTC 2bits",TextureFormat.PVRTC_RGBA2 },
            { "RGB PVRTC 4bits",TextureFormat.PVRTC_RGB4 },
            { "RGBA PVRTC 4bits",TextureFormat.PVRTC_RGBA4 },
            { "RGB ASTC 4x4",TextureFormat.ASTC_RGB_4x4 },
            { "RGBA ASTC 4x4",TextureFormat.ASTC_RGBA_4x4 },
            { "RGB 24bits",TextureFormat.RGB24 },
            { "RGB 16bits",TextureFormat.RGB565 },
            { "Alpha 8",TextureFormat.Alpha8 },
            { "RGBA 16bits",TextureFormat.RGBA4444 },
            { "RGBA 32bits",TextureFormat.RGBA32 },
            { "ETC2 RGBA 8bits",TextureFormat.ETC2_RGBA8Crunched },
        };
    }
}
