#pragma warning disable 0649
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public static class Configs
    {
        public static Config.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
            public PackageTree packageTree;
            public BundleTree bundleTree;
        }

        public static void Init()
        {
            configs = new Config.Configs();
            g = new GlobalReference();
        }
        
        public static void Clear()
        {
            configs = null;
            g = null;
        }

        public static string[] NecesseryEnum = new string[] { "Immediate", "Delayed" };
        public static string[] DeploymentLocationEnum = new string[] { "Built-in", "Server" };
        public static string[] PackageModeEnum = new string[] { "Addon", "Patch" };
        public static string[] LuaSourceEnum = new string[] { "None", "Origin", "ByteCode", "Encrypted" };
    }
}
namespace EazyBuildPipeline.PackageManager.Editor.Config
{
    public class Configs
    {
        private readonly string localConfigSearchText = "LocalConfig PackageManager EazyBuildPipeline";
        public TagEnumConfig TagEnumConfig = new TagEnumConfig();
        public PackageMapConfig PackageMapConfig = new PackageMapConfig();
        public LocalConfig LocalConfig = new LocalConfig();
        public PackageConfig PackageConfig = new PackageConfig();
        public string Tag { get { return string.Join("_", PackageConfig.CurrentTags); } }
        public string BundlePath { get { return Path.Combine(LocalConfig.BundleFolderPath, Tag + "/Bundles"); } }
        public string BundleInfoPath { get { return Path.Combine(LocalConfig.BundleFolderPath, Tag + "/_Info"); } }

        //    public string TagName
        //    {
        //        get
        //        {
        //            int i;
        //            string tagName = "";
        //            for (i = 0; i < PackageConfig.CurrentTags.Length; i++)
        //            {
        //	if (!string.IsNullOrEmpty(PackageConfig.CurrentTags[i]))
        //	{
        //		tagName += PackageConfig.CurrentTags[i] + "_";
        //	}
        //            }
        //if (!string.IsNullOrEmpty(tagName))
        //{
        //	tagName = tagName.Remove(tagName.Length - 1);
        //}
        //return tagName;
        //        }
        //    }
        public int PathHandCount
        {
            get
            {
                return BundlePath.Length + 1;
            }
        }
        public bool LoadAllConfigsByLocalConfig()
        {
            bool success = true;
            try
            {
                string[] guids = AssetDatabase.FindAssets(LocalConfig.Global_TagEnumConfigName);
                if (guids.Length == 0)
                {
                    throw new ApplicationException("未能找到全局Tag枚举配置文件，搜索文本：" + LocalConfig.Global_TagEnumConfigName);
                }
                TagEnumConfig.Path = AssetDatabase.GUIDToAssetPath(guids[0]);
                TagEnumConfig.Load();
            }
            catch (Exception e)
            {
                UnityEditor.EditorUtility.DisplayDialog("错误", "加载枚举配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + TagEnumConfig.Path
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + JsonConvert.SerializeObject(TagEnumConfig.Tags, Formatting.Indented), "确定");
                success = false;
            }
            try
            {
                if (Directory.Exists(LocalConfig.RootPath))
                {
                    PackageConfig.Path = LocalConfig.PackageConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(PackageConfig.Path)))
                    {
                        if (!File.Exists(PackageConfig.Path))
                        {
                            File.Create(PackageConfig.Path).Close();
                            PackageConfig.Save();
                        }
                        PackageConfig.Load();
                    }
                    else
                    {
                        UnityEditor.EditorUtility.DisplayDialog("PackageManager", "不是有效的Pipeline根目录:" + LocalConfig.RootPath +
                           "\n\n若要新建一个此工具可用的Pipeline根目录，确保存在如下目录即可：" + Path.GetDirectoryName(PackageConfig.Path), "确定");
                        success = false;
                    }
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("PackageManager", "根目录不存在:" + LocalConfig.RootPath, "确定");
                    success = false;
                }
            }
            catch (Exception e)
            {
                UnityEditor.EditorUtility.DisplayDialog("PackageManager", "加载当前配置时发生错误：" + e.Message, "确定");
                success = false;
            }
            return success;
        }
        public void LoadLocalConfig()
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
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + JsonUtility.ToJson(LocalConfig, true), "确定");
            }
        }
    }

    public class PackageMapConfig : Config
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
            [NonSerialized]
            public string FileName; //TODO: 仅用于程序中数据传递
        }
        public List<Package> Packages = new List<Package>(); //该项不随改动而改动
        public string PackageVersion;
        public string PackageMode;
        public string LuaSource;
        public int CompressionLevel = -1;
    }

    public class LocalConfig : Config
    {
        //本地配置路径
        public string Global_TagEnumConfigName;
        public string Global_BundleIcon;
        public string Global_BundleIcon_Scene;
        public string Global_PackageIcon;
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

    public class TagEnumConfig : Config
    {
        public Dictionary<string, string[]> Tags = new Dictionary<string, string[]>
        {
            { "Example Group 1:",new string[]{"example tag1","example tag2","example tag3"} },
            { "Example Group 2:",new string[]{"example tag a","example tag b"} },
        };

        public override void Load()
        {
            string s = File.ReadAllText(Path);
            Tags = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(s);
        }
        public override void Save()
        {
            File.WriteAllText(Path, JsonConvert.SerializeObject(Tags, Formatting.Indented));
        }
    }

    public class PackageConfig : Config
    {
        public string[] CurrentTags = new string[0];
        public string CurrentPackageMap;
        public bool Applying;
    }

    public class Config
    {
        [NonSerialized]
        public string Path;

        public virtual void Load()
        {
            string s = File.ReadAllText(Path);
            JsonUtility.FromJsonOverwrite(s, this);
        }
        public virtual void Save()
        {
            File.WriteAllText(Path, JsonUtility.ToJson(this, true));
        }
    }
}