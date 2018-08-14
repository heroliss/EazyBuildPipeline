#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor;
using EazyBuildPipeline.Common.Editor;
using System.Linq;

namespace EazyBuildPipeline.BundleManager.Editor
{
    public static class G
    {
        public static Configs.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
            public Action OnChangeCurrentConfig = () => { };
            public Action OnChangeConfigList = () => { };
            public AssetBundleManagement2.AssetBundleMainWindow mainTab;
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

namespace EazyBuildPipeline.BundleManager.Editor.Configs
{
    [Serializable]
    public class Configs : EBPConfigs
    {
        public Dictionary<string, BuildAssetBundleOptions> CompressionEnumMap = new Dictionary<string, BuildAssetBundleOptions>
        {
            { "Uncompress",BuildAssetBundleOptions.UncompressedAssetBundle },
            { "LZMA",BuildAssetBundleOptions.None },
            { "LZ4" ,BuildAssetBundleOptions.ChunkBasedCompression}
        };
        public string[] CompressionEnum;

        public Configs()
        {
            CompressionEnum = CompressionEnumMap.Keys.ToArray();
            Runner = new Runner(this);
        }


        public override string ModuleName { get { return "BundleManager"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline BundleManager LocalConfig";
        public Runner Runner;
        public LocalConfig LocalConfig = new LocalConfig();
        public CurrentConfig CurrentConfig = new CurrentConfig();
        public BundleBuildMapConfig BundleBuildMapConfig = new BundleBuildMapConfig();

        public bool LoadAllConfigs(string rootPath = null)
        {
            if (!LoadCommonLocalConfig()) return false;
            if (!LoadCommonTagEnumConfig()) return false;
            if (!LoadCommonAssetsTagsConfig()) return false;

            if (!LoadLocalConfig(rootPath)) return false;
            if (!LoadCurrentConfig()) return false;
            return true;
        }

        public bool LoadCurrentConfig()
        {
            try
            {
                if (Directory.Exists(LocalConfig.Json.RootPath))
                {
                    CurrentConfig.JsonPath = LocalConfig.BundleManagerConfigPath;
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
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.JsonPath
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + LocalConfig.ToString());
                return false;
            }
        }
        public bool LoadBundleBuildMap()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentConfig.Json.CurrentBundleMap))
                {
                    BundleBuildMapConfig.JsonPath = Path.Combine(LocalConfig.Local_BundleMapsFolderPath, CurrentConfig.Json.CurrentBundleMap);
                    BundleBuildMapConfig.Load();
                    return true;
                }
                else
                {
                    CurrentConfig.Json.CurrentBundleMap = null;
                    BundleBuildMapConfig.JsonPath = null;
                    return false;
                }
            }
            catch(Exception e)
            {
                DisplayDialog("加载BundleBuildMap时发生错误：" + e.Message + "\n加载路径：" + BundleBuildMapConfig.JsonPath);
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
        public string Local_BundleMapsFolderPath { get { return Path.Combine(LocalRootPath, Json.Local_BundleMapsFolderRelativePath); } }
        public string LocalRootPath;
        //Pipeline配置路径
        public string BundleManagerConfigPath { get { return Path.Combine(Json.RootPath, Json.BundleManagerConfigRelativePath); } }
        public string BundlesFolderPath { get { return Path.Combine(Json.RootPath, Json.BundlesFolderRelativePath); } }
        [Serializable]
        public class JsonClass
        {
            public string Local_BundleMapsFolderRelativePath;
            public string RootPath;
            public string BundleManagerConfigRelativePath;
            public string BundlesFolderRelativePath;
        }
    }

    [Serializable]
    public class BundleBuildMapConfig : EBPConfig<AssetBundleBuild[]>
    {
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
            }
            public string[] CurrentTags = new string[0];
            public string CurrentBundleMap;
            public int CurrentBuildAssetBundleOptionsValue;
            public int CurrentResourceVersion;
            public int CurrentBundleVersion;
            public bool Applying;
            public bool IsPartOfPipeline;
        }
    }
}
