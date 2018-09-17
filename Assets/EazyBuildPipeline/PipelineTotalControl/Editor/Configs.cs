#pragma warning disable 0649
using EazyBuildPipeline.Common.Editor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace EazyBuildPipeline.PipelineTotalControl.Editor
{
    public static class G
    {
        public static Configs.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
            public EditorWindow MainWindow;
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
namespace EazyBuildPipeline.PipelineTotalControl.Editor.Configs
{
    [Serializable]
    public class Configs : EBPConfigs
    {
        public override string ModuleName { get { return "TotalControl"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline TotalControl LocalConfig";
        public LocalConfig LocalConfig = new LocalConfig();
        public AssetPreprocessor.Editor.Configs.Configs AssetPreprocessorConfigs;
        public BundleManager.Editor.Configs.Configs BundleManagerConfigs;
        public PackageManager.Editor.Configs.Configs PackageManagerConfigs;
        public PlayerBuilder.Editor.Configs.Configs PlayerBuilderConfigs;

        public Configs()
        {
        }

        public bool LoadAllConfigs(string rootPath = null)
        {
            bool success = true;
            if (!LoadCommonLocalConfig()) return false;
            success &= LoadCommonTagEnumConfig();
            success &= LoadCommonAssetsTagsConfig();

            if (!LoadLocalConfig(rootPath)) return false;
            success &= LoadAssetPreprocessorConfig();
            success &= LoadBundleManagerConfig();
            success &= LoadPackageManagerConfig();
            success &= LoadPlayerBuilderConfig();
            return success;
        }

        public bool LoadPackageManagerConfig()
        {
            PackageManagerConfigs = new PackageManager.Editor.Configs.Configs();
            return PackageManagerConfigs.LoadAllConfigs(LocalConfig.Json.RootPath);
        }

        public bool LoadBundleManagerConfig()
        {
            BundleManagerConfigs = new BundleManager.Editor.Configs.Configs();
            return BundleManagerConfigs.LoadAllConfigs(LocalConfig.Json.RootPath);
        }

        public bool LoadAssetPreprocessorConfig()
        {
            AssetPreprocessorConfigs = new AssetPreprocessor.Editor.Configs.Configs();
            return AssetPreprocessorConfigs.LoadAllConfigs(LocalConfig.Json.RootPath);
        }

        public bool LoadPlayerBuilderConfig()
        {
            PlayerBuilderConfigs = new PlayerBuilder.Editor.Configs.Configs();
            PlayerBuilder.Editor.G.configs = PlayerBuilderConfigs; //这里需要将静态的configs与TotalControl中的configs同步
            return PlayerBuilderConfigs.LoadAllConfigs(LocalConfig.Json.RootPath);
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
                LocalConfig.JsonPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                LocalConfig.LocalRootPath = Path.GetDirectoryName(LocalConfig.JsonPath);
                LocalConfig.Load();
                if (rootPath != null)
                {
                    LocalConfig.Json.RootPath = rootPath;
                }
            }
            catch (Exception e)
            {
                DisplayDialog("加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.JsonPath
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + new LocalConfig().ToString());
                return false;
            }
            return true;
        }
    }

    [Serializable]
    public class LocalConfig : EBPConfig<LocalConfig.JsonClass>
    {
        public LocalConfig()
        {
            Json = new JsonClass();
        }
        //本地配置路径
        public string LocalRootPath;
        [Serializable]
        public class JsonClass
        {
            public string RootPath;
            public bool EnableCheckDiff;
        }
    }
}