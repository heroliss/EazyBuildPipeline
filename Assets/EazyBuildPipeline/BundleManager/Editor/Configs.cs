#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor;
using System.Linq;

namespace EazyBuildPipeline.BundleManager.Configs
{
    [Serializable]
    public class ModuleConfig : Common.Configs.ModuleConfig<ModuleConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass : Common.Configs.ModuleConfigJsonClass
        {
        }
    }

    [Serializable]
    public class ModuleStateConfig : Common.Configs.ModuleStateConfig<ModuleStateConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass : Common.Configs.ModuleStateConfigJsonClass
        {
            public BuildAssetBundleOptions CompressionOption
            {
                get
                {
                    return
                        (CurrentBuildAssetBundleOptionsValue & (int)BuildAssetBundleOptions.ChunkBasedCompression) == 0 ?
                        (CurrentBuildAssetBundleOptionsValue & (int)BuildAssetBundleOptions.UncompressedAssetBundle) == 0 ?
                        BuildAssetBundleOptions.None :
                        BuildAssetBundleOptions.UncompressedAssetBundle :
                        BuildAssetBundleOptions.ChunkBasedCompression;
                }
                set
                {
                    CurrentBuildAssetBundleOptionsValue -= (int)CompressionOption;
                    CurrentBuildAssetBundleOptionsValue += (int)value;
                }
            }
            public int CurrentBuildAssetBundleOptionsValue;
            public int CurrentResourceVersion;
        }
    }

    [Serializable]
    public class UserConfig : EBPConfig<List<AssetBundleBuild>>
    {
    }
}
