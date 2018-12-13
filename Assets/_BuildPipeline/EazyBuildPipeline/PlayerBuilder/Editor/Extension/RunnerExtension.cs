using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;
using UnityEditor.XCodeEditor;
using UnityEditor.iOS.Xcode;
using System.Collections.Generic;
using EazyBuildPipeline.PlayerBuilder.Configs;
using System.Text.RegularExpressions;

namespace EazyBuildPipeline.PlayerBuilder
{
    public partial class Runner
    {
        public void Prepare()
        {
            try
            {
                Module.StartLog();
                Module.LogHead("Start Prepare", 1);
                EBPUtility.RefreshAssets();

                Module.DisplayProgressBar("Preparing BuildOptions", 0, true);
                PrepareBuildOptions();

                Module.DisplayProgressBar("Start DownloadConfigs", 0.02f, true);
                DownLoadConfigs(0.02f, 0.3f);

                Module.DisplayProgressBar("Start Copy Directories", 0.3f, true);
                CopyAllDirectories();

                Module.DisplayProgressBar("Creating Building Configs Class File", 0.8f, true);
                CreateBuildingConfigsClassFile();

                Module.DisplayProgressBar("Applying PlayerSettings", 0.9f, true);
                ApplyPlayerSettings(BuildPlayerOptions.target);

                Module.DisplayProgressBar("Applying Scripting Defines", 0.95f, true);
                ApplyScriptDefines(BuildPlayerOptions.target);

                EBPUtility.RefreshAssets();
            }
            catch (Exception e)
            {
                var state = Module.ModuleStateConfig.Json;
                state.ErrorMessage = e.Message;
                state.DetailedErrorMessage = e.ToString();
                if (!string.IsNullOrEmpty(Module.ModuleStateConfig.JsonPath)) Module.ModuleStateConfig.Save();
                Module.Log(state.DetailedErrorMessage);
                throw new EBPException(e.Message, e);
            }
            finally
            {
                Module.LogHead("End Prepare", 1);
                Module.EndLog();
                EditorUtility.ClearProgressBar();
            }
        }

        protected override void PreProcess()
        {
            EBPUtility.RefreshAssets();

            Module.DisplayProgressBar("Preparing BuildOptions", 0, true);
            PrepareBuildOptions();

            Module.DisplayProgressBar("-- Start Handle Wrap Files --", 0f, true);
            HandleWrapFiles(0f, 1f);
            Module.DisplayProgressBar("-- End Handle Wrap Files --", 1f, true);

            Module.DisplayProgressBar("Applying PostProcess Settings", 1f, true);
            ApplyPostProcessSettings();

            //为了防止第二次启动Unity后部分设置自动消失（如Android的Keystore配置）
            Module.DisplayProgressBar("Applying PlayerSettings", 1f, true);
            ApplyPlayerSettings(BuildPlayerOptions.target);

            EBPUtility.RefreshAssets();

            iOSBuildPostProcessor.DisableOnce = true; //HACK: 关闭一次旧的后处理过程
        }

        protected override void PostProcess()
        {
            //工程需要的后处理
            switch (BuildPlayerOptions.target)
            {
                case BuildTarget.Android:
                    Module.DisplayProgressBar("Renaming OBB File...", 0, true);
                    RenameOBBFileForAndroid();
                    break;
                case BuildTarget.iOS:
                    Module.DisplayProgressBar("Postprocessing for iOS...", 0, true);
                    IOSPostProcess(BuildPlayerOptions.locationPathName);
                    break;
                default:
                    throw new EBPException("意外的平台：" + BuildPlayerOptions.target.ToString());
            }

            EBPUtility.RefreshAssets();
        }

        protected override void Finally()
        {
            iOSBuildPostProcessor.DisableOnce = false;

            //还原宏定义
            Module.DisplayProgressBar("Revert Scripting Defines", 0f, true);
            ApplyScriptDefines(EditorUserBuildSettings.activeBuildTarget, true);

            //还原被拷贝覆盖的文件
            Module.DisplayProgressBar("Revert Copied Files", 0f, true);
            RevertAllCopiedFiles();

            //清除Wrap
            ToLuaMenu.ClearLuaWraps();

            EBPUtility.RefreshAssets();
        }

