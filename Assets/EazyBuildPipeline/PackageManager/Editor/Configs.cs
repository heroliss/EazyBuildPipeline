#pragma warning disable 0649
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using EazyBuildPipeline.Common.Editor;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public static class G
    {
        public static string OverrideCurrentSavedConfigName = null;

        public static Configs.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
            public PackageTree packageTree;
            public BundleTree bundleTree;
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
            OverrideCurrentSavedConfigName = null;
        }

        public static string[] NecesseryEnum = new string[] { "Immediate", "Delayed" };
        public static string[] DeploymentLocationEnum = new string[] { "Built-in", "Server" };
        public static string[] PackageModeEnum = new string[] { "Addon", "Patch" };
        public static string[] LuaSourceEnum = new string[] { "None", "Origin", "ByteCode", "Encrypted" };
    }
}
namespace EazyBuildPipeline.PackageManager.Editor.Configs
{
    public class Configs : EBPConfigs
    {
        public override string ModuleName { get { return "PackageManager"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline PackageManager LocalConfig";
        public Runner Runner;
        public PackageMapConfig PackageMapConfig = new PackageMapConfig();
        public LocalConfig LocalConfig = new LocalConfig();
        public CurrentConfig CurrentConfig = new CurrentConfig();
        public string GetBundleFolderPath() { return Path.Combine(LocalConfig.BundleFolderPath, EBPUtility.GetTagStr(CurrentConfig.CurrentTags) + "/Bundles"); }
        public string GetBundleInfoFolderPath() { return Path.Combine(LocalConfig.BundleFolderPath, EBPUtility.GetTagStr(CurrentConfig.CurrentTags) + "/_Info"); }
        public int GetBundleFolderPathStrCount() { return GetBundleFolderPath().Length + 1; }

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
            if (!LoadCurrentConfig()) return false;
            return true;
        }

        public bool LoadCurrentConfig()
        {
            try
            {
                if (Directory.Exists(LocalConfig.RootPath))
                {
                    CurrentConfig.Path = LocalConfig.PackageConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(CurrentConfig.Path)))
                    {
                        if (!File.Exists(CurrentConfig.Path))
                        {
                            File.Create(CurrentConfig.Path).Close();
                            CurrentConfig.Save();
                        }
                        CurrentConfig.Load();
                        if (G.OverrideCurrentSavedConfigName != null) //用于总控
                        {
                            CurrentConfig.CurrentPackageMap = G.OverrideCurrentSavedConfigName;
                            G.OverrideCurrentSavedConfigName = null;
                        }
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

        public void LoadMap()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentConfig.CurrentPackageMap))
                {
                    string mapsFolderPath = LocalConfig.Local_PackageMapsFolderPath;
                    string currentMapPath = Path.Combine(mapsFolderPath, CurrentConfig.CurrentPackageMap);
                    PackageMapConfig.Path = currentMapPath;
                    PackageMapConfig.Load();
                }
                else
                {
                    CurrentConfig.CurrentPackageMap = null;
                    PackageMapConfig.Path = null;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "载入映射文件：" + CurrentConfig.CurrentPackageMap + " 时发生错误：" + e.Message, "确定");
                CurrentConfig.CurrentPackageMap = null;
                PackageMapConfig.Path = null;
            }
        }
    }

    public class PackageMapConfig : EBPConfig
    {
        [Serializable]
        public struct Package
        {
            public string PackageName;
            public string Color;
            public List<string> Bundles;
            public List<string> EmptyFolders;
            public string Necessery;
            public string DeploymentLocation;
            public bool CopyToStreaming;
            public string FileName;
        }
        public List<Package> Packages = new List<Package>(); //该项不随改动而改动
        public string PackageMode;
        public string LuaSource;
        public int CompressionLevel = -1;
    }

    public class LocalConfig : EBPConfig
    {
        //本地配置路径
        public string Local_PackageMapsFolderPath { get { return System.IO.Path.Combine(LocalRootPath, Local_PackageMapsFolderRelativePath); } }
        public string Local_PackageMapsFolderRelativePath;
        [NonSerialized]
        public string LocalRootPath;
        //Pipeline配置路径
        public string RootPath;
        public string PackageConfigPath { get { return System.IO.Path.Combine(RootPath, PackageConfigRelativePath); } }
        public string PackageConfigRelativePath;
        public string BundleFolderPath { get { return System.IO.Path.Combine(RootPath, BundleFolderRelativePath); } }
        public string BundleFolderRelativePath;
        public string PackageFolderPath { get { return System.IO.Path.Combine(RootPath, PackageFolderRelativePath); } }
        public string PackageFolderRelativePath;
        //其他配置
        public string PackageExtension;
        public bool CheckBundle;
    }

    public class CurrentConfig : EBPConfig
    {
        public string[] CurrentTags = new string[0];
        public string CurrentPackageMap;
        public string CurrentAddonVersion;
        public bool Applying;
        public bool IsPartOfPipeline;
    }
}