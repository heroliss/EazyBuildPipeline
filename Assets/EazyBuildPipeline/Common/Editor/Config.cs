using UnityEditor;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace EazyBuildPipeline.Common.Editor
{
    public class EBPConfigs
    {
        public virtual string ModuleName { get { return "EazyBuildPipeline"; } }
        private readonly string commonConfigSearchText = "EazyBuildPipeline CommonConfig";
        public CommonLocalConfig Common_LocalConfig = new CommonLocalConfig();
        public CommonTagEnumConfig Common_TagEnumConfig = new CommonTagEnumConfig();
        public CommonAssetsTagsConfig Common_AssetsTagsConfig = new CommonAssetsTagsConfig();

        public bool LoadCommonAssetsTagsConfig()
        {
            try
            {
                Common_AssetsTagsConfig.Path = Common_LocalConfig.AssetsTagsConfigPath;
                Common_AssetsTagsConfig.Load();
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载公共AssetsTags文件时发生错误：" + e.Message
                    + "\n加载路径：" + Common_AssetsTagsConfig.Path
                    + "\n请正确设置形如以下所示的配置文件：\n" + Common_AssetsTagsConfig.ToString());
                return false;
            }
        }

        public bool LoadCommonTagEnumConfig()
        {
            try
            {
                Common_TagEnumConfig.Path = Common_LocalConfig.TagEnumConfigPath;
                Common_TagEnumConfig.Load();
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载公共枚举配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + Common_TagEnumConfig.Path
                    + "\n请正确设置形如以下所示的配置文件：\n" + Common_TagEnumConfig.ToString());
                return false;
            }
        }

        public bool LoadCommonLocalConfig()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets(commonConfigSearchText);
                if (guids.Length == 0)
                {
                    throw new ApplicationException("未能找到公共本地配置文件! 搜索文本：" + commonConfigSearchText);
                }
                Common_LocalConfig.Path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Common_LocalConfig.LocalRootPath = Path.GetDirectoryName(Common_LocalConfig.Path);
                Common_LocalConfig.Load();
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载公共本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + Common_LocalConfig.Path
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + Common_LocalConfig.ToString());
                return false;
            }
        }

        public class CommonLocalConfig : EBPConfig
        {
            public string TagEnumConfigPath { get { return System.IO.Path.Combine(LocalRootPath, TagEnumConfigRelativePath); } }
            public string TagEnumConfigRelativePath;
            public string AssetsTagsConfigPath { get { return System.IO.Path.Combine(LocalRootPath, AssetsTagsConfigRelativePath); } }
            public string AssetsTagsConfigRelativePath;
            public string BundleIconPath { get { return System.IO.Path.Combine(LocalRootPath, BundleIconRelativePath); } }
            public string BundleIconRelativePath;
            public string BundleIcon_SceneConfigPath { get { return System.IO.Path.Combine(LocalRootPath, BundleIcon_SceneRelativePath); } }
            public string BundleIcon_SceneRelativePath;
            public string PackageIconPath { get { return System.IO.Path.Combine(LocalRootPath, PackageIconRelativePath); } }
            public string PackageIconRelativePath;
            public string SettingIconPath { get { return System.IO.Path.Combine(LocalRootPath, SettingIconRelativePath); } }
            public string SettingIconRelativePath;
            [NonSerialized]
            public string LocalRootPath;
        }

        public class CommonTagEnumConfig : EBPConfig
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
            public override string ToString()
            {
                return JsonConvert.SerializeObject(Tags, Formatting.Indented);
            }
        }

        public class CommonAssetsTagsConfig : EBPConfig
        {
            public string[] CurrentTags = new string[] { "Example Tag1", "Example Tag2" };

            public override void Load()
            {
                string s = File.ReadAllText(Path);
                CurrentTags = JsonConvert.DeserializeObject<string[]>(s);
            }
            public override void Save()
            {
                File.WriteAllText(Path, JsonConvert.SerializeObject(CurrentTags, Formatting.Indented));
            }
            public override string ToString()
            {
                return JsonConvert.SerializeObject(CurrentTags, Formatting.Indented);
            }
        }

        public void DisplayDialog(string text)
        {
            EditorUtility.DisplayDialog(ModuleName, text, "确定");
        }
    }

    public class EBPConfig
    {
        [NonSerialized]
        public string Path;

        public virtual void Load()
        {
            string s = File.ReadAllText(Path);
            EditorJsonUtility.FromJsonOverwrite(s, this);
        }
        public virtual void Save()
        {
            File.WriteAllText(Path, EditorJsonUtility.ToJson(this, true));
        }
        public override string ToString()
        {
            return EditorJsonUtility.ToJson(this, true);
        }
    }
}