        public void HandleWrapFiles(float startProgress, float endProgress)
        {
            float progressLength = endProgress - startProgress;
            float progress = 0f;
            var steps = new ToLuaMenu.ClearWrapFilesAndCreateSteps();
            foreach (var step in steps)
            {
                Module.DisplayProgressBar(step, startProgress + progressLength * progress, true);
                progress += 1f / steps.StepCount;
            }
            Module.DisplayProgressBar("Clear & Generate Wrap Files Finished!", startProgress + progressLength * 1, true);
        }

        #region CopyDirectories & RevertCopiedFiles
        string message;
        string errorMessage;
        readonly string copyFileLogName = "CopiedFilesForBuild.log";

        private void CopyAllDirectories()
        {
            string directoryRegexStr = CommonModule.CommonConfig.Json.DirectoryRegex;
            string fileRegexStr = CommonModule.CommonConfig.Json.FileRegex;
            List<UserConfig.PlayerSettings.CopyItem> copyList;
            switch (BuildPlayerOptions.target)
            {
                case BuildTarget.Android:
                    string directoryRegexStr_Android = Module.UserConfig.Json.PlayerSettings.Android.CopyDirectoryRegex;
                    string fileRegexStr_Android = Module.UserConfig.Json.PlayerSettings.Android.CopyFileRegex;
                    if (!string.IsNullOrEmpty(directoryRegexStr_Android))
                    {
                        directoryRegexStr = directoryRegexStr_Android;
                    }
                    if (!string.IsNullOrEmpty(fileRegexStr_Android))
                    {
                        fileRegexStr = fileRegexStr_Android;
                    }
                    copyList = Module.UserConfig.Json.PlayerSettings.Android.CopyList;
                    break;
                case BuildTarget.iOS:
                    string directoryRegexStr_IOS = Module.UserConfig.Json.PlayerSettings.IOS.CopyDirectoryRegex;
                    string fileRegexStr_IOS = Module.UserConfig.Json.PlayerSettings.IOS.CopyFileRegex;
                    if (!string.IsNullOrEmpty(directoryRegexStr_IOS))
                    {
                        directoryRegexStr = directoryRegexStr_IOS;
                    }
                    if (!string.IsNullOrEmpty(fileRegexStr_IOS))
                    {
                        fileRegexStr = fileRegexStr_IOS;
                    }
                    copyList = Module.UserConfig.Json.PlayerSettings.IOS.CopyList;
                    break;
                default:
                    throw new EBPException("意外的平台：" + BuildPlayerOptions.target.ToString());
            }

            Regex directoryRegex = string.IsNullOrEmpty(directoryRegexStr) ? null : new Regex(directoryRegexStr);
            Regex fileRegex = string.IsNullOrEmpty(fileRegexStr) ? null : new Regex(fileRegexStr);

            using (var copyFileLogWriter = new StreamWriter(Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, copyFileLogName)) { AutoFlush = true })
            {
                for (int i = 0; i < copyList.Count; i++)
                {
                    if (!copyList[i].Active)
                    {
                        continue;
                    }
                    Module.DisplayProgressBar(string.Format("Copy Directory... ({0}/{1})", i + 1, copyList.Count), copyList[i].SourcePath + " -> " + copyList[i].TargetPath, 0.5f, true);
                    bool revertThis = copyList[i].Revert;
                    EBPUtility.CopyDirectory(copyList[i].SourcePath, copyList[i].TargetPath, copyList[i].CopyMode, directoryRegex, fileRegex,
                        (targetPath) =>
                        {
                            copyFileLogWriter.WriteLine((revertThis ? "R " : "  ") + targetPath);
                        });
                }
            }
        }

