using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using EazyBuildPipeline.Common.Configs;

namespace EazyBuildPipeline
{
    public static class CommonModule
    {
        public static readonly string MarkDirName = ".eazyBuildPipeline";
        public static CommonConfig CommonConfig = new CommonConfig();
        public static string CommonConfigSearchText { get { return "EazyBuildPipeline CommonConfig"; } }
        public static Texture2D GetIcon(string iconFileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(CommonConfig.IconsFolderPath, iconFileName));
        }

        /// <summary>
        /// //该函数内已经确保了日志目录路径存在（但不确保主日志路径存在）
        /// </summary>
        /// <returns></returns>
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

                if (CommonConfig.IsBatchMode)
                {
                    GenerateLogFolderPath(); //确保没有LogPath参数时也存在日志目录路径
                    CommonConfig.CurrentLogFolderPath = EBPUtility.GetArgValue("LogPath"); //只有batchmode才开启自定义日志路径
                }
                else //只有非batchmode才开启根目录检查和自动创建
                {
                    string rootPath = null;
                    if (!string.IsNullOrEmpty(CommonConfig.Json.PipelineRootPath))
                    {
                        rootPath = EBPUtility.OpenPipelineRoot(CommonConfig.Json.PipelineRootPath);
                    }
                    if (rootPath == null)
                    {
                        rootPath = EBPUtility.OpenPipelineRoot();
                    }
                    if (rootPath == null)
                    {
                        return false;
                    }
                    ChangeRootPath(rootPath);
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

        public static void ChangeRootPath(string rootPath)
        {
            CommonConfig.Json.PipelineRootPath = rootPath;
            CommonConfig.Save();
        }

        public static void GenerateLogFolderPath()
        {
            CommonConfig.CurrentLogFolderPath = Path.Combine(CommonConfig.LogsRootPath, DateTime.Now.ToString("[yyyyMMddHHmmss]"));
        }
        public static void ClearLogFolderPath()
        {
            CommonConfig.CurrentLogFolderPath = null;
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

        public abstract IModuleConfig BaseModuleConfig { get; }
        public abstract IModuleStateConfig BaseModuleStateConfig { get; }
        public abstract bool LoadModuleConfig();
        public abstract bool LoadModuleStateConfig();
        public abstract bool LoadAllConfigs(bool NOTLoadUserConfig = false);
        public abstract bool LoadUserConfig();

        #region 对话框和进度条
        readonly double progressInterval = 0.04f;
        double lastTimeSinceStartup;
        public void DisplayDialog(string message)
        {
            if (!CommonModule.CommonConfig.IsBatchMode)
            {
                EditorUtility.DisplayDialog(ModuleName, message, "确定");
            }
        }

        public bool DisplayDialog(string message, string ok, string cancel)
        {
            if (CommonModule.CommonConfig.IsBatchMode)
            {
                throw new EBPException("BatchMode时不应显示该对话框!");
            }
            return EditorUtility.DisplayDialog(ModuleName, message, ok, cancel);
        }

        public void DisplayProgressBar(string title, float progress, bool log = false, bool interval = false)
        {
            if (log)
            {
                Log(title);
            }
            if (CanDisplayProgressBar(interval))
            {
                EditorUtility.DisplayProgressBar(title, null, progress);
            }
        }

        public void DisplayProgressBar(string title, string info, float progress, bool log = false, bool interval = false)
        {
            if (log)
            {
                Log(title + " : " + info);
            }
            if (CanDisplayProgressBar(interval))
            {
                EditorUtility.DisplayProgressBar(title, info, progress);
            }
        }

        public bool DisplayCancelableProgressBar(string title, string info, float progress, bool log = false, bool interval = false)
        {
            if (log)
            {
                Log(title + " : " + info);
            }
            if (CanDisplayProgressBar(interval))
            {
                return EditorUtility.DisplayCancelableProgressBar(title, info, progress);
            }
            return false;
        }

        private bool CanDisplayProgressBar(bool interval)
        {
            if (CommonModule.CommonConfig.IsBatchMode || (interval && EditorApplication.timeSinceStartup - lastTimeSinceStartup < progressInterval))
            {
                return false;
            }
            else
            {
                lastTimeSinceStartup = EditorApplication.timeSinceStartup;
                return true;
            }
        }

        public void DisplayRunError(string preText = null)
        {
            //if (BaseModuleStateConfig.BaseJson.Applying) //TODO:这里是干啥的？？
            {
                if (DisplayDialog(preText + "运行过程中发生错误：" + BaseModuleStateConfig.BaseJson.ErrorMessage, "详细信息", "确定"))
                {
                    EditorUtility.OpenWithDefaultApp(BaseModuleStateConfig.JsonPath);
                }
            }
        }
        #endregion

        #region 日志系统

        private double currentTime;
        private StreamWriter logWriter;

        public void LogHead(string text, ushort level)
        {
            string start = "";
            string end = "";
            switch (level)
            {
                case 0: start = end = ""; break;
                case 1: start = "-------- "; end = " --------"; break;
                case 2: start = "---- "; end = " ----"; break;
                case 3: start = "-- "; end = " --"; break;
                default: break;
            }
            Log(start + text + end);
        }

        public void Log(string text, bool delayFlush = false)
        {
            if (logWriter != null)
            {
                logWriter.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + text);
                if (delayFlush)
                {
                    if (EditorApplication.timeSinceStartup - currentTime > 0.2)
                    {
                        logWriter.Flush();
                        currentTime = EditorApplication.timeSinceStartup;
                    }
                }
                else
                {
                    logWriter.Flush();
                }
            }
        }

        public void StartLog()
        {
            if (logWriter == null && !string.IsNullOrEmpty(CommonModule.CommonConfig.PipelineLogPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CommonModule.CommonConfig.PipelineLogPath));
                logWriter = new StreamWriter(CommonModule.CommonConfig.PipelineLogPath, true);
            }
        }
        public void EndLog()
        {
            if (logWriter != null)
            {
                logWriter.Close();
                logWriter = null;
            }
        }

