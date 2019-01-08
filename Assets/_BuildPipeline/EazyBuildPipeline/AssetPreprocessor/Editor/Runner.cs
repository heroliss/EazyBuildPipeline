using UnityEditor;
using System.IO;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using EazyBuildPipeline.AssetPreprocessor.Configs;
using System.Text.RegularExpressions;

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

        readonly Regex regexSpace = new Regex(@"\s");
        readonly Regex regexOperator = new Regex("[^" + new string(TruthExpressionParser.opset) + "]+");

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
                    throw new EBPCheckFailedException("不能应用配置，找不到PreStoredAssets目录:" + Module.ModuleConfig.PreStoredAssetsFolderPath);
                }
            }
            //检查所有label表达式
            foreach (var importerGroup in Module.UserConfig.Json.ImporterGroups)
            {
                foreach (var labelGroup in importerGroup.LabelGroups)
                {
                    string expression = labelGroup.LabelExpression;
                    if (string.IsNullOrEmpty(expression))
                    {
                        throw new EBPCheckFailedException("条件表达式不能为空!");
                    }
                    expression = regexSpace.Replace(expression, ""); //消除所有\t\n\r\v\f
                    string[] labels = expression.Split(TruthExpressionParser.opset, StringSplitOptions.RemoveEmptyEntries); //获得labels
                    expression = regexOperator.Replace(expression, "o") + "#"; //获得表达式
                    int labelsLen = labels.Length;
                    bool[] values = new bool[labelsLen]; //存放每个资产的label包含结果
                    try
                    {
                        TruthExpressionParser.Parse(expression, values);
                    }
                    catch (Exception e)
                    {
                        throw new EBPCheckFailedException("条件表达式不正确：(" + e.Message + ")\n\n" + labelGroup.LabelExpression);
                    }
                    if (TruthExpressionParser.StackEmpty() == false)
                    {
                        throw new EBPCheckFailedException("条件表达式不正确（栈没有清空）：\n\n" + labelGroup.LabelExpression);
                    }
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

            EBPUtility.RefreshAssets();

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

        private void ApplyImporter(StreamWriter logWriter)
        {
            int totalCount = 0, skipCount = 0, successCount = 0; //统计变量
            logWriter.WriteLine("Start Setting Importers");
            foreach (var importerGroup in Module.UserConfig.Json.ImporterGroups)
            {
                Module.DisplayProgressBar("Setting " + importerGroup.Name, 0, true);
                logWriter.WriteLine("\nSetting " + importerGroup.Name);
                var assetInfos = GetAssetInfoList(importerGroup.SearchFilter);
                int assetInfosLen = assetInfos.Length;
                foreach (var labelGroup in importerGroup.LabelGroups)
                {
                    if (!labelGroup.Active || labelGroup.LabelExpression.Trim() == "")
                    {
                        continue;
                    }

                    string expression = labelGroup.LabelExpression;
                    expression = regexSpace.Replace(expression, ""); //消除所有\t\n\r\v\f
                    string[] labels = expression.Split(TruthExpressionParser.opset, StringSplitOptions.RemoveEmptyEntries); //获得labels
                    expression = regexOperator.Replace(expression, "o") + "#"; //获得表达式
                    int labelsLen = labels.Length;
                    bool[] values = new bool[labelsLen]; //存放每个资产的label包含结果
                    for (int i = 0; i < assetInfosLen; i++)
                    {
                        var assetInfo = assetInfos[i];
                        //没有label的资产跳过
                        if(assetInfo.Labels == null) { continue; }
                        //标记该Asset是否包含当前表达式中的label
                        for (int j = 0; j < labelsLen; j++)
                        {
                            values[j] = assetInfo.Labels.Contains(labels[j]);
                        }
                        //计算真值表达式
                        bool truth = TruthExpressionParser.Parse(expression, values);
                        //值为false则跳过
                        if (truth == false) { continue; }
                        //至此该Asset可以被设置
                        if (Module.DisplayCancelableProgressBar("Setting " + importerGroup.Name, assetInfo.Path, i / assetInfosLen, false, true))
                        {
                            throw new EBPException("运行被中止");
                        }
                        totalCount++;
                        if (labelGroup.SetPropertyGroups(assetInfo.Path))
                        {
                            logWriter.WriteLine("Set  " + assetInfo.Path);
                            successCount++;
                        }
                        else
                        {
                            logWriter.WriteLine("Skip " + assetInfo.Path);
                            skipCount++;
                        }
                    }
                }
            }
            logWriter.WriteLine("\n\nTotal: " + totalCount);
            logWriter.WriteLine("\nSkip: " + skipCount);
            logWriter.WriteLine("\nSuccess: " + successCount);
            totalCountList[currentStepIndex] = totalCount;
            skipCountList[currentStepIndex] = skipCount;
            successCountList[currentStepIndex] = successCount;
        }

        private struct AssetInfo
        {
            public string Path;
            public string[] Labels;
        }
        private AssetInfo[] GetAssetInfoList(string searchFilter)
        {
            string[] guids = AssetDatabase.FindAssets(searchFilter);
            List<string> labelList = new List<string>();
            AssetInfo[] assetInfos = new AssetInfo[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                Module.DisplayProgressBar("Reading Asset Meta Info Searched by " + searchFilter, assetPath, (float)i / guids.Length, false, true);
                assetInfos[i] = new AssetInfo() { Path = assetPath };
                using (var file = File.OpenText(assetPath + ".meta"))
                {
                    bool startReadLabel = false;
                    while (!file.EndOfStream)
                    {
                        if (file.ReadLine() == "labels:")
                        {
                            startReadLabel = true;
                            break;
                        }
                    }
                    if (startReadLabel)
                    {
                        labelList.Clear();
                        while (!file.EndOfStream)
                        {
                            string line = file.ReadLine();
                            if (line[0] == '-')
                            {
                                labelList.Add(line.Substring(2));
                            }
                            else
                            {
                                break;
                            }
                        }
                        assetInfos[i].Labels = labelList.ToArray();
                    }
                }
            }
            return assetInfos;
        }
    }
}