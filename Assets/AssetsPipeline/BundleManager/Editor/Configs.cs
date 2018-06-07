#pragma warning disable 0649
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LiXuFeng.AssetPreprocessor.Editor.Config;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;

namespace LiXuFeng.BundleManager.Editor
{
    public static class Configs
    {
        public static Config.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
            public Action OnChangeRootPath = () => { };
            public Action OnChangeTags = () => { };
            public Action<BuildTarget, int, string> Apply = (x, y, z) => { };
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

namespace LiXuFeng.BundleManager.Editor.Config
{
    public class Configs
    {
        public LocalConfig LocalConfig = new LocalConfig() { Path = "Assets/AssetsPipeline/BundleManager/Config/LocalConfig.json" };
        public BundleManagerConfig BundleManagerConfig = new BundleManagerConfig();
        public TagEnumConfig TagEnumConfig = new TagEnumConfig();

        public bool LoadAllConfigsByLocalConfig()
        {
            bool success = true;
            try
            {
                TagEnumConfig.Path = LocalConfig.TagEnumConfigPath;
                TagEnumConfig.Load();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载公共标签配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + TagEnumConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + JsonConvert.SerializeObject(TagEnumConfig.Tags), "确定");
                success = false;
            }

            try
            {
                if (Directory.Exists(LocalConfig.RootPath))
                {
                    BundleManagerConfig.Path = LocalConfig.BundleManagerConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(BundleManagerConfig.Path)))
                    {
                        if (!File.Exists(BundleManagerConfig.Path))
                        {
                            File.Create(BundleManagerConfig.Path).Close();
                            BundleManagerConfig.Save();
                        }
                        BundleManagerConfig.Load();
                    }
                    else
                    {
                        UnityEditor.EditorUtility.DisplayDialog("BundleManager", "不是有效的Pipeline根目录:" + LocalConfig.RootPath +
                       "\n\n若要新建一个此工具可用的Pipeline根目录，确保存在如下目录即可：" + Path.GetDirectoryName(BundleManagerConfig.Path), "确定");
                        success = false;
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("BundleManager", "根目录不存在:" + LocalConfig.RootPath, "确定");
                    success = false;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("BundleManager", "加载当前配置时发生错误：" + e.Message, "确定");
                success = false;
            }
            return success;
        }
        
        public bool LoadLocalConfig()
        {
            try
            {
                LocalConfig.Load();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + JsonUtility.ToJson(LocalConfig, true), "确定");
                return false;
            }
            return true;
        }
    }

    public class LocalConfig : Config
    {
        public string TagEnumConfigPath, RootPath;
        public string BundleManagerConfigPath { get { return System.IO.Path.Combine(RootPath, bundleManagerConfigPath); } }
        public string BundlesFolderPath { get { return System.IO.Path.Combine(RootPath, bundlesFolderPath); } }
        [SerializeField]
        private string bundleManagerConfigPath, bundlesFolderPath;
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

    public class BundleManagerConfig : Config
    {
        public string[] CurrentTags;
        public int CurrentBuildAssetBundleOptionsValue;
        public bool Applying;
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
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
