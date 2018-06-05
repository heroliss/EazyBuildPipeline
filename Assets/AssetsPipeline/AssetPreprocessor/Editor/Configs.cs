#pragma warning disable 0649
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LiXuFeng.AssetPreprocessor.Editor.Config;
using System.Linq;
using Newtonsoft.Json;

namespace LiXuFeng.AssetPreprocessor.Editor
{
    public static class Configs
    {
        public static Config.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
            public Action OnChangeCurrentConfig;
        }
        public static void Clear()
        {
            configs = null;
            g = null;
        }
    }
}

namespace LiXuFeng.AssetPreprocessor.Editor.Config
{
    public class Configs
    {
        public bool Dirty;
        public LocalConfig LocalConfig = new LocalConfig() { Path = "Assets/AssetsPipeline/AssetPreprocessor/Config/LocalConfig.json" };
        public PreprocessorConfig PreprocessorConfig = new PreprocessorConfig();
        public OptionsEnumConfig OptionsEnumConfig = new OptionsEnumConfig();
        public CurrentSavedConfig CurrentSavedConfig = new CurrentSavedConfig();
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
                UnityEditor.EditorUtility.DisplayDialog("错误", "加载公共标签配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + TagEnumConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + JsonConvert.SerializeObject(TagEnumConfig.Tags), "确定");
                success = false;
            }

            try
            {
                OptionsEnumConfig.Path = LocalConfig.OptionsEnumConfigPath;
                OptionsEnumConfig.Load();
            }
            catch (Exception e)
            {
                UnityEditor.EditorUtility.DisplayDialog("错误", "加载选项配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + OptionsEnumConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + JsonUtility.ToJson(OptionsEnumConfig, true), "确定");
                success = false;
            }
            
            try
            {
                if (Directory.Exists(LocalConfig.RootPath))
                {
                    PreprocessorConfig.Path = LocalConfig.PreprocessorConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(PreprocessorConfig.Path)))
                    {
                        if (!File.Exists(PreprocessorConfig.Path))
                        {
                            File.Create(PreprocessorConfig.Path).Close();
                            PreprocessorConfig.Save();
                        }
                        PreprocessorConfig.Load();
                        CurrentSavedConfig.Path = Path.Combine(LocalConfig.SavedConfigsFolderPath, PreprocessorConfig.CurrentSavedConfigName);
                        if (PreprocessorConfig.CurrentSavedConfigName == "")
                        {
                            CurrentSavedConfig.Path = PreprocessorConfig.CurrentSavedConfigName = "";
                        }
                        else
                        {
                            CurrentSavedConfig.Load();
                        }
                    }
                    else
                    {
                        UnityEditor.EditorUtility.DisplayDialog("AssetsPreprocessor", "不是有效的Pipeline根目录:" + LocalConfig.RootPath +
                       "\n\n若要新建一个此工具可用的Pipeline根目录，确保存在如下目录即可：" + Path.GetDirectoryName(PreprocessorConfig.Path), "确定");
                        success = false;
                    }
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("AssetsPreprocessor", "根目录不存在:" + LocalConfig.RootPath, "确定");
                    success = false;
                }
            }
            catch (Exception e)
            {
                CurrentSavedConfig.Path = PreprocessorConfig.CurrentSavedConfigName = "";
                UnityEditor.EditorUtility.DisplayDialog("AssetsPreprocessor", "加载当前配置时发生错误：" + e.Message, "确定");
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
                UnityEditor.EditorUtility.DisplayDialog("错误", "加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + JsonUtility.ToJson(LocalConfig, true), "确定");
                return false;
            }
            return true;
        }
    }

    public class OptionsEnumConfig : Config
    {
        [Serializable]
        public class Group { public string FullGroupName; public List<string> Options; public bool MultiSelect; public string Platform; }
        public List<Group> Groups = new List<Group>
        { new Group(){ FullGroupName = "Title1/Title2/Example Group Name", MultiSelect = true, Options = new List<string>{ "Example Option 1","Example Option 2" ,"Example Option 3"} },
          new Group(){ FullGroupName = "Title1/Title3/Example Group Name 2", MultiSelect = false, Options =  new List<string>{ "Example Option 1","Example Option 2" ,"Example Option 3"} } };
    }

    public class LocalConfig : Config
    {
        public string OptionsEnumConfigPath, TagEnumConfigPath, SavedConfigsFolderPath, RootPath;
        public string PreprocessorConfigPath { get { return System.IO.Path.Combine(RootPath, preProcessorConfigPath); } }
        public string PreStoredAssetsFolderPath { get { return System.IO.Path.Combine(RootPath, preStoredAssetsFolderPath); } }
		public string LogsFolderPath { get { return System.IO.Path.Combine(RootPath, logsFolderPath); } }
        [SerializeField]
        private string preProcessorConfigPath, preStoredAssetsFolderPath, logsFolderPath;
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

    public class CurrentSavedConfig : Config
    {
        [Serializable]
        public class Group { public string FullGroupName; public List<string> Options; }
        public List<Group> Groups = new List<Group>();
        public List<string> Tags =new List<string>();
    }

    public class PreprocessorConfig : Config
    {
        public string CurrentSavedConfigName;
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
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