        /// <summary>
        /// 专用于打包工具启动和加载配置时，错误弹窗（编辑器模式）或 日志记录（批处理模式）
        /// </summary>
        /// <param name="customMessage"></param>
        /// <param name="e"></param>
        public void DisplayOrLogAndThrowError(string customMessage, Exception e)
        {
            if (CommonModule.CommonConfig.IsBatchMode)
            {
                StartLog();
                Log(customMessage + "\n[Exception] " + e.ToString());
                EndLog();
                throw e;
            }
            else
            {
                DisplayDialog(customMessage);
            }
        }

        #endregion
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
                DisplayOrLogAndThrowError("加载模块 " + ModuleName + " 配置文件时发生错误：" + e.Message
                            + "\n加载路径：" + ModuleConfig.JsonPath
                            + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + new TModuleConfig(), e);
                return false;
            }
        }

        public override bool LoadModuleStateConfig()
        {
            ModuleStateConfig.UserConfigsFolderPath = ModuleConfig.UserConfigsFolderPath; //拷贝配置项
            try
            {
                ModuleStateConfig.JsonPath = ModuleConfig.StateConfigPath;
                if (Directory.Exists(CommonModule.CommonConfig.DataRootPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ModuleStateConfig.JsonPath)); //Create _Configs目录
                    if (!File.Exists(ModuleStateConfig.JsonPath)) //状态配置文件是否存在
                    {
                        ModuleStateConfig.Save();
                    }
                    else
                    {
                        ModuleStateConfig.Load();
                    }
                    StateConfigAvailable = true;
                    return true;
                }
                else
                {
                    StateConfigAvailable = false;
                    return false;
                }
                //if (G.OverrideCurrentSavedConfigName != null) //用于总控
                //{
                //    CurrentConfig.Json.CurrentSavedConfigName = G.OverrideCurrentSavedConfigName;
                //    G.OverrideCurrentSavedConfigName = null;
                //}

            }
            catch (Exception e)
            {
                StateConfigLoadFailedMessage = "加载模块 " + ModuleName + " 状态配置文件时发生错误：" + e.Message
                            + "\n加载路径：" + ModuleStateConfig.JsonPath
                            + "\n请设置正确的文件路径以及形如以下所示的配置文件：\n" + new TModuleStateConfig();
                DisplayOrLogAndThrowError(StateConfigLoadFailedMessage, e);
                StateConfigAvailable = false;
                return false;
            }
        }
    }
}