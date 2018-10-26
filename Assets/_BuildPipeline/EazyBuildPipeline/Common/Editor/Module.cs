using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using EazyBuildPipeline.Common.Configs;

namespace EazyBuildPipeline
{
    public static class CommonModule
    {
        public static CommonConfig CommonConfig = new CommonConfig();
        public static string CommonConfigSearchText { get { return "EazyBuildPipeline CommonConfig"; } }
        public static Texture2D GetIcon(string iconFileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(CommonConfig.IconsFolderPath, iconFileName));
        }
        public static bool LoadCommonConfig()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets(CommonConfigSearchText);
                if (guids.Length == 0)
                {
                    throw new EBPException("未能找到本地公共配置文件! 搜索文本：" + CommonConfigSearchText);
                }
                CommonConfig.Load(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (!CommonConfig.IsBatchMode)
                {
                    CheckAndSetAllRootPath();
                }
                return true;
            }
            catch (Exception e)
            {
                if (CommonConfig.IsBatchMode)
                {
                    throw new EBPException("加载本地公共配置文件时发生错误：" + e.ToString());
                }
                EditorUtility.DisplayDialog("EazyBuildPipeline", "加载本地公共配置文件时发生错误：" + e.Message
                                            + "\n加载路径：" + CommonConfig.JsonPath
                                            + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + new CommonConfig(), "确定");
                return false;
            }
        }

        public static void ChangeRootPath(string path)
        {
            CommonConfig.Json.PipelineRootPath = path;
            CommonConfig.Save();
            CheckAndSetAllRootPath();
        }

        public static void CheckAndSetAllRootPath()
        {
            CheckAndResetRootPath(CommonConfig.UserConfigsRootPath);
            CheckAndResetRootPath(CommonConfig.DataRootPath);
            CheckAndResetRootPath(CommonConfig.LogsRootPath);
        }

        static bool CheckAndResetRootPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                if (EditorUtility.DisplayDialog("检查目录结构", "Pipeline根目录（" + CommonConfig.Json.PipelineRootPath +
                    "）中缺少子目录：" + path + "\n\n请重置根目录，程序将自动补全目录结构。", "选择根目录并重置", "取消"))
                {
                    string newPath = EditorUtility.OpenFolderPanel("Open Pipeline Root", CommonConfig.Json.PipelineRootPath, null);
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        CommonConfig.Json.PipelineRootPath = newPath;
                        CommonConfig.Save();
                        Directory.CreateDirectory(CommonConfig.DataRootPath);
                        Directory.CreateDirectory(CommonConfig.UserConfigsRootPath);
                        Directory.CreateDirectory(CommonConfig.LogsRootPath);
                    }
                }
                return false;
            }
            return true;
        }
    }

    [Serializable]
    public abstract class BaseModule
    {
        public bool IsDirty; //用来表示子类中自定义配置是否被修改，该变量与这个类所有内容都无关
        public bool StateConfigAvailable;
        public string StateConfigLoadFailedMessage;
        public abstract string ModuleName { get; }
        public string ModuleConfigSearchText { get { return "EazyBuildPipeline ModuleConfig " + ModuleName; } }

        public void DisplayDialog(string text)
        {
            StartLog();
            Log(text, true);
            EndLog();
            if (CommonModule.CommonConfig.IsBatchMode) //HACK: Application.isBatchMode(for Unity 2018.3+)
            {
                throw new EBPException(text);
            }
            else
            {
                EditorUtility.DisplayDialog(ModuleName, text, "确定");
            }
        }

        public abstract IModuleConfig BaseModuleConfig { get; }
        public abstract IModuleStateConfig BaseModuleStateConfig { get; }
        public abstract bool LoadModuleConfig();
        public abstract bool LoadModuleStateConfig();
        public abstract bool LoadAllConfigs(bool NOTLoadUserConfig = false);
        public abstract bool LoadUserConfig();

        //日志系统
        private double currentTime;
        private StreamWriter writer;
        public void Log(string text, bool forceFlush = false)
        {
            if (writer != null)
            {
                writer.WriteLine("[" + DateTime.Now.ToLongTimeString() + "]  " + text);
                if (EditorApplication.timeSinceStartup - currentTime > 0.5 || forceFlush) //保存日志的最少间隔时间
                {
                    writer.Flush();
                    currentTime = EditorApplication.timeSinceStartup;
                }
            }
        }
        public void StartLog()
        {
            if (writer == null)
            {
                writer = string.IsNullOrEmpty(CommonModule.CommonConfig.LogPath) ? null : new StreamWriter(CommonModule.CommonConfig.LogPath, true);
            }
        }
        public void EndLog()
        {
            if (writer != null)
            {
                writer.Close();
                writer = null;
            }
        }
    }

    /// <summary>EazyBuildPipeline模块基类</summary>
    /// <Tip>
    /// 其中各种Config类继承自EBPConfig类，这种继承本可以省略，直接产生一个 EBPConfig<JsonClass> 即可，
    /// 但考虑两点：
    /// 1.JsonClass可能为List<string>,这种情况下必须由Config子类的构造函数来初始化
    /// 2.Unity序列化不支持对泛型类的序列化，Config子类可以消除EBPConfig的泛型特性
    /// 另外：
    /// EBPConfig的Load和Save函数不使用Unity内置序列化工具是为了对字典等类型的序列化保存到文件时有更好看的字符串结果
    /// (由于Unity内置序列化工具不支持字典，所以使用Unity的JsonUtility序列化字典只能变为序列化两个List)
    /// </Tip>
    [Serializable]
    public abstract class EBPModule<TModuleConfig, TModuleConfigJsonClass, TModuleStateConfig, TModuleStateConfigJsonClass> : BaseModule
        where TModuleConfig : ModuleConfig<TModuleConfigJsonClass>, new()
        where TModuleConfigJsonClass : ModuleConfigJsonClass, new()
        where TModuleStateConfig : ModuleStateConfig<TModuleStateConfigJsonClass>, new()
        where TModuleStateConfigJsonClass : ModuleStateConfigJsonClass, new()
    {
        public TModuleConfig ModuleConfig = new TModuleConfig();
        public TModuleStateConfig ModuleStateConfig = new TModuleStateConfig();

        public override IModuleConfig BaseModuleConfig { get { return ModuleConfig; } }
        public override IModuleStateConfig BaseModuleStateConfig { get { return ModuleStateConfig; } }

        public override bool LoadModuleConfig()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets(ModuleConfigSearchText);
                if (guids.Length == 0)
                {
                    throw new EBPException("未能找到模块配置文件! 搜索文本：" + ModuleConfigSearchText);
                }
                ModuleConfig.Load(AssetDatabase.GUIDToAssetPath(guids[0]));
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载模块 " + ModuleName + " 配置文件时发生错误：" + e.Message
                            + "\n加载路径：" + ModuleConfig.JsonPath
                            + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + new TModuleConfig());
                return false;
            }
        }

        public override bool LoadModuleStateConfig()
        {
            ModuleStateConfig.UserConfigsFolderPath = ModuleConfig.UserConfigsFolderPath; //拷贝配置项
            try
            {
                ModuleStateConfig.JsonPath = ModuleConfig.StateConfigPath;
                Directory.CreateDirectory(Path.GetDirectoryName(ModuleStateConfig.JsonPath)); //Create _Configs目录
                if (!File.Exists(ModuleStateConfig.JsonPath)) //状态配置文件是否存在
                {
                    ModuleStateConfig.Save();
                }
                else
                {
                    ModuleStateConfig.Load();
                }
                //if (G.OverrideCurrentSavedConfigName != null) //用于总控
                //{
                //    CurrentConfig.Json.CurrentSavedConfigName = G.OverrideCurrentSavedConfigName;
                //    G.OverrideCurrentSavedConfigName = null;
                //}
                StateConfigAvailable = true;
                return true;
            }
            catch (Exception e)
            {
                StateConfigLoadFailedMessage = "加载模块 " + ModuleName + " 状态配置文件时发生错误：" + e.Message
                            + "\n加载路径：" + ModuleStateConfig.JsonPath
                            + "\n请设置正确的文件路径以及形如以下所示的配置文件：\n" + new TModuleStateConfig();
                DisplayDialog(StateConfigLoadFailedMessage);
                StateConfigAvailable = false;
                return false;
            }
        }
    }
}