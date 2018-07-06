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
            public Action OnChangeRootPath = () => { };
            public Action OnChangeTags = () => { };
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
    public class Configs
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
        }


        public Runner runner = new Runner();
        private readonly string localConfigSearchText = "LocalConfig BundleManager EazyBuildPipeline";
        public LocalConfig LocalConfig = new LocalConfig();
        public BundleManagerConfig BundleManagerConfig = new BundleManagerConfig();
        public TagEnumConfig TagEnumConfig = new TagEnumConfig();
        public string Tag { get { return string.Join("_", BundleManagerConfig.CurrentTags); } }

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
                EditorUtility.DisplayDialog("错误", "加载公共标签配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + TagEnumConfig.Path
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + JsonConvert.SerializeObject(TagEnumConfig.Tags, Formatting.Indented), "确定");
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
                EditorUtility.DisplayDialog("错误", "加载本地配置文件时发生错误：" + e.Message
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
        public string Global_TagEnumConfigName;
        public string Local_BundleMapsFolderPath { get { return "AssetBundles/BuildMaps"; } }//TODO：BundleMaster的特殊处理
        //public string Local_BundleMapsFolderRelativePath;
        [NonSerialized]
        public string LocalRootPath;
        //Pipeline配置路径
        public string RootPath;
        public string BundleManagerConfigPath { get { return System.IO.Path.Combine(RootPath, BundleManagerConfigRelativePath); } }
        public string BundleManagerConfigRelativePath;
        public string BundlesFolderPath { get { return System.IO.Path.Combine(RootPath, BundlesFolderRelativePath); } }
        public string BundlesFolderRelativePath;
    }

    public class TagEnumConfig : EBPConfig
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

    public class BundleManagerConfig : EBPConfig
    {
        public string[] CurrentTags;
        public string CurrentBundleMap;
        public int CurrentBuildAssetBundleOptionsValue;
        public int CurrentResourceVersion;
        public int CurrentBundleVersion;
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
}
