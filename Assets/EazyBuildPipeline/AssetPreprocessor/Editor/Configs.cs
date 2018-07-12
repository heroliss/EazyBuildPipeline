#pragma warning disable 0649

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
        public static string OverrideCurrentSavedConfigName = null;

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
            OverrideCurrentSavedConfigName = null;
        }
    }
}

namespace EazyBuildPipeline.AssetPreprocessor.Editor.Configs
{
    public class Configs : EBPConfigs
    {
        public override string ModuleName{ get { return "AssetPreprocessor"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline AssetPreprocessor LocalConfig";
        public bool Dirty;
        public Runner Runner;
        public LocalConfig LocalConfig = new LocalConfig();
        public CurrentConfig CurrentConfig = new CurrentConfig();
        public OptionsEnumConfig OptionsEnumConfig = new OptionsEnumConfig();
        public CurrentSavedConfig CurrentSavedConfig = new CurrentSavedConfig();

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
            if (!LoadOptionsEnumConfig()) return false;

            if (!LoadCurrentConfig()) return false;
            if (!LoadCurrentSavedConfig()) return false;
            return true;
        }

        public bool LoadCurrentConfig()
        {
            try
            {
                if (Directory.Exists(LocalConfig.RootPath))
                {
                    CurrentConfig.Path = LocalConfig.CurrentConfigPath;
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
                            CurrentConfig.CurrentSavedConfigName = G.OverrideCurrentSavedConfigName;
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
                CurrentConfig.CurrentSavedConfigName = "";
                DisplayDialog("加载当前配置时发生错误：" + e.Message);
                return false;
            }
        }

        public bool LoadOptionsEnumConfig()
        {
            try
            {
                OptionsEnumConfig.Path = LocalConfig.Local_OptionsEnumConfigPath;
                OptionsEnumConfig.Load();
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载选项配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + OptionsEnumConfig.Path
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + OptionsEnumConfig.ToString());
                return false;
            }
        }

        public bool LoadCurrentSavedConfig()
        {
            try
            {
                CurrentSavedConfig.Path = Path.Combine(LocalConfig.Local_SavedConfigsFolderPath, CurrentConfig.CurrentSavedConfigName);
                if (CurrentConfig.CurrentSavedConfigName == "")
                {
                    CurrentSavedConfig.Path = "";
                }
                else
                {
                    CurrentSavedConfig.Load();
                }
                return true;
            }
            catch(Exception e)
            {
                DisplayDialog("加载保存的配置文件时发生错误：" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// 加载本地配置
        /// </summary>
        /// <param name="rootPath">设置Pipeline根路径，若为空则不设置（保留从json中加载的内容）</param>
        /// <returns></returns>
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
        public string CurrentConfigPath { get { return System.IO.Path.Combine(RootPath, CurrentConfigRelativePath); } }
        public string CurrentConfigRelativePath;
        public string PreStoredAssetsFolderPath { get { return System.IO.Path.Combine(RootPath, PreStoredAssetsFolderRelativePath); } }
        public string PreStoredAssetsFolderRelativePath;
        public string LogsFolderPath { get { return System.IO.Path.Combine(RootPath, LogsFolderRelativePath); } }
        public string LogsFolderRelativePath;
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
