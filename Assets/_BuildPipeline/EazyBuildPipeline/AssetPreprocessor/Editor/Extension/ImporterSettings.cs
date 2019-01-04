using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.AssetPreprocessor.ImporterSettings
{
    [SetFor(typeof(ModelImporter), "t:Model")]
    [Serializable]
    public class SettingForModelImporter
    {
        //
        // 摘要:
        //     Animation clips to split animation into. See Also: ModelImporterClipAnimation.
        public ModelImporterClipAnimation[] clipAnimations;
    }

    [SetFor(typeof(AudioImporter), "t:AudioClip")]
    [Serializable]
    public class SettingForAudioImporter : ImporterSetting
    {
        public AudioClipLoadType loadType;
        //
        // 摘要:
        //     Defines how the sample rate is modified (if at all) of the importer audio file.
        public AudioSampleRateSetting sampleRateSetting;
        //
        // 摘要:
        //     Target sample rate to convert to when samplerateSetting is set to OverrideSampleRate.
        public uint sampleRateOverride;
        //
        // 摘要:
        //     CompressionFormat defines the compression type that the audio file is encoded
        //     to. Different compression types have different performance and audio artifact
        //     characteristics.
        public AudioCompressionFormat compressionFormat;
        //
        // 摘要:
        //     Audio compression quality (0-1) Amount of compression. The value roughly corresponds
        //     to the ratio between the resulting and the source file sizes.
        public float quality;
        public int conversionMode;

        public override bool Set(AssetImporter importer, IEnumerable<string> properties, string platform)
        {
            return false;
        }
    }


    [SetFor(typeof(TextureImporter), "t:Texture")]
    [Serializable]
    public class SettingForTextureImporter : ImporterSetting
    {
        //
        // 摘要:
        //     Maximum texture size.
        public int maxTextureSize = 1024;
        //
        // 摘要:
        //     Format of imported texture.
        public TextureImporterFormat format;
        //
        // 摘要:
        //     Quality of texture compression in the range [0..100].
        [Range(0, 100)]
        public int compressionQuality;

        public override bool Set(AssetImporter importer, IEnumerable<string> properties, string platform)
        {
            bool dirty = false;
            TextureImporter textureImporter = (TextureImporter)importer;
            var setting = textureImporter.GetPlatformTextureSettings(platform);
            if (setting.overridden != true)
            {
                setting.overridden = true;
                dirty = true;
            }
            foreach (var property in properties)
            {
                switch (property)
                {
                    case "maxTextureSize":
                        if (setting.maxTextureSize != maxTextureSize)
                        {
                            setting.maxTextureSize = maxTextureSize;
                            dirty = true;
                        }
                        break;
                    case "format":
                        if (setting.format != format)
                        {
                            setting.format = format;
                            dirty = true;
                        }
                        break;
                    case "compressionQuality":
                        if (setting.compressionQuality != compressionQuality)
                        {
                            setting.compressionQuality = compressionQuality;
                            dirty = true;
                        }
                        break;
                    default:
                        break;
                }
            }
            if (dirty)
            {
                textureImporter.SetPlatformTextureSettings(setting);
            }
            return dirty;
        }
    }
}