        private void RevertAllCopiedFiles()
        {
            var copyList = new List<UserConfig.PlayerSettings.CopyItem>();
            switch (BuildPlayerOptions.target)
            {
                case BuildTarget.Android:
                    copyList = Module.UserConfig.Json.PlayerSettings.Android.CopyList;
                    break;
                case BuildTarget.iOS:
                    copyList = Module.UserConfig.Json.PlayerSettings.IOS.CopyList;
                    break;
                default:
                    throw new EBPException("意外的平台：" + BuildPlayerOptions.target.ToString());
            }
            //读入所有已拷贝的文件列表
            var revertFilesList = new List<string>();
            var copiedFilesLogText = new string[0];
            string copyFileLogPath = Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, copyFileLogName);
            if (File.Exists(copyFileLogPath))
            {
                copiedFilesLogText = File.ReadAllLines(copyFileLogPath);
            }
            foreach (var line in copiedFilesLogText)
            {
                if (line[0] == 'R')
                {
                    revertFilesList.Add(line.Substring(2).Trim());
                }
            }
            //单独的日志文件
            string logPath = Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, "RevertFiles.log");
            errorMessage = "";
            float progress = 0;
            //删除所有已拷贝并标记为R(revert)的文件（不存在的文件不记录日志）
            using (var logWriter = new StreamWriter(logPath, true))
            {
                Module.DisplayProgressBar("Start Delete Copied Files", 0, true);
                logWriter.WriteLine("Start Delete Files"); logWriter.Flush();
                foreach (string file in revertFilesList)
                {
                    if (File.Exists(file))
                    {
                        Module.DisplayProgressBar("Delete Copied Files", file, progress++ % 1000 / 1000);
                        File.Delete(file);
                        logWriter.WriteLine("Deleted: " + file); logWriter.Flush();
                    }
                }
                logWriter.WriteLine("End Delete Files"); logWriter.Flush();
                Module.DisplayProgressBar("End Delete Copied Files", 0, true);
            }

            //还原目录
            progress = 0;
            Module.DisplayProgressBar("Start Revert All Copied Files", 0, true);
            foreach (var item in copyList)
            {
                if (item.Revert == false)
                {
                    continue;
                }
                string title = "Revert Copied Files in " + EBPUtility.Quote(Path.GetFileName(item.TargetPath));
                Module.DisplayProgressBar(title, "Start Revert " + EBPUtility.Quote(item.TargetPath), 0, true);

                Process p = SVNUpdate.Runner.ExcuteCommand("/bin/bash",
                    EBPUtility.Quote(Path.Combine(Module.ModuleConfig.ModuleRootPath, "Shells/SVNRevert.sh")) + " " +
                    EBPUtility.Quote(item.TargetPath) + " " +
                    EBPUtility.Quote(logPath), OnReceived, OnErrorReceived, null);

                while (!p.HasExited)
                {
                    Module.DisplayProgressBar(title, message, progress++ % 1000 / 1000);
                    System.Threading.Thread.Sleep(50);
                }

                if (p.ExitCode != 0)
                {
                    throw new EBPException("还原目录(" + item.TargetPath + ")时发生错误：" + errorMessage);
                }
                Module.DisplayProgressBar(title, "Finish!", 1, true);
            }

            Module.DisplayProgressBar("End Revert All Copied Files", 1, true);
        }

        void OnErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data.Trim() != "")
            {
                errorMessage += e.Data + "\n";
            }
        }

        void OnReceived(object sender, DataReceivedEventArgs e)
        {
            message = e.Data;
        }

        #endregion

        private void PrepareBuildOptions()
        {
            //准备BuildOptions
            BuildOptions buildOptions =
                (Module.ModuleStateConfig.Json.DevelopmentBuild ? BuildOptions.Development : BuildOptions.None) |
                (Module.ModuleStateConfig.Json.ConnectWithProfiler ? BuildOptions.ConnectWithProfiler : BuildOptions.None) |
                (Module.ModuleStateConfig.Json.AllowDebugging ? BuildOptions.AllowDebugging : BuildOptions.None) |
                (Module.UserConfig.Json.BuildSettings.CompressionMethod == UserConfig.BuildSettings.CompressionMethodEnum.LZ4 ? BuildOptions.CompressWithLz4 : BuildOptions.None) |
                (Module.UserConfig.Json.BuildSettings.CompressionMethod == UserConfig.BuildSettings.CompressionMethodEnum.LZ4HC ? BuildOptions.CompressWithLz4HC : BuildOptions.None);

            //设置路径和文件名
            string tagsPath = Path.Combine(Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(Module.ModuleStateConfig.Json.CurrentTag));
            string locationPath;
            string versionInfo = string.Format("{0}_{1}.{2}", PlayerSettings.productName, PlayerSettings.bundleVersion, PlayerSettings.Android.bundleVersionCode);
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                    locationPath = Path.Combine(tagsPath, versionInfo + ".apk");
                    break;
                case BuildTarget.iOS:
                    locationPath = Path.Combine(tagsPath, "Project");
                    break;
                default:
                    throw new EBPException("意外的平台：" + EditorUserBuildSettings.activeBuildTarget);
            }

            //获取场景
            string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes).Take(2).ToArray(); //Hack: 只获取头两个场景

            //构成BuildPlayerOptions
            BuildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPath,
                target = EditorUserBuildSettings.activeBuildTarget,
                options = buildOptions
            };
        }

        #region DownLoad Configs

        private void DownLoadConfigs(float startProgress, float endProgress)
        {
            float progressLength = endProgress - startProgress;
            string configsPath = Path.Combine(Application.streamingAssetsPath, "Configs");
            //重建目录
            Module.DisplayProgressBar("正在重建目录", configsPath, startProgress + progressLength * 0, true);
            if (Directory.Exists(configsPath))
            {
                Directory.Delete(configsPath, true);
            }
            Directory.CreateDirectory(configsPath);

            //NetWorkConnection.ConfigNetworkURL();

            //.zip
            string configURL_Game = Module.UserConfig.Json.PlayerSettings.General.ConfigURL_Game;
            if (!string.IsNullOrEmpty(configURL_Game))
            {
                Module.DisplayProgressBar("Download Game Config...", configURL_Game, startProgress + progressLength * 0f, true);
                DownLoadFile(configURL_Game, Path.Combine(configsPath, "StaticConfigs.zip"));
            }
            //.bbb
            string configURL_Language = Module.UserConfig.Json.PlayerSettings.General.ConfigURL_Language;
            if (!string.IsNullOrEmpty(configURL_Language))
            {
                Module.DisplayProgressBar("Download Language Config...", configURL_Language, startProgress + progressLength * 0.3f, true);
                DownLoadFile(configURL_Language, Path.Combine(configsPath, Path.GetFileName(configURL_Language)));
            }
            //.json
            string configURL_LanguageVersion = Module.UserConfig.Json.PlayerSettings.General.ConfigURL_LanguageVersion;
            if (!string.IsNullOrEmpty(configURL_LanguageVersion))
            {
                Module.DisplayProgressBar("Download Language Version Config...", configURL_LanguageVersion, startProgress + progressLength * 0.6f, true);
                DownLoadFile(configURL_LanguageVersion, Path.Combine(configsPath, Path.GetFileName(configURL_LanguageVersion)));
            }
        }

        private void DownLoadFile(string srcPath, string targetPath)
        {
            //实例一个process类
            Process process = new Process();
            //设定程序名
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = Path.Combine(Module.ModuleConfig.ModuleRootPath, "Shells/DownloadFile.sh") + " " + srcPath + " " + targetPath;
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
        }

        #endregion

        void RenameOBBFileForAndroid()
        {
            if (PlayerSettings.Android.useAPKExpansionFiles)
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

            string build = Module.ModuleStateConfig.Json.BuildNumber.ToString();
            string resourceVersion = Module.ModuleStateConfig.Json.ResourceVersion.ToString();

            string code = "public static class " + BuildingConfigsClassName + "\n{\n";
            code += string.Format("\tpublic static readonly string BuildVersion = \"{0}\";\n", build);
            code += string.Format("\tpublic static readonly int Resourceversion = {0};\n", resourceVersion);
            code += string.Format("\tpublic static readonly string BuglyAppId = \"{0}\";\n", ps.General.BuglyAppID);
            code += string.Format("\tpublic static readonly string BuglyAppKey = \"{0}\";\n", ps.General.BuglyAppKey);

            if (ps.General.Channel == EazyGameChannel.Channels.None)
            {
                code += string.Format("\tpublic static readonly string GameChannel = {0};\n", "string.Empty");
            }
            else
            {
                code += string.Format("\tpublic static readonly string GameChannel = \"{0}\";\n", ps.General.Channel.ToString());
            }

            code += "}\n";
            return code;
        }
        #endregion

        public void ApplyPostProcessSettings()
        {
            var ps = Module.UserConfig.Json.PlayerSettings;

            //IsBuildArchive = ps.IOS.IsBuildArchive;
            //ExportIpaPath = ps.IOS.ExportIpaPath;
            //TaskPath = ps.IOS.TaskPath;

            //iOSBuildPostProcessor.BuglyAppKey = ps.IOS.BuglyAppKey; //不需要
            BuglyInit.BuglyAppID = ps.General.BuglyAppID;
            BuglyInit.BuglyAppKey = ps.General.BuglyAppKey;
        }

        #region iOSPostProcess
        private void IOSPostProcess(string path)
        {
            var psIOS = Module.UserConfig.Json.PlayerSettings.IOS;

            //init
            string projPath = PBXProject.GetPBXProjectPath(path);

            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));

            string target = proj.TargetGuidByName("Unity-iPhone");

            //customsetting
            //*******************************添加framework*******************************//
            proj.AddFrameworkToProject(target, "CoreLocation.framework", false);
            proj.AddFrameworkToProject(target, "UserNotifications.framework", false);
            proj.AddFrameworkToProject(target, "SystemConfiguration.framework", false);
            proj.AddFrameworkToProject(target, "Security.framework", false);
            proj.AddFrameworkToProject(target, "CoreTelephony.framework", false);
            proj.AddFrameworkToProject(target, "JavaScriptCore.framework", false);
            proj.AddFrameworkToProject(target, "MobileCoreServices.framework", false);
            proj.AddFrameworkToProject(target, "AVFoundation.framework", false);

            //*******************************添加tbd*******************************//
            proj.AddFileToBuild(target, proj.AddFile("usr/lib/libz.tbd", "Frameworks/libz.tbd", PBXSourceTree.Sdk));
            proj.AddFileToBuild(target, proj.AddFile("usr/lib/libc++.tbd", "Frameworks/libc++.tbd", PBXSourceTree.Sdk));
            proj.AddFileToBuild(target, proj.AddFile("usr/lib/libsqlite3.tbd", "Frameworks/libsqlite3.tbd", PBXSourceTree.Sdk));

            //*******************************添加第三方framework*******************************//
            CopyAndReplaceDirectory(psIOS.ThirdFrameWorkPath + "/BuglySDK/Bugly/Bugly.framework", Path.Combine(path, "Frameworks/Bugly.framework"));
            proj.AddFileToBuild(target, proj.AddFile("Frameworks/Bugly.framework", "Frameworks/Bugly.framework", PBXSourceTree.Source));

            // 追加framework的检索目录
            proj.SetBuildProperty(target, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");
            proj.AddBuildProperty(target, "FRAMEWORK_SEARCH_PATHS", "$(PROJECT_DIR)/Frameworks");

            //*******************************设置buildsetting*******************************//
            proj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
            proj.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");

            proj.SetBuildProperty(target, "BUILD_PRODUCTS_PATH", path);
            proj.SetBuildProperty(target, "DEBUG_INFORMATION_FORMAT", "DWARF with dSYM File");


            //*******************************设置capability*******************************//




            //*******************************设置plist文件*******************************//
            // XCPlist list = new XCPlist (xcodePath);
            // string blueToothAdd = @"<key>NSBluetoothPeripheralUsageDescription</key><string>" + BlueToothUsageDesc + @"</string>"; //添加蓝牙权限
            // list.AddKey (blueToothAdd);
            // list.Save ();

            // XCPlist list2 = new XCPlist(xcodePath);
            // string photoAdd = @"<key>NSPhotoLibraryUsageDescription</key><string>" + PhotoUsageDesc + @"</string>"; //添加相机权限
            // list2.AddKey(photoAdd);
            // list2.Save();

            // XCPlist list3 = new XCPlist(xcodePath);
            // string photoAdd2 = @"<key>NSPhotoLibraryAddUsageDescription</key><string>" + PhotoUsageAddDesc + @"</string>"; //添加iOS11相机权限
            // list3.AddKey(photoAdd2);
            // list3.Save();

            string plistPath = Path.Combine(path, "info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSBluetoothPeripheralUsageDescription", psIOS.BlueToothUsageDesc);
            plist.root.SetString("NSPhotoLibraryUsageDescription", psIOS.PhotoUsageDesc);
            plist.root.SetString("NSPhotoLibraryAddUsageDescription", psIOS.PhotoUsageAddDesc);
            PlistElementArray loginChannelsArr = plist.root.CreateArray("LSApplicationQueriesSchemes");
            loginChannelsArr.AddString("mqqapi");

            File.WriteAllText(plistPath, plist.WriteToString());



            //*******************************编辑代码文件*******************************//
            //读取UnityAppController.mm文件
            XClass UnityAppController = new XClass(path + "/Classes/UnityAppController.mm");

            //在指定代码后面增加一行代码
            UnityAppController.WriteBelow("#include \"PluginBase/AppDelegateListener.h\"", "#import <Bugly/Bugly.h>");
            UnityAppController.WriteBelow("[KeyboardDelegate Initialize];", "[Bugly startWithAppId:@\"" + BuglyInit.BuglyAppID + "\"];");

            File.WriteAllText(projPath, proj.WriteToString());

            //*******************************Build Xcode*******************************//
            if (psIOS.IPAExportOptions.ExportIPA)
            {
                CreateIPAExportOptionsPlist();
                XcodeBuild();
            }
        }

        private void CreateIPAExportOptionsPlist()
        {
            var ios = Module.UserConfig.Json.PlayerSettings.IOS;
            var eo = ios.IPAExportOptions;
            PlistDocument plist = new PlistDocument();
            plist.root = new PlistElementDict();
            plist.root.SetBoolean("compileBitcode", eo.CompileBitcode);
            plist.root.SetString("destination", eo.Destination);
            plist.root.SetString("method", eo.Method);
            var provisioningProfiles = plist.root.CreateDict("provisioningProfiles");
            provisioningProfiles.SetString(ios.BundleID, ios.ProfileID);
            plist.root.SetString("signingCertificate", eo.SigningCertificate);
            plist.root.SetString("signingStyle", eo.SigningStyle);
            plist.root.SetBoolean("stripSwiftSymbols", eo.StripSwiftSymbols);
            plist.root.SetString("teamID", ios.TeamID);
            plist.root.SetString("thinning", eo.Thinning);

            string tagsPath = Path.Combine(Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(Module.ModuleStateConfig.Json.CurrentTag));
            string ipaFolderPath = Path.Combine(tagsPath, "IPA");
            Directory.CreateDirectory(ipaFolderPath);
            plist.WriteToFile(Path.Combine(ipaFolderPath, "ExportOptions.plist"));
        }

        private void XcodeBuild()
        {
            string workPath = Path.Combine(Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(Module.ModuleStateConfig.Json.CurrentTag));
            message = errorMessage = "";
            Module.DisplayProgressBar("Start Xcode Build...", 0, true);
            using (var xcodeBuildLogWriter = new StreamWriter(Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, "XcodeBuild.log")) { AutoFlush = true })
            {
                Process p = SVNUpdate.Runner.ExcuteCommand("/bin/bash",
                    EBPUtility.Quote(Path.Combine(Module.ModuleConfig.ModuleRootPath, "Shells/XcodeBuild.sh")) + " " +
                    EBPUtility.Quote(workPath) + " " +
                    EBPUtility.Quote("Project/Unity-iPhone.xcodeproj"),
                    (object sender, DataReceivedEventArgs e) =>
                    {
                        message = e.Data;
                        xcodeBuildLogWriter.WriteLine(message);
                    },
                    OnErrorReceived, null);
                int progress = 0;
                while (!p.HasExited)
                {
                    Module.DisplayProgressBar("Xcode Building...", message, (float)(progress++ % 1000) / 1000);
                    System.Threading.Thread.Sleep(100);
                }
                if (p.ExitCode != 0)
                {
                    throw new EBPException("XcodeBuild Shell Error: " + errorMessage);
                }
            }
            Module.DisplayProgressBar("Xcode Build", "Finish!", 1, true);
        }


        // 复制并替换文件
        internal static void CopyAndReplaceDirectory(string srcPath, string dstPath)
        {
            if (Directory.Exists(dstPath))
                Directory.Delete(dstPath);
            if (File.Exists(dstPath))
                File.Delete(dstPath);

            Directory.CreateDirectory(dstPath);

            foreach (var file in Directory.GetFiles(srcPath))
                File.Copy(file, Path.Combine(dstPath, Path.GetFileName(file)));

            foreach (var dir in Directory.GetDirectories(srcPath))
                CopyAndReplaceDirectory(dir, Path.Combine(dstPath, Path.GetFileName(dir)));
        }
        #endregion
    }
}