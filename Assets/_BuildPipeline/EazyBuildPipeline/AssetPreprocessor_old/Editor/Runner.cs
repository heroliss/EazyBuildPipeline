using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using EazyBuildPipeline.AssetPreprocessor_old.Editor;
using EazyBuildPipeline.AssetPreprocessor_old.Configs;

namespace EazyBuildPipeline.AssetPreprocessor_old
{
    [Serializable]
    public partial class Runner : EBPRunner<Module,
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        [NonSerialized] public Process process;
        [NonSerialized] public int RuningShellCount;
        [NonSerialized] public int applyingID;
        [NonSerialized] public int totalApplyCount;
        [NonSerialized] public List<int> totalCountList;
        [NonSerialized] public List<int> skipCountList;
        [NonSerialized] public List<int> successCountList;
        [NonSerialized] public List<string> LogFilePathList;
        [NonSerialized] public int currentShellIndex;
        [NonSerialized] public string applyingFile;
        [NonSerialized] public string errorMessage;

        public Runner() { }
        public Runner(Module module) : base(module)
        {
        }

        private void RunShell_ShowProgress_WaitForExit(string arguments, string startMessage, string titleMessage)
        {
            applyingFile = startMessage;
            applyingID = 0;
            totalApplyCount = 0;
            RunShell(arguments);
            while (RuningShellCount > 0)
            {
                string s = string.Format("({0}/{1})", applyingID, totalApplyCount);
                Module.DisplayProgressBar(titleMessage + s, applyingFile, (float)applyingID / totalApplyCount);
                System.Threading.Thread.Sleep(50);
            }
            Module.DisplayProgressBar(titleMessage, "Finish!", 1, true);
        }

        string Arguments(params string[] arguments)
        {
            return " " + string.Join(" ", arguments);
        }
        void RunShell(string arguments)
        {
            //实例一个process类
            process = new Process();
            //设定程序名
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = arguments;
            //Shell的使用
            process.StartInfo.UseShellExecute = false;
            //重新定向标准输入，错误输出
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            //设置cmd窗口的显示
            process.StartInfo.CreateNoWindow = true;
            //设置标准输出响应
            process.OutputDataReceived += OnShellOutputDataReceived;
            process.ErrorDataReceived += OnShellErrorDataReceived;
            process.EnableRaisingEvents = true;
            process.Exited += OnShellExited;
            //开始
            RuningShellCount++;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        private void OnShellErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            errorMessage = e.Data;
        }

        private void OnShellExited(object sender, EventArgs e)
        {
            RuningShellCount--;
        }

