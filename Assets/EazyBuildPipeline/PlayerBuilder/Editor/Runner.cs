﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace EazyBuildPipeline.PlayerBuilder.Editor
{
    public class Runner : IRunner
    {
        Configs.Configs configs;
        public Runner(Configs.Configs configs)
        {
            this.configs = configs;
        }
        public bool Check()
        {
            if (configs.Common_AssetsTagsConfig.Json.Length == 0)
            {
                configs.DisplayDialog("错误：Assets Tags为空");
                return false;
            }
            //验证根目录
            if (!Directory.Exists(configs.LocalConfig.PlayersFolderPath))
            {
                configs.DisplayDialog("目录不存在：" + configs.LocalConfig.PlayersFolderPath);
                return false;
            }
            var target = BuildTarget.NoTarget;
            string targetStr = configs.Common_AssetsTagsConfig.Json[0];
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), configs.Common_AssetsTagsConfig.Json[0], true);
            }
            catch
            {
                configs.DisplayDialog("没有此平台：" + targetStr);
                return false;
            }
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                configs.DisplayDialog(string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target));
                return false;
            }
            return true;
        }

        public void Run(bool isPartOfPipeline)
        {
            //修改PlayerSettings
            EditorUtility.DisplayProgressBar("Applying PlayerSettings", "", 0);
            ApplyPlayerSettings();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            //准备BuildOptions
            EditorUtility.DisplayProgressBar("Preparing BuildOptions", "", 0);
            BuildOptions buildOptions =
                (configs.CurrentConfig.Json.DevelopmentBuild ? BuildOptions.Development : BuildOptions.None) |
                (configs.CurrentConfig.Json.ConnectWithProfiler ? BuildOptions.ConnectWithProfiler : BuildOptions.None) |
                (configs.CurrentConfig.Json.AllowDebugging ? BuildOptions.AllowDebugging : BuildOptions.None) |
                (configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod == Configs.PlayerSettingsConfig.BuildSettings.CompressionMethodEnum.LZ4 ? BuildOptions.CompressWithLz4 : BuildOptions.None) |
                (configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod == Configs.PlayerSettingsConfig.BuildSettings.CompressionMethodEnum.LZ4HC ? BuildOptions.CompressWithLz4HC : BuildOptions.None);

            string tagsPath = Path.Combine(configs.LocalConfig.PlayersFolderPath, EBPUtility.GetTagStr(configs.Common_AssetsTagsConfig.Json));
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), configs.Common_AssetsTagsConfig.Json[0], true);
            string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = tagsPath,
                target = target,
                options = buildOptions
            };

            //开始
            configs.CurrentConfig.Json.IsPartOfPipeline = isPartOfPipeline;
            configs.CurrentConfig.Json.Applying = true;
            configs.CurrentConfig.Save();
            //重建目录
            EditorUtility.DisplayProgressBar("正在重建目录", tagsPath, 0);
            if (Directory.Exists(tagsPath))
            {
                Directory.Delete(tagsPath, true);
            }
            Directory.CreateDirectory(tagsPath);
            //Build Player
            EditorUtility.DisplayProgressBar("开始BuildPlayer", "", 0);
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            if (!string.IsNullOrEmpty(report))
            {
                throw new ApplicationException("BuildPlayer时发生错误：" + report);
            }
            //结束
            configs.CurrentConfig.Json.Applying = false;
            configs.CurrentConfig.Save();
        }
        public void FetchPlayerSettings()
        {
            var ps = configs.PlayerSettingsConfig.Json.PlayerSettings;

            FetchAllScriptDefines();

            ps.General.CompanyName = PlayerSettings.companyName;
            ps.General.ProductName = PlayerSettings.productName;

            //iOS
            ps.IOS.BundleID = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS);
            ps.IOS.ClientVersion = PlayerSettings.bundleVersion;
            ps.IOS.BuildNumber = PlayerSettings.iOS.buildNumber;
            ps.IOS.AutomaticallySign = PlayerSettings.iOS.appleEnableAutomaticSigning;
            ps.IOS.ProvisioningProfile = PlayerSettings.iOS.iOSManualProvisioningProfileID;
            ps.IOS.TeamID = PlayerSettings.iOS.appleDeveloperTeamID;
            ps.IOS.CameraUsageDesc = PlayerSettings.iOS.cameraUsageDescription;
            ps.IOS.LocationUsageDesc = PlayerSettings.iOS.locationUsageDescription;
            ps.IOS.MicrophoneUsageDesc = PlayerSettings.iOS.microphoneUsageDescription;
            //TODO: 未找到 //ps.IOS.BlueToothUsageDesc = iOSBuildPostProcessor.BlueToothUsageDesc;
            ps.IOS.TargetDevice = PlayerSettings.iOS.targetDevice;
            ps.IOS.TargetSDK = PlayerSettings.iOS.sdkVersion;
            ps.IOS.TargetMinimumIOSVersion = PlayerSettings.iOS.targetOSVersionString;
            ps.IOS.Architecture = (Configs.PlayerSettingsConfig.PlayerSettings.IOSSettings.ArchitectureEnum)PlayerSettings.GetArchitecture(BuildTargetGroup.iOS);
            ps.IOS.StripEngineCode = PlayerSettings.stripEngineCode;
            ps.IOS.ScriptCallOptimization = PlayerSettings.iOS.scriptCallOptimization;

            //Android
            ps.Android.PreserveFramebufferAlpha = PlayerSettings.preserveFramebufferAlpha;
            //TODO: 未找到 Resolution Scaling Mode
            ps.Android.BlitType = PlayerSettings.Android.blitType;
            ps.Android.ProtectGraphicsMemory = PlayerSettings.protectGraphicsMemory;
            ps.Android.PackageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            ps.Android.ClientVersion = PlayerSettings.bundleVersion;
            ps.Android.BundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            ps.Android.MinimumAPILevel = PlayerSettings.Android.minSdkVersion;
            ps.Android.TargetAPILevel = PlayerSettings.Android.targetSdkVersion;
            ps.Android.DeviceFilter = PlayerSettings.Android.targetDevice;
            ps.Android.InstallLocation = PlayerSettings.Android.preferredInstallLocation;
            ps.Android.ForceInternetPermission = PlayerSettings.Android.forceInternetPermission;
            ps.Android.ForceSDCardPermission = PlayerSettings.Android.forceSDCardPermission;
            ps.Android.AndroidTVCompatibility = PlayerSettings.Android.androidTVCompatibility;
            ps.Android.AndroidGame = PlayerSettings.Android.androidIsGame;
            //TODO: 未找到 16.	Android GamePad Support
            ps.Android.StripEngineCode = PlayerSettings.stripEngineCode;
        }

        public void ApplyIOSPostProcessSettings()
        {
            var ps = configs.PlayerSettingsConfig.Json.PlayerSettings;

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
            BuglyInit.BuglyAppIDForIOS = ps.IOS.BuglyAppID;
            BuglyInit.BuglyAppKeyForIOS = ps.IOS.BuglyAppKey;
        }

        public void ApplyPlayerSettings()
        {
            var ps = configs.PlayerSettingsConfig.Json.PlayerSettings;
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
            ApplyScriptDefines(buildTargetGroup);

            PlayerSettings.companyName = ps.General.CompanyName;
            PlayerSettings.productName = ps.General.ProductName;
            switch (buildTargetGroup)
            {
                case BuildTargetGroup.iOS:
                    //Identity
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, ps.IOS.BundleID);
                    PlayerSettings.bundleVersion = ps.IOS.ClientVersion;
                    PlayerSettings.iOS.buildNumber = ps.IOS.BuildNumber;
                    PlayerSettings.iOS.appleEnableAutomaticSigning = ps.IOS.AutomaticallySign;
                    PlayerSettings.iOS.iOSManualProvisioningProfileID = ps.IOS.ProvisioningProfile;
                    PlayerSettings.iOS.appleDeveloperTeamID = ps.IOS.TeamID;
                    PlayerSettings.iOS.cameraUsageDescription = ps.IOS.CameraUsageDesc;
                    PlayerSettings.iOS.locationUsageDescription = ps.IOS.LocationUsageDesc;
                    PlayerSettings.iOS.microphoneUsageDescription = ps.IOS.MicrophoneUsageDesc;
                    PlayerSettings.iOS.targetDevice = ps.IOS.TargetDevice;
                    PlayerSettings.iOS.sdkVersion = ps.IOS.TargetSDK;
                    PlayerSettings.iOS.targetOSVersionString = ps.IOS.TargetMinimumIOSVersion;
                    PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, (int)ps.IOS.Architecture);
                    PlayerSettings.stripEngineCode = ps.IOS.StripEngineCode;
                    PlayerSettings.iOS.scriptCallOptimization = ps.IOS.ScriptCallOptimization;
                    ApplyIOSPostProcessSettings();
                    break;
                case BuildTargetGroup.Android:
                    PlayerSettings.preserveFramebufferAlpha = ps.Android.PreserveFramebufferAlpha;
                    //TODO：未找到：Resolution Scaling Mode
                    PlayerSettings.Android.blitType = ps.Android.BlitType;
                    PlayerSettings.protectGraphicsMemory = ps.Android.ProtectGraphicsMemory;
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, ps.Android.PackageName);
                    PlayerSettings.bundleVersion = ps.Android.ClientVersion;
                    PlayerSettings.Android.bundleVersionCode = ps.Android.BundleVersionCode;
                    PlayerSettings.Android.minSdkVersion = ps.Android.MinimumAPILevel;
                    PlayerSettings.Android.targetSdkVersion = ps.Android.TargetAPILevel;
                    PlayerSettings.Android.targetDevice = ps.Android.DeviceFilter;
                    PlayerSettings.Android.preferredInstallLocation = ps.Android.InstallLocation;
                    PlayerSettings.Android.forceInternetPermission = ps.Android.ForceInternetPermission;
                    PlayerSettings.Android.forceSDCardPermission = ps.Android.ForceSDCardPermission;
                    PlayerSettings.Android.androidTVCompatibility = ps.Android.AndroidTVCompatibility;
                    PlayerSettings.Android.androidIsGame = ps.Android.AndroidGame;
                    //TODO：未找到：16.	Android GamePad Support
                    PlayerSettings.stripEngineCode = ps.Android.StripEngineCode;
                    break;
                default:
                    break;
            }
        }

        public void FetchAllScriptDefines()
        {
            var ps = configs.PlayerSettingsConfig.Json.PlayerSettings;

            ps.IOS.ScriptDefines.Add(FetchScriptDefineGroup(BuildTargetGroup.iOS));
            ps.Android.ScriptDefines.Add(FetchScriptDefineGroup(BuildTargetGroup.Android));
        }

        public Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup FetchScriptDefineGroup(BuildTargetGroup buildTargetGroup)
        {
            var definesGroup = new Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup()
            {
                Active = true,
                GroupName = "Current Script Defines",
            };
            foreach (var item in PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';'))
            {
                definesGroup.Defines.Add(new Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefine() { Active = true, Define = item });
            }
            return definesGroup;
        }

        public void ApplyScriptDefines(BuildTargetGroup buildTargetGroup)
        {
            var ps = configs.PlayerSettingsConfig.Json.PlayerSettings;

            switch (buildTargetGroup)
            {
                case BuildTargetGroup.iOS:
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS,
                        GetScriptDefinesStr(ps.General.ScriptDefines) +
                        GetScriptDefinesStr(ps.IOS.ScriptDefines));
                    break;
                case BuildTargetGroup.Android:
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android,
                        GetScriptDefinesStr(ps.General.ScriptDefines) +
                        GetScriptDefinesStr(ps.Android.ScriptDefines));
                    break;
                default:
                    break;
            }
        }

        private string GetScriptDefinesStr(List<Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup> scriptDefines)
        {
            string s = "";
            foreach (var definesGroup in scriptDefines)
            {
                if (definesGroup.Active)
                {
                    foreach (var define in definesGroup.Defines)
                    {
                        if (define.Active)
                        {
                            s += define.Define + ";";
                        }
                    }
                }
            }
            return s;
        }
    }
}
