#pragma warning disable 0649

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor;
using EazyBuildPipeline.Common.Editor;

//namespace EazyBuildPipeline.AssetUpdate.Editor
//{
//    public static class G
//    {
//        public static Configs.Configs configs;
//        public static GlobalReference g;
//        public class GlobalReference
//        {
//        }
//        public static void Init()
//        {
//            configs = new Configs.Configs();
//            g = new GlobalReference();
//        }
//        public static void Clear()
//        {
//            configs = null;
//            g = null;
//        }
//    }
//}

namespace EazyBuildPipeline.AssetUpdate.Editor.Configs
{
    [Serializable]
    public class Configs : EBPConfigs
    {
        public override string ModuleName{ get { return "AssetsUpdate"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline AssetsUpdate LocalConfig";
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
            bool success = true;
            if (!LoadCommonLocalConfig()) return false;
            success &= LoadCommonTagEnumConfig();
            success &= LoadCommonAssetsTagsConfig();

            if (!LoadLocalConfig(rootPath)) return false;
            success &= LoadOptionsEnumConfig();
            success &= LoadCurrentConfig();
            success &= LoadCurrentSavedConfig();
            return success;
        }

        public bool LoadCurrentConfig()
        {
            try
            {
                if (Directory.Exists(LocalConfig.Json.RootPath))
                {
                    CurrentConfig.JsonPath = LocalConfig.CurrentConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(CurrentConfig.JsonPath)))
                    {
                        if (!File.Exists(CurrentConfig.JsonPath))
                        {
                            File.Create(CurrentConfig.JsonPath).Close();
                            CurrentConfig.Save();
                        }
                        CurrentConfig.Load();

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
                CurrentConfig.Json.CurrentSavedConfigName = "";
                DisplayDialog("加载当前配置时发生错误：" + e.Message);
                return false;
            }
        }

        public bool LoadOptionsEnumConfig()
        {
            try
            {
                OptionsEnumConfig.JsonPath = LocalConfig.Local_OptionsEnumConfigPath;
                OptionsEnumConfig.Load();
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载选项配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + OptionsEnumConfig.JsonPath
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + new OptionsEnumConfig().ToString());
                return false;
            }
        }

        public bool LoadCurrentSavedConfig()
        {
            try
            {
                CurrentSavedConfig.JsonPath = Path.Combine(LocalConfig.Local_SavedConfigsFolderPath, CurrentConfig.Json.CurrentSavedConfigName);
                if (CurrentConfig.Json.CurrentSavedConfigName == "")
                {
                    CurrentSavedConfig.JsonPath = "";
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
    }

    [Serializable]
    public class OptionsEnumConfig : EBPConfig<List<OptionsEnumConfig.Group>>
    {
        public OptionsEnumConfig()
        {
            Json = new List<Group>()
            {
                 new Group(){ FullGroupName = "Title1/Title2/Example Group Name", MultiSelect = true, Options = new List<string>{ "Example Option 1","Example Option 2" ,"Example Option 3"},Platform = new string[]{"android" } },
                 new Group(){ FullGroupName = "Title1/Title3/Example Group Name 2", MultiSelect = false, Options =  new List<string>{ "Example Option 1","Example Option 2" ,"Example Option 3"},Platform = new string[]{"android","ios" } }
            };
        }
        [Serializable]
        public class Group { public string FullGroupName; public List<string> Options; public bool MultiSelect; public string[] Platform; }
    }

    [Serializable]
    public class LocalConfig : EBPConfig<LocalConfig.JsonClass>
    {
        public LocalConfig()
        {
            Json = new JsonClass();
        }
        //本地配置路径
        public string Local_OptionsEnumConfigPath { get { return Path.Combine(LocalRootPath, Json.Local_OptionsEnumConfigRelativePath); } }
        public string Local_SavedConfigsFolderPath { get { return Path.Combine(LocalRootPath, Json.Local_SavedConfigsFolderRelativePath); } }
        public string Local_ShellsFolderPath { get { return Path.Combine(LocalRootPath, Json.Local_ShellsFolderRelativePath); } }
        public string LocalRootPath;
        //Pipeline配置路径
        public string CurrentConfigPath { get { return Path.Combine(Json.RootPath, Json.CurrentConfigRelativePath); } }
        public string PreStoredAssetsFolderPath { get { return Path.Combine(Json.RootPath, Json.PreStoredAssetsFolderRelativePath); } }
        public string LogsFolderPath { get { return Path.Combine(Json.RootPath, Json.LogsFolderRelativePath); } }
        [Serializable]
        public class JsonClass
        {
            public string Local_OptionsEnumConfigRelativePath;
            public string Local_SavedConfigsFolderRelativePath;
            public string Local_ShellsFolderRelativePath;
            public string RootPath;
            public string CurrentConfigRelativePath;
            public string PreStoredAssetsFolderRelativePath;
            public string LogsFolderRelativePath;
        }
    }

    [Serializable]
    public class CurrentSavedConfig : EBPConfig<CurrentSavedConfig.JsonClass>
    {
        public CurrentSavedConfig()
        {
            Json = new JsonClass();
        }
        [Serializable]
        public class Group { public string FullGroupName; public List<string> Options; }
        [Serializable]
        public class JsonClass
        {
            public List<Group> Groups = new List<Group>();
            public string[] Tags = new string[0];
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
            public string CurrentSavedConfigName;
            public bool Applying;
            public bool IsPartOfPipeline;
        }
    }
}
