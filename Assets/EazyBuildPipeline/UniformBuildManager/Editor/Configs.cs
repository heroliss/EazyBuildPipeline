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
            public Action OnChangeConfigList = ()=> { };
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
    [Serializable]
    public class Configs : EBPConfigs
    {
        public override string ModuleName { get { return "UniformBuildManager"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline UniformBuildManager LocalConfig";
        public Runner Runner;
        public LocalConfig LocalConfig = new LocalConfig();
        public CurrentConfig CurrentConfig = new CurrentConfig();
        public BuildSettingConfig BuildSettingConfig = new BuildSettingConfig();
        public AssetPreprocessor.Editor.Configs.Configs AssetPreprocessorConfigs;
        public BundleManager.Editor.Configs.Configs BundleManagerConfigs;
        public PackageManager.Editor.Configs.Configs PackageManagerConfigs;
        
        public Configs()
        {
            Runner = new Runner(this);
        }

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
            success &= LoadCurrentConfig();
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
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + LocalConfig.ToString());
                return false;
            }
            return true;
        }

        public bool LoadCurrentConfig()
        {
            try
            {
                if (Directory.Exists(LocalConfig.Json.RootPath))
                {
                    CurrentConfig.JsonPath = LocalConfig.PlayersConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(CurrentConfig.JsonPath)))
                    {
                        if (!File.Exists(CurrentConfig.JsonPath))
                        {
                            File.Create(CurrentConfig.JsonPath).Close();
                            CurrentConfig.Save();
                        }
                        CurrentConfig.Load();
                        return true;
                    }
                    else
                    {
                        DisplayDialog("不是有效的Pipeline根目录:" + LocalConfig.Json.RootPath +
                           "\n\n若要新建一个此工具可用的Pipeline根目录，确保存在如下目录即可：" + Path.GetDirectoryName(CurrentConfig.JsonPath));
                        return false;
                    }
                }
                else
                {
                    DisplayDialog("根目录不存在:" + LocalConfig.Json.RootPath);
                    return false;
                }
            }
            catch (Exception e)
            {
                DisplayDialog("加载当前配置时发生错误：" + e.Message);
                return false;
            }
        }

        public bool LoadCurrentBuildSetting()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentConfig.Json.CurrentBuildSettingName))
                {
                    string currentBuildSettingPath = Path.Combine(LocalConfig.Local_BuildSettingsFolderPath, CurrentConfig.Json.CurrentBuildSettingName);
                    BuildSettingConfig.JsonPath = currentBuildSettingPath;
                    BuildSettingConfig.Load();
                    return true;
                }
                else
                {
                    CurrentConfig.Json.CurrentBuildSettingName = null;
                    BuildSettingConfig.JsonPath = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "载入BuildSetting文件：" + CurrentConfig.Json.CurrentBuildSettingName + " 时发生错误：" + e.Message, "确定");
                CurrentConfig.Json.CurrentBuildSettingName = null;
                BuildSettingConfig.JsonPath = null;
                return false;
            }
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
        public string Local_BuildSettingsFolderPath { get { return Path.Combine(LocalRootPath, Json.Local_BuildSettingsFolderRelativePath); } }
        //Pipeline配置路径
        public string PlayersFolderPath { get { return Path.Combine(Json.RootPath, Json.PlayersFolderRelativePath); } }
        public string PlayersConfigPath { get { return Path.Combine(Json.RootPath, Json.PlayersConfigRelativePath); } }
        [Serializable]
        public class JsonClass
        {
            public string Local_BuildSettingsFolderRelativePath;
            public string RootPath;
            public string PlayersFolderRelativePath;
            public string PlayersConfigRelativePath;
        }
    }

    [Serializable]
    public class CurrentConfig : EBPConfig<CurrentConfig.JsonClass>
    {
        public CurrentConfig()
        {
            Json = new JsonClass();
        }
        [Serializable]
        public class JsonClass
        {
            public string CurrentBuildSettingName;
            public bool Applying;
            public bool IsPartOfPipeline;
        }
    }

    [Serializable]
    public class BuildSettingConfig : EBPConfig<BuildSettingConfig.JsonClass>
    {
        public BuildSettingConfig()
        {
            Json = new JsonClass();
        }
        public bool Dirty;
        [Serializable]
        public class JsonClass
        {

        }
    }
}