#pragma warning disable 0649
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LiXuFeng.PackageManager.Editor
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
    }
}
namespace LiXuFeng.PackageManager.Editor.Config
{
    public class Configs
    {
        public TagEnumConfig TagEnumConfig = new TagEnumConfig();
        public PackageMapConfig PackageMapConfig = new PackageMapConfig();
        public LocalConfig LocalConfig = new LocalConfig() { Path = "Assets/AssetsPipeline/PackageManager/Config/LocalConfig.json" };
        public PackageConfig PackageConfig = new PackageConfig();
        public string Tag { get { return string.Join("_", PackageConfig.CurrentTags); } }

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
                return LocalConfig.BundlePath.Length + Tag.Length + 2;
            }
        }
        public bool LoadAllConfigsByLocalConfig()
        {
            bool success = true;
            try
            {
                TagEnumConfig.Path = LocalConfig.EnumConfigPath;
                TagEnumConfig.Load();
            }
            catch (Exception e)
            {
                UnityEditor.EditorUtility.DisplayDialog("错误", "加载枚举配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + TagEnumConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + JsonConvert.SerializeObject(TagEnumConfig.Tags), "确定");
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
                LocalConfig.Load();
            }
            catch (Exception e)
            {
                UnityEditor.EditorUtility.DisplayDialog("错误", "加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + JsonUtility.ToJson(LocalConfig, true), "确定");
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
        }
        public List<Package> Packages = new List<Package>();
        public string PackageVersion;
        public string PackageMode;
        public string LuaSource;
        public int CompressionLevel = -1;
    }

    public class LocalConfig : Config
    {
        public string PackageExtension;
        public bool CheckBundle;
        public string EnumConfigPath, PackageMapsFolderPath, RootPath;
        public string PackageConfigPath { get { return System.IO.Path.Combine(RootPath, packageConfigPath); } }
        public string BundlePath { get { return System.IO.Path.Combine(RootPath, bundlePath); } }
        public string PackagePath { get { return System.IO.Path.Combine(RootPath, packagePath); } }
        [SerializeField]
        private string packageConfigPath, bundlePath, packagePath;
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
            File.WriteAllText(Path, JsonConvert.SerializeObject(Tags));
        }
    }

    public class PackageConfig : Config
    {
        public string[] CurrentTags;
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
