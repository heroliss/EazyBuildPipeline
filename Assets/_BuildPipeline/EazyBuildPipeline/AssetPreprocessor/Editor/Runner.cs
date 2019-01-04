using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using EazyBuildPipeline.AssetPreprocessor.Editor;
using EazyBuildPipeline.AssetPreprocessor.Configs;
using System.Reflection;

namespace EazyBuildPipeline.AssetPreprocessor
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
        [NonSerialized] public int currentStepIndex;
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
                        totalCountList[currentStepIndex] = totalApplyCount;
                        break;
                    case "Applying:":
                        applyingID = int.Parse(messages[1]);
                        applyingFile = string.Join(" ", messages.Skip(2).ToArray());
                        break;
                    case "Error:":
                        errorMessage = string.Join(" ", messages.Skip(1).ToArray());
                        break;
                    case "Skip:":
                        skipCountList[currentStepIndex] = int.Parse(messages[1]);
                        break;
                    case "Success:":
                        successCountList[currentStepIndex] = int.Parse(messages[1]);
                        break;
                    case "LogFilePath:":
                        LogFilePathList[currentStepIndex] = string.Join(" ", messages.Skip(1).ToArray());
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

            //初始化两个步骤的信息集
            totalCountList = new List<int> { -1, -1 };
            skipCountList = new List<int>() { -1, -1 };
            successCountList = new List<int> { -1, -1 };
            LogFilePathList = new List<string> { null, null };

            //第一步
            currentStepIndex = 0;
            string copyFileArgs = Path.Combine(Module.ModuleConfig.ShellsFolderPath, "CopyFile.sh") + " "
                + Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, "CopyPreAssets.log") + " Assets " + Module.ModuleConfig.PreStoredAssetsFolderPath;
            copyFileArgs += " " + Module.UserConfig.Json.CopyFileTags;
            RunShell_ShowProgress_WaitForExit(copyFileArgs, "正在从PreStoredAssets里查找文件...", "第1步(共2步) 从PreStoredAssets拷贝文件至Assets目录");
            if (process.ExitCode != 0) //第一步出错
            {
                throw new EBPException("第一步CopyFile时发生错误：" + errorMessage);
            }

            //第二步
            currentStepIndex = 1;
            string importerLogPath = Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, "ImporterSetting.log");
            LogFilePathList[currentStepIndex] = importerLogPath;
            using (var writer = new StreamWriter(importerLogPath) { AutoFlush = true })
            {
                ApplyImporter(writer);
            }

            CommonModule.CommonConfig.Json.CurrentAssetTag = Module.UserConfig.Json.Tags;
            CommonModule.CommonConfig.Save();
        }

        private void ApplyImporter(StreamWriter writer)
        {
            writer.WriteLine("Start Setting Importers");
            foreach (var importerGroup in Module.UserConfig.Json.ImporterGroups)
            {
                Module.DisplayProgressBar("Setting " + importerGroup.Name, 0, true);
                writer.WriteLine("\nSetting " + importerGroup.Name);
                foreach (var labelGroup in importerGroup.LabelGroups)
                {
                    var guids = AssetDatabase.FindAssets(importerGroup.SearchFilter + " l:" + labelGroup.Label); //TODO
                    float total = guids.Length;
                    for (int i = 0; i < total; i++)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                        if (Module.DisplayCancelableProgressBar("Setting " + importerGroup.Name, assetPath, i / total))
                        {
                            throw new EBPException("运行被中止");
                        }
                        if(labelGroup.SetPropertyGroups(assetPath))
                        {
                            writer.WriteLine("Set  " + assetPath);
                        }
                        else
                        {
                            writer.WriteLine("Skip " + assetPath);
                        }
                    }
                }
            }
        }
    }
}