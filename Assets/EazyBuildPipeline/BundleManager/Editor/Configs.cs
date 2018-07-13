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
                if (Directory.Exists(LocalConfig.RootPath))
                {
                    CurrentConfig.Path = LocalConfig.BundleManagerConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(CurrentConfig.Path)))
                    {
                        if (!File.Exists(CurrentConfig.Path))
                        {
                            File.Create(CurrentConfig.Path).Close();
                            CurrentConfig.Save();
                        }
                        CurrentConfig.Load();
                        return true;
                    }
                    else
                    {
                        DisplayDialog("不是有效的Pipeline根目录:" + LocalConfig.RootPath +
                       "\n\n若要新建一个此工具可用的Pipeline根目录，确保存在如下目录即可：" + Path.GetDirectoryName(CurrentConfig.Path));
                        return false;
                    }
                }
                else
                {
                    DisplayDialog("根目录不存在:" + LocalConfig.RootPath);
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
                LocalConfig.Path = AssetDatabase.GUIDToAssetPath(guids[0]);
                LocalConfig.LocalRootPath = Path.GetDirectoryName(LocalConfig.Path);
                LocalConfig.Load();
                if (rootPath != null)
                {
                    LocalConfig.RootPath = rootPath;
                }
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.Path
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + LocalConfig.ToString());
                return false;
            }
        }
        public bool LoadBundleBuildMap()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentConfig.CurrentBundleMap))
                {
                    BundleBuildMapConfig.Path = Path.Combine(LocalConfig.Local_BundleMapsFolderPath, CurrentConfig.CurrentBundleMap);
                    BundleBuildMapConfig.Load();
                    return true;
                }
                else
                {
                    CurrentConfig.CurrentBundleMap = null;
                    BundleBuildMapConfig.Path = null;
                    return false;
                }
            }
            catch(Exception e)
            {
                DisplayDialog("加载BundleBuildMap时发生错误：" + e.Message + "\n加载路径：" + BundleBuildMapConfig.Path);
                return false;
            }
        }

    }


    public class LocalConfig : EBPConfig
    {
        //本地配置路径
        public string Local_BundleMapsFolderPath { get { return System.IO.Path.Combine(LocalRootPath, Local_BundleMapsFolderRelativePath); } }
        public string Local_BundleMapsFolderRelativePath;
        [NonSerialized]
        public string LocalRootPath;
        //Pipeline配置路径
        public string RootPath;
        public string BundleManagerConfigPath { get { return System.IO.Path.Combine(RootPath, BundleManagerConfigRelativePath); } }
        public string BundleManagerConfigRelativePath;
        public string BundlesFolderPath { get { return System.IO.Path.Combine(RootPath, BundlesFolderRelativePath); } }
        public string BundlesFolderRelativePath;
    }

    public class BundleBuildMapConfig : EBPConfig
    {
        public AssetBundleBuild[] BundleBuildMap;
        public override void Load()
        {
            string content = File.ReadAllText(Path);
            BundleBuildMap = JsonConvert.DeserializeObject<AssetBundleBuild[]>(content);
        }
        public override void Save()
        {
            string content = JsonConvert.SerializeObject(BundleBuildMap);
            File.WriteAllText(Path, content);
        }
    }

    public class CurrentConfig : EBPConfig
    {
        public string[] CurrentTags;
        public string CurrentBundleMap;
        public int CurrentBuildAssetBundleOptionsValue;
        public int CurrentResourceVersion;
        public int CurrentBundleVersion;
        public bool Applying;
        public bool IsPartOfPipeline;

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
    }
}
