using UnityEngine;
using UnityEditor;
using EazyBuildPipeline.BundleManager.Configs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EazyBuildPipeline.BundleManager
{
    public static class G
    {
        public static Module Module;
        public static Runner Runner;
        public static GlobalReference g;
        public class GlobalReference
        {
            public Action OnChangeCurrentConfig = () => { };
            public Action OnChangeConfigList = () => { };
            public AssetBundleManagement2.AssetBundleMainWindow mainTab;
        }
        public static void Init()
        {
            Module = new Module();
            Runner = new Runner(Module);
            g = new GlobalReference();
        }
        public static void Clear()
        {
            Module = null;
            Runner = null;
            g = null;
        }
    }

    [Serializable]
    public class Module : EBPModule<
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public Dictionary<string, BuildAssetBundleOptions> CompressionEnumMap = new Dictionary<string, BuildAssetBundleOptions>
        {
            { "Uncompress",BuildAssetBundleOptions.UncompressedAssetBundle },
            { "LZMA",BuildAssetBundleOptions.None },
            { "LZ4" ,BuildAssetBundleOptions.ChunkBasedCompression}
        };
        public string[] CompressionEnum;

        public Module()
        {
            CompressionEnum = CompressionEnumMap.Keys.ToArray();
        }

        public override string ModuleName { get { return "BundleManager"; } }
        public UserConfig UserConfig = new UserConfig();

        public bool LoadAllConfigs(string pipelineRootPath = null)
        {
            if (!CommonModule.LoadCommonConfig()) return false;
            if (pipelineRootPath != null)
            {
                CommonModule.CommonConfig.Json.PipelineRootPath = pipelineRootPath;
            }
            bool success = 
                LoadModuleConfig() &&
                LoadModuleStateConfig();
            if (success)
            {
                LoadUserConfig();
            }
            return success;
        }

        public bool LoadUserConfig()
        {
            try
            {
                UserConfig.Load(ModuleStateConfig.CurrentUserConfigPath);
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载用户配置时发生错误：" + e.Message + "\n加载路径：" + UserConfig.JsonPath);
                return false;
            }
        }
    }
}