﻿using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;

namespace EazyBuildPipeline.PlayerBuilder
{
    public partial class Runner
    {
        public string ConfigURL_Game;
        public string ConfigURL_Language;
        public string ConfigURL_LanguageVersion;

        protected override void PostProcess()
        {
            Module.DisplayProgressBar("Renaming OBB File...", 0, true);
            RenameOBBFile();
        }

        protected override void PreProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Module.DisplayProgressBar("Applying PlayerSettings And ScriptDefines", 0.1f, true);
            ApplyPlayerSettingsAndScriptDefines();

            Module.DisplayProgressBar("Applying IOS PostProcess Settings", 0.2f, true);
            ApplyIOSPostProcessSettings();

            Module.DisplayProgressBar("Creating Building Configs Class File", 0.3f, true);
            CreateBuildingConfigsClassFile();

            if (CommonModule.CommonConfig.IsBatchMode)
            {
                Module.DisplayProgressBar("Start DownloadConfigs", 0.35f, true);
                DownLoadConfigs();
                DownLoadMultiLanguage();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        public void ApplyIOSPostProcessSettings()
        {
            var ps = Module.UserConfig.Json.PlayerSettings;

            iOSBuildPostProcessor.ProductName = ps.General.ProductName; //重复
            iOSBuildPostProcessor.ProvisioningProfile = ps.IOS.ProvisioningProfile; //重复
            iOSBuildPostProcessor.TeamID = ps.IOS.TeamID; //重复
            iOSBuildPostProcessor.FrameWorkPath = ps.IOS.ThirdFrameWorkPath;
            iOSBuildPostProcessor.IsBuildArchive = ps.IOS.IsBuildArchive;
            iOSBuildPostProcessor.ExportIpaPath = ps.IOS.ExportIpaPath;
            iOSBuildPostProcessor.TaskPath = ps.IOS.TaskPath;
            iOSBuildPostProcessor.BlueToothUsageDesc = ps.IOS.BlueToothUsageDesc;
            iOSBuildPostProcessor.PhotoUsageDesc = ps.IOS.PhotoUsageDesc;
            iOSBuildPostProcessor.PhotoUsageAddDesc = ps.IOS.PhotoUsageAddDesc;
            //iOSBuildPostProcessor.BuglyAppKey = ps.IOS.BuglyAppKey; //不需要
            BuglyInit.BuglyAppID = ps.General.BuglyAppID;
            BuglyInit.BuglyAppKey = ps.General.BuglyAppKey;
        }



        private void DownLoadConfigs()
        {
            string configsPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + "Configs";
            //WriteToLog("[EazyBuildPipeline] DwonLoad Configs Start.");

            //string srcPath = "http://10.1.1.10/configtool/data/" + Module.UserConfig.Json.PlayerSettings.General.DownloadConfigType + ".zip";
            NetWorkConnection.ConfigNetworkURL();

            //Module.DisplayProgressBar("Download Configs...", srcPath, 0, true);
            //DownLoadFile(srcPath, Path.Combine(configsPath, "StaticConfigs.zip"));
            Module.DisplayProgressBar("Download Game Config...", ConfigURL_Game, 0.4f, true);
            DownLoadFile(ConfigURL_Game, Path.Combine(configsPath, "StaticConfigs.zip"));  //TODO:这里的保存的名永远都是StaticConfigs.zip？？
        }

            private void DownLoadMultiLanguage()
        {
            string configsPath = Application.streamingAssetsPath + Path.DirectorySeparatorChar + "Configs";

            //WriteToLog("[EazyBuildPipeline] DwonLoad Language Start.");
            //var multiLanType = Module.UserConfig.Json.PlayerSettings.General.DownloadLanguageType;
            //string srcPath = "http://10.1.1.10/configtool/data/locale_bin/" + multiLanType + ".bbb";
            //Module.DisplayProgressBar("Download Language...", srcPath, 0, true);
            //DownLoadFile(srcPath, Path.Combine(configsPath, multiLanType + ".bbb"));
            Module.DisplayProgressBar("Download Language Config...", ConfigURL_Language, 0.6f, true);
            DownLoadFile(ConfigURL_Language, Path.Combine(configsPath, Path.GetFileName(ConfigURL_Language)));

            //srcPath = "http://10.1.1.10/configtool/data/locale_bin/" + multiLanType + ".json";
            //Module.DisplayProgressBar("Download Language...", srcPath, 0, true);
            //DownLoadFile(srcPath, Path.Combine(configsPath, "locale.json"));
            Module.DisplayProgressBar("Download Language Version Config...", ConfigURL_LanguageVersion, 0.8f, true);
            DownLoadFile(ConfigURL_LanguageVersion, Path.Combine(configsPath, Path.GetFileName(ConfigURL_LanguageVersion)));
        }

        private void DownLoadFile(string srcPath, string targetPath)
        {
            //WriteToLog("[EazyBuildPipeline] DwonLoad URL is: " + srcPath);
            //WriteToLog("[EazyBuildPipeline] DwonLoad targetPath is: " + targetPath);
            //WriteToLog("[EazyBuildPipeline] DwonLoad fileName is: " + fileName);
            //实例一个process类
            Process process = new Process();
            //设定程序名
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = Path.Combine(Module.ModuleConfig.ModuleRootPath,"Shells/DownloadFile.sh") + " " + srcPath + " " + targetPath;
            //Shell的使用
            process.StartInfo.UseShellExecute = false;
            //重新定向标准输入，错误输出
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            //设置cmd窗口的显示
            process.StartInfo.CreateNoWindow = true;
            //开始
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new EBPException("下载文件" + srcPath + "失败! 错误信息：\n" + process.StandardError.ReadToEnd());
            }
            process.Close();
            //ezDebug.LogError("DownLoadFile Completed");
        }

