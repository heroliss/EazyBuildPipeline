#pragma warning disable 0649
using EazyBuildPipeline.Common.Editor;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEditor;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    public static class G
    {
        public static Configs.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
        }

        public static void Init()
        {
            configs = new Configs.Configs();
            g = new GlobalReference();
        }
        
        public static void Clear()
        {
            configs = null;
            g = null;
        }
    }
}
namespace EazyBuildPipeline.UniformBuildManager.Editor.Configs
{
    public class Configs : EBPConfigs
    {
        public override string ModuleName { get { return "UniformBuildManager"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline UniformBuildManager LocalConfig";
        public LocalConfig LocalConfig = new LocalConfig();
        public AssetPreprocessor.Editor.Configs.Configs AssetPreprocessorConfigs;
        public BundleManager.Editor.Configs.Configs BundleManagerConfigs;
        public PackageManager.Editor.Configs.Configs PackageManagerConfigs;
        
        public bool LoadAllConfigs(string rootPath = null)
        {
            if (!LoadCommonLocalConfig()) return false;
            if (!LoadCommonTagEnumConfig()) return false;
            if (!LoadCommonAssetsTagsConfig()) return false;

            if (!LoadLocalConfig(rootPath)) return false;
 
            bool success = true;
            success &= LoadAssetPreprocessorConfig();
            success &= LoadBundleManagerConfig();
            success &= LoadPackageManagerConfig();
            return success;
        }

        public bool LoadPackageManagerConfig()
        {
            PackageManagerConfigs = new PackageManager.Editor.Configs.Configs();
            return PackageManagerConfigs.LoadAllConfigs(LocalConfig.RootPath);
        }

        public bool LoadBundleManagerConfig()
        {
            BundleManagerConfigs = new BundleManager.Editor.Configs.Configs();
            return BundleManagerConfigs.LoadAllConfigs(LocalConfig.RootPath);
        }

        public bool LoadAssetPreprocessorConfig()
        {
            AssetPreprocessorConfigs = new AssetPreprocessor.Editor.Configs.Configs();
            return AssetPreprocessorConfigs.LoadAllConfigs(LocalConfig.RootPath);
        }

        public bool LoadLocalConfig(string rootPath = null)
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets(localConfigSearchText);
                if (guids.Length == 0)
                {
                    throw new ApplicationException("未能找到本地配置文件! 搜索文本：" + localConfigSearchText);
                }
                LocalConfig.Path = AssetDatabase.GUIDToAssetPath(guids[0]);
                LocalConfig.LocalRootPath = Path.GetDirectoryName(LocalConfig.Path);
                LocalConfig.Load();
                if (rootPath != null)
                {
                    LocalConfig.RootPath = rootPath;
                }
            }
            catch (Exception e)
            {
                DisplayDialog("加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.Path
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + LocalConfig.ToString());
                return false;
            }
            return true;
        }
    }

    public class LocalConfig : EBPConfig
    {
        //本地配置路径
        [NonSerialized]
        public string LocalRootPath;
        //Pipeline配置路径
        public string RootPath;
    }
}