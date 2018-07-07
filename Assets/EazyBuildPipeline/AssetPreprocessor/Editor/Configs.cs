﻿#pragma warning disable 0649

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor;
using EazyBuildPipeline.Common.Editor;

namespace EazyBuildPipeline.AssetPreprocessor.Editor
{
    public static class G
    {
        public static Configs.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
            public Action OnChangeCurrentConfig;
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

namespace EazyBuildPipeline.AssetPreprocessor.Editor.Configs
{
    public class Configs
    {
        public Runner runner = new Runner();
        private readonly string localConfigSearchText = "LocalConfig AssetPreprocessor EazyBuildPipeline";
        public bool Dirty;
        public LocalConfig LocalConfig = new LocalConfig();
        public CurrentConfig CurrentConfig = new CurrentConfig();
        public OptionsEnumConfig OptionsEnumConfig = new OptionsEnumConfig();
        public CurrentSavedConfig CurrentSavedConfig = new CurrentSavedConfig();
        public TagEnumConfig TagEnumConfig = new TagEnumConfig();
        public string Tag { get { return string.Join("_", CurrentSavedConfig.Tags); } }

        public bool LoadAllConfigsByLocalConfig(string rootPath = null, CurrentConfig currentConfig = null)
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
                OptionsEnumConfig.Path = LocalConfig.Local_OptionsEnumConfigPath;
                OptionsEnumConfig.Load();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载选项配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + OptionsEnumConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + OptionsEnumConfig.ToString(), "确定");
                success = false;
            }
            
            try
            {
                if (rootPath != null) //用于总控
                {
                    LocalConfig.RootPath = rootPath;
                }

                if (Directory.Exists(LocalConfig.RootPath))
                {
                    CurrentConfig.Path = LocalConfig.PreprocessorConfigPath;

                    if (Directory.Exists(Path.GetDirectoryName(CurrentConfig.Path)))
                    {
                        if (!File.Exists(CurrentConfig.Path))
                        {
                            File.Create(CurrentConfig.Path).Close();
                            CurrentConfig.Save();
                        }
                        if (currentConfig != null) //用于总控
                        {
                            CurrentConfig = currentConfig;
                        }
                        else
                        {
                            CurrentConfig.Load(); //原代码
                        }
                        CurrentSavedConfig.Path = Path.Combine(LocalConfig.Local_SavedConfigsFolderPath, CurrentConfig.CurrentSavedConfigName);
                        if (CurrentConfig.CurrentSavedConfigName == "")
                        {
                            CurrentSavedConfig.Path = CurrentConfig.CurrentSavedConfigName = "";
                        }
                        else
                        {
                            CurrentSavedConfig.Load();
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("AssetsPreprocessor", "不是有效的Pipeline根目录:" + LocalConfig.RootPath +
                       "\n\n若要新建一个此工具可用的Pipeline根目录，确保存在如下目录即可：" + Path.GetDirectoryName(CurrentConfig.Path), "确定");
                        success = false;
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("AssetsPreprocessor", "根目录不存在:" + LocalConfig.RootPath, "确定");
                    success = false;
                }
            }
            catch (Exception e)
            {
                CurrentSavedConfig.Path = CurrentConfig.CurrentSavedConfigName = "";
                EditorUtility.DisplayDialog("AssetsPreprocessor", "加载当前配置时发生错误：" + e.Message, "确定");
                success = true;
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

    public class OptionsEnumConfig : EBPConfig
    {
        [Serializable]
        public class Group { public string FullGroupName; public List<string> Options; public bool MultiSelect; public string Platform; }
        public List<Group> Groups = new List<Group>
        { new Group(){ FullGroupName = "Title1/Title2/Example Group Name", MultiSelect = true, Options = new List<string>{ "Example Option 1","Example Option 2" ,"Example Option 3"} },
          new Group(){ FullGroupName = "Title1/Title3/Example Group Name 2", MultiSelect = false, Options =  new List<string>{ "Example Option 1","Example Option 2" ,"Example Option 3"} } };
    }

    public class LocalConfig : EBPConfig
    {
        //本地配置路径
        public string Global_TagEnumConfigName;
        public string Local_OptionsEnumConfigPath { get { return System.IO.Path.Combine(LocalRootPath, Local_OptionsEnumConfigRelativePath); } }
        public string Local_OptionsEnumConfigRelativePath;
        public string Local_SavedConfigsFolderPath { get { return System.IO.Path.Combine(LocalRootPath, Local_SavedConfigsFolderRelativePath); } }
        public string Local_SavedConfigsFolderRelativePath;
		public string Local_ShellsFolderPath { get { return System.IO.Path.Combine(LocalRootPath, Local_ShellsFolderRelativePath); } }
        public string Local_ShellsFolderRelativePath;
        [NonSerialized]
        public string LocalRootPath;
        //Pipeline配置路径
        public string RootPath;
        public string PreprocessorConfigPath { get { return System.IO.Path.Combine(RootPath, PreProcessorConfigRelativePath); } }
        public string PreProcessorConfigRelativePath;
        public string PreStoredAssetsFolderPath { get { return System.IO.Path.Combine(RootPath, PreStoredAssetsFolderRelativePath); } }
        public string PreStoredAssetsFolderRelativePath;
		public string LogsFolderPath { get { return System.IO.Path.Combine(RootPath, LogsFolderRelativePath); } }
        public string LogsFolderRelativePath;
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

    public class CurrentSavedConfig : EBPConfig
    {
        [Serializable]
        public class Group { public string FullGroupName; public List<string> Options; }
        public List<Group> Groups = new List<Group>();
        public string[] Tags = new string[0];
    }

    public class CurrentConfig : EBPConfig
    {
        public string CurrentSavedConfigName;
        public bool Applying;
        public bool IsPartOfPipeline;
    }
}