        void RenameOBBFile()
        {
            if (BuildPlayerOptions.target == BuildTarget.Android && PlayerSettings.Android.useAPKExpansionFiles)
            {
                string folderPath = Path.GetDirectoryName(BuildPlayerOptions.locationPathName);
                string obbSrcFileName = Path.GetFileNameWithoutExtension(BuildPlayerOptions.locationPathName) + ".main.obb";
                string obbSrcPath = Path.Combine(folderPath, obbSrcFileName);
                string obbTargetFileName = string.Format("main.{0}.{1}.obb", PlayerSettings.Android.bundleVersionCode, PlayerSettings.applicationIdentifier);
                string obbTargetPath = Path.Combine(folderPath, obbTargetFileName);
                File.Move(obbSrcPath, obbTargetPath);
            }
        }

        #region CreateBuildingConfigsClassFile
        private const string BuildingConfigsClassName = "ClientBuildingConfigs";
        private const string TargetCodeFile = "Assets/_Scripts/Auxiliary/" + BuildingConfigsClassName + ".cs";

        private void CreateBuildingConfigsClassFile()
        {
            using (StreamWriter writer = new StreamWriter(TargetCodeFile, false))
            {
                string code = GenerateCode();
                writer.WriteLine("{0}", code);
            }
        }

        /// <summary>
        /// Regenerates (and replaces) the code for ClassName with new build version id.
        /// </summary>
        /// <returns>
        /// Code to write to file.
        /// </returns>
        /// <param name='BuildVersion'>
        /// New bundle version.
        /// </param>
        private string GenerateCode()
        {
            var ps = Module.UserConfig.Json.PlayerSettings;
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);

            string build = "";

            switch (buildTargetGroup)
            {
                case BuildTargetGroup.iOS:
                    build = ps.IOS.BuildNumber;
                    break;
                case BuildTargetGroup.Android:
                    build = ps.Android.BundleVersionCode.ToString();
                    break;
                default:
                    break;
            }

            string code = "public static class " + BuildingConfigsClassName + "\n{\n";
            code += System.String.Format("\tpublic static readonly string BuildVersion = \"{0}\";\n", build);
            code += System.String.Format("\tpublic static readonly int Resourceversion = {0};\n", CommonModule.CommonConfig.Json.CurrentResourceVersion);
            code += System.String.Format("\tpublic static readonly string BuglyAppId = \"{0}\";\n", ps.General.BuglyAppID);
            code += System.String.Format("\tpublic static readonly string BuglyAppKey = \"{0}\";", ps.General.BuglyAppKey);
            code += "\n}\n";
            return code;
        }
        #endregion

        //public static void WriteToLog(string str)
        //{
        //    if (taskPath != string.Empty || FileStaticAPI.IsFileExists(taskPath))
        //    {
        //        string path = Path.Combine(taskPath, "log.txt");
        //        if (FileStaticAPI.IsFileExists(path))
        //        {
        //            FileStaticAPI.CreateFile(path);
        //        }
        //        FileStaticAPI.Write(path, DateTime.Now.ToString() + str, true);
        //    }
        //}
    }
}