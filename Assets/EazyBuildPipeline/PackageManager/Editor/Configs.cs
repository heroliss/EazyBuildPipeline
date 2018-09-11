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
    [Serializable]
    public class Styles
    {
        //为解决bundleTree和packageTree中不能直接调用EditorStyles的问题
        public GUIStyle LabelStyle;
        public GUIStyle InDropDownStyle;
        public GUIStyle InToggleStyle;
        public GUIStyle ButtonStyle;
        public void InitStyles() //这个函数只能在Awake和OnGUI里调用
        {
            LabelStyle = new GUIStyle("Label");
            InDropDownStyle = new GUIStyle("IN DropDown");
            InToggleStyle = new GUIStyle(EditorGUIUtility.isProSkin ? "OL ToggleWhite" : "OL Toggle");//这样做是因为OL Toggle样式在专业版皮肤下有Bug，因此用OL ToggleWhite代替
            ButtonStyle = new GUIStyle("Button");
        }
    }

    public static class G
    {
        public static string OverrideCurrentSavedConfigName = null;

        public static Configs.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
            public Action OnChangeCurrentConfig = () => { };
            public Action OnChangeConfigList = () => { };
            public PackageTree packageTree;
            public BundleTree bundleTree;
            public Styles styles;
        }

        public static void Init()
        {
            configs = new Configs.Configs();
            g = new GlobalReference();
            g.styles = new Styles();
            g.styles.InitStyles();
        }

        public static void Clear()
        {
            configs = null;
            g = null;
            OverrideCurrentSavedConfigName = null;
        }

        public static string[] NecesseryEnum = { "Immediate", "Delayed" };
        public static string[] DeploymentLocationEnum = { "Built-in", "Server" };
        public static string[] PackageModeEnum = { "Addon", "Patch" };
        public static string[] LuaSourceEnum = { "None", "Origin", "ByteCode", "Encrypted" };
    }
}
namespace EazyBuildPipeline.PackageManager.Editor.Configs
{
    [Serializable]
    public class Configs : EBPConfigs
    {
        public bool Dirty;
        public override string ModuleName { get { return "PackageManager"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline PackageManager LocalConfig";
        public Runner Runner;
        public PackageMapConfig PackageMapConfig = new PackageMapConfig();
        public LocalConfig LocalConfig = new LocalConfig();
        public CurrentConfig CurrentConfig = new CurrentConfig();


        public string GetBundleFolderPath() { return Path.Combine(LocalConfig.BundleFolderPath, EBPUtility.GetTagStr(CurrentConfig.Json.CurrentTags) + "/Bundles"); }
        public string GetBundleInfoFolderPath() { return Path.Combine(LocalConfig.BundleFolderPath, EBPUtility.GetTagStr(CurrentConfig.Json.CurrentTags) + "/_Info"); }
        public int GetBundleFolderPathStrCount() { return GetBundleFolderPath().Length + 1; }

        public Configs()
        {
            Runner = new Runner(this);
        }


        public bool LoadAllConfigs(string rootPath = null)
        {
            bool success = true;
            if (!LoadCommonLocalConfig()) return false;
            success &= LoadCommonTagEnumConfig();
            success &= LoadCommonAssetsTagsConfig();

            if (!LoadLocalConfig(rootPath)) return false;
            success &= LoadCurrentConfig();
            return success;
        }

        public bool LoadCurrentConfig()
        {
            try
            {
                if (Directory.Exists(LocalConfig.Json.RootPath))
                {
                    CurrentConfig.JsonPath = LocalConfig.PackageConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(CurrentConfig.JsonPath)))
                    {
                        if (!File.Exists(CurrentConfig.JsonPath))
                        {
                            File.Create(CurrentConfig.JsonPath).Close();
                            CurrentConfig.Save();
                        }
                        CurrentConfig.Load();
                        if (G.OverrideCurrentSavedConfigName != null) //用于总控
                        {
                            CurrentConfig.Json.CurrentPackageMap = G.OverrideCurrentSavedConfigName;
                            G.OverrideCurrentSavedConfigName = null;
                        }
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
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + new LocalConfig().ToString());
                return false;
            }
        }

        public bool LoadPackageMap()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentConfig.Json.CurrentPackageMap))
                {
                    string mapsFolderPath = LocalConfig.Local_PackageMapsFolderPath;
                    string currentMapPath = Path.Combine(mapsFolderPath, CurrentConfig.Json.CurrentPackageMap);
                    PackageMapConfig.JsonPath = currentMapPath;
                    PackageMapConfig.Load();
                    return true;
                }
                else
                {
                    CurrentConfig.Json.CurrentPackageMap = null;
                    PackageMapConfig.JsonPath = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "载入映射文件：" + CurrentConfig.Json.CurrentPackageMap + " 时发生错误：" + e.Message, "确定");
                CurrentConfig.Json.CurrentPackageMap = null;
                PackageMapConfig.JsonPath = null;
                return false;
            }
        }
    }

    [Serializable]
    public class PackageMapConfig : EBPConfig<PackageMapConfig.JsonClass>
    {
        public PackageMapConfig()
        {
            Json = new JsonClass();
        }
        [Serializable]
        public class JsonClass
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
            }
            public List<Package> Packages = new List<Package>(); //该项不随改动而改动
            public string PackageMode;
            public string LuaSource;
            public int CompressionLevel = -1;
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
        public string Local_PackageMapsFolderPath { get { return Path.Combine(LocalRootPath, Json.Local_PackageMapsFolderRelativePath); } }
        public string LocalRootPath;
        //Pipeline配置路径
        public string PackageConfigPath { get { return Path.Combine(Json.RootPath, Json.PackageConfigRelativePath); } }
        public string BundleFolderPath { get { return Path.Combine(Json.RootPath, Json.BundleFolderRelativePath); } }
        public string PackageFolderPath { get { return Path.Combine(Json.RootPath, Json.PackageFolderRelativePath); } }

        [Serializable]
        public class JsonClass
        {
            public string Local_PackageMapsFolderRelativePath;
            public string RootPath;
            public string PackageConfigRelativePath;
            public string BundleFolderRelativePath;
            public string PackageFolderRelativePath;
            //其他配置
            public string PackageExtension;        
            public bool CheckBundle;
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
            public string[] CurrentTags = new string[0];
            public string CurrentPackageMap;
            public string CurrentAddonVersion;
            public bool Applying;
            public bool IsPartOfPipeline;
        }
    }
}