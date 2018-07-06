#pragma warning disable 0649
using EazyBuildPipeline.Common.Editor;
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

        //public static string[] NecesseryEnum = new string[] { "Immediate", "Delayed" };
        //public static string[] DeploymentLocationEnum = new string[] { "Built-in", "Server" };
        //public static string[] PackageModeEnum = new string[] { "Addon", "Patch" };
        //public static string[] LuaSourceEnum = new string[] { "None", "Origin", "ByteCode", "Encrypted" };
    }
}
namespace EazyBuildPipeline.UniformBuildManager.Editor.Configs
{
    public class Configs
    {
        private readonly string localConfigSearchText = "EazyBuildPipeline UniformBuildManager LocalConfig";
        public LocalConfig LocalConfig = new LocalConfig();
        public AssetPreprocessor.Editor.Configs.Configs AssetPreprocessorConfigs;
        public BundleManager.Editor.Configs.Configs BundleManagerConfigs;
        public PackageManager.Editor.Configs.Configs PackageManagerConfigs;
        
        public bool LoadAllConfigsByLocalConfig()
        {
            bool success = true;
            AssetPreprocessorConfigs = new AssetPreprocessor.Editor.Configs.Configs();
            if (AssetPreprocessorConfigs.LoadLocalConfig())
            {
                AssetPreprocessorConfigs.LocalConfig.RootPath = LocalConfig.RootPath;
                success = success && AssetPreprocessorConfigs.LoadAllConfigsByLocalConfig();
            }

            BundleManagerConfigs = new BundleManager.Editor.Configs.Configs();
            if (BundleManagerConfigs.LoadLocalConfig())
            {
                BundleManagerConfigs.LocalConfig.RootPath = LocalConfig.RootPath;
                success = success && BundleManagerConfigs.LoadAllConfigsByLocalConfig();
            }

            PackageManagerConfigs = new PackageManager.Editor.Configs.Configs();
            if (PackageManagerConfigs.LoadLocalConfig())
            {
                PackageManagerConfigs.LocalConfig.RootPath = LocalConfig.RootPath;
                success = success && PackageManagerConfigs.LoadAllConfigsByLocalConfig();
            }
            
            return success;
        }
        public bool LoadLocalConfig()
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
            }
            catch (Exception e)
            {
                UnityEditor.EditorUtility.DisplayDialog("错误", "加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.Path
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + LocalConfig.ToString(), "确定");
                return false;
            }
            return true;
        }
    }


    public class LocalConfig : EBPConfig
    {
        //本地配置路径
        public string Global_SettingIcon;
        [NonSerialized]
        public string LocalRootPath;
        //Pipeline配置路径
        public string RootPath;
    }
}