        private void OnShellOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                string[] messages = e.Data.Split();
                switch (messages[0])
                {
                    case "Total:":
                        totalApplyCount = int.Parse(messages[1]);
                        totalCountList[currentShellIndex] = totalApplyCount;
                        break;
                    case "Applying:":
                        applyingID = int.Parse(messages[1]);
                        applyingFile = string.Join(" ", messages.Skip(2).ToArray());
                        break;
                    case "Error:":
                        errorMessage = string.Join(" ", messages.Skip(1).ToArray());
                        break;
                    case "Skip:":
                        skipCountList[currentShellIndex] = int.Parse(messages[1]);
                        break;
                    case "Success:":
                        successCountList[currentShellIndex] = int.Parse(messages[1]);
                        break;
                    case "LogFilePath:":
                        LogFilePathList[currentShellIndex] = string.Join(" ", messages.Skip(1).ToArray());
                        break;
                    default:
                        break;
                }
            }
        }

        protected override void CheckProcess(bool onlyCheckConfig = false)
        {
            if (Module.UserConfig.Json.Tags.Length == 0)
            {
                throw new EBPCheckFailedException("错误：Tag不能为为空！");
            }
            var target = BuildTarget.NoTarget;
            string targetStr = Module.UserConfig.Json.Tags[0];
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetStr, true);
            }
            catch
            {
                throw new EBPCheckFailedException("没有此平台：" + targetStr);
            }

            if (!onlyCheckConfig)
            {
                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    throw new EBPCheckFailedException(string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target));
                }
                if (!Directory.Exists(Module.ModuleConfig.PreStoredAssetsFolderPath))
                {
                    throw new EBPCheckFailedException("不能应用配置，找不到目录:" + Module.ModuleConfig.PreStoredAssetsFolderPath);
                }
            }
            base.CheckProcess(onlyCheckConfig);
        }

        protected override void RunProcess()
        {
            CommonModule.CommonConfig.Json.CurrentAssetTag = new string[] { "Applying" }.Concat(Module.UserConfig.Json.Tags).ToArray();
            CommonModule.CommonConfig.Save();

            string platform = "";//TODO：这里能否使用EditorUserBuildSettings.activeBuildTarget？
#if UNITY_ANDROID
			platform = "Android";
#elif UNITY_IOS
            platform = "iPhone";
#endif
            //TODO:这里能否再优化一下结构？
            string copyFileArgs = Path.Combine(Module.ModuleConfig.ShellsFolderPath, "CopyFile.sh") + " "
                + Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, "CopyPreAssets.log") + " Assets " + Module.ModuleConfig.PreStoredAssetsFolderPath;
            string changeMetaArgs = Path.Combine(Module.ModuleConfig.ShellsFolderPath, "ModifyMeta.sh") + " "
                + Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, "ModifyMetaFile.log") + " Assets " + platform;
            int changeMetaArgsLength = changeMetaArgs.Length;
            foreach (var group in Module.UserConfig.Json.Groups)
            {
                switch (group.FullGroupName)
                {
                    case "Language":
                        copyFileArgs += Arguments(group.Options[0]);
                        break;
                    case "Texture Import Settings Override/Max Size":
                        foreach (string option in group.Options)
                        {
                            string[] splits = option.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                            changeMetaArgs += Arguments("maxTextureSize", splits[0].Trim(), splits[1].Trim());
                        }
                        break;
                    case "Texture Import Settings Override/Format":
                        foreach (string option in group.Options)
                        {
                            string[] splits = option.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                            TextureFormat source, target;
                            if (!StringMaps.TextureMaps.TryGetValue(splits[0].Trim(), out source))
                                source = (TextureFormat)Enum.Parse(typeof(TextureFormat), splits[0].Trim().Replace(' ', '_'));
                            if (!StringMaps.TextureMaps.TryGetValue(splits[0].Trim(), out target))
                                target = (TextureFormat)Enum.Parse(typeof(TextureFormat), splits[1].Trim().Replace(' ', '_'));
                            changeMetaArgs += Arguments("textureFormat", ((int)source).ToString(), ((int)target).ToString());
                        }
                        break;
                    default:
                        break;
                }
            }
            currentShellIndex = 0;
            totalCountList = new List<int> { -1, -1 };
            skipCountList = new List<int>() { -1, -1 };
            successCountList = new List<int> { -1, -1 };
            LogFilePathList = new List<string> { null, null };

            RunShell_ShowProgress_WaitForExit(copyFileArgs, "正在从PreStoredAssets里查找文件...", "第1步(共2步) 从PreStoredAssets拷贝文件至Assets目录");
            if (process.ExitCode != 0) //第一步出错
            {
                throw new EBPException("第一步CopyFile时发生错误：" + errorMessage);
            }
            if (changeMetaArgs.Length != changeMetaArgsLength) //第二步勾选
            {
                currentShellIndex++;
                RunShell_ShowProgress_WaitForExit(changeMetaArgs, "正在查找.meta文件...", "第2步(共2步) 修改meta文件");
            }
            if (process.ExitCode != 0) //第二步出错
            {
                throw new EBPException("第二步ChangeMeta时发生错误：" + errorMessage);
            }
            CommonModule.CommonConfig.Json.CurrentAssetTag = Module.UserConfig.Json.Tags;
            CommonModule.CommonConfig.Save();
        }
    }
}