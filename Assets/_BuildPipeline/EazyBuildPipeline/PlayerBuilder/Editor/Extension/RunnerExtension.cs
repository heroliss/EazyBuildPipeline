using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System;
using System.Linq;
using UnityEditor.XCodeEditor;
using UnityEditor.iOS.Xcode;

namespace EazyBuildPipeline.PlayerBuilder
{
    public partial class Runner
    {
        public string ConfigURL_Game;
        public string ConfigURL_Language;
        public string ConfigURL_LanguageVersion;

        protected override void PostProcess()
        {
            if (BuildPlayerOptions.target == BuildTarget.Android)
            {
                Module.DisplayProgressBar("Renaming OBB File...", 0, true);
                RenameOBBFileForAndroid();
            }
            else if (BuildPlayerOptions.target == BuildTarget.iOS)
            {
                Module.DisplayProgressBar("Postprocessing for iOS...", 0, true);
                IOSPostProcess(BuildPlayerOptions.locationPathName);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            iOSBuildPostProcessor.Disable = false; //HACK: 开启旧的后处理过程
        }

        protected override void PreProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Module.DisplayProgressBar("Applying PlayerSettings And ScriptDefines", 0.1f, true);
            ApplyPlayerSettingsAndScriptDefines();

            Module.DisplayProgressBar("Applying PostProcess Settings", 0.2f, true);
            ApplyPostProcessSettings();

            Module.DisplayProgressBar("Creating Building Configs Class File", 0.3f, true);
            CreateBuildingConfigsClassFile();

            if (CommonModule.CommonConfig.IsBatchMode)
            {
                Module.DisplayProgressBar("Start DownloadConfigs", 0.35f, true);
                DownLoadConfigs();
                DownLoadMultiLanguage();
            }

            //重新创建Wrap和Lua文件
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Module.DisplayProgressBar("Clear and Generate Wrap Files...", 0.9f, true);
            ToLuaMenu.ClearWrapFilesAndCreate();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            iOSBuildPostProcessor.Disable = true; //HACK: 关闭旧的后处理过程
        }

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
            //ezDebug.LogError("DownLoadFile Completed");
        }

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
        #region iOSPostProcess
        private void IOSPostProcess(string path)
        {
            var psIOS = Module.UserConfig.Json.PlayerSettings.IOS;

            //init
            string projPath = PBXProject.GetPBXProjectPath(path);

            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));

            string target = proj.TargetGuidByName("Unity-iPhone");

            //获得xcode工程完整目录
            string xcodePath = Path.GetFullPath(path);
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

            string plistPath = Path.Combine(xcodePath, "info.plist");
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
            //XcodeBuild (path);
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