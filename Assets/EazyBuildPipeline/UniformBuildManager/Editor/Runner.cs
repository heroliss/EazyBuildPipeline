using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    public class Runner
    {
        public Configs.Configs configs;
        public Runner(Configs.Configs configs)
        {
            this.configs = configs;
        }
        public bool Check()
        {        
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
                EditorUtility.DisplayDialog("Build Bundles", "没有此平台：" + targetStr, "确定");
                return false;
            }
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                EditorUtility.DisplayDialog("Build Bundles", string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target), "确定");
                return false;
            }
            return true;
        }

        public void Apply(bool isPartOfPipeline)
        {
            //准备BuildOptions
            BuildOptions buildOptions =
                (configs.CurrentConfig.Json.DevelopmentBuild ? BuildOptions.Development : BuildOptions.None) |
                (configs.CurrentConfig.Json.ConnectWithProfiler ? BuildOptions.ConnectWithProfiler : BuildOptions.None) |
                (configs.CurrentConfig.Json.AllowDebugging ? BuildOptions.AllowDebugging : BuildOptions.None) |
                (configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod == Configs.PlayerSettingsConfig.BuildSettings.CompressionMethodEnum.LZ4 ? BuildOptions.CompressWithLz4 : BuildOptions.None) |
                (configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod == Configs.PlayerSettingsConfig.BuildSettings.CompressionMethodEnum.LZ4HC ? BuildOptions.CompressWithLz4HC : BuildOptions.None);
            //修改PlayerSettings
            ApplyPlayerSettings();
            //开始
            configs.CurrentConfig.Json.IsPartOfPipeline = isPartOfPipeline;
            configs.CurrentConfig.Json.Applying = true;
            configs.CurrentConfig.Save();
            //重建目录
            string tagsPath = Path.Combine(configs.LocalConfig.PlayersFolderPath, EBPUtility.GetTagStr(configs.Common_AssetsTagsConfig.Json));
            if (Directory.Exists(tagsPath))
            {
                Directory.Delete(tagsPath, true);
            }
            Directory.CreateDirectory(tagsPath);
            //Build Player
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), configs.Common_AssetsTagsConfig.Json[0], true);
            string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = tagsPath;
            buildPlayerOptions.target = target;
            buildPlayerOptions.options = buildOptions;
            string error = BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log(error);

            //结束
            configs.CurrentConfig.Json.Applying = false;
            configs.CurrentConfig.Save();
        }

        public void ApplyPlayerSettings()
        {
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);

            PlayerSettings.companyName = configs.PlayerSettingsConfig.Json.PlayerSettings.CompanyName;
            PlayerSettings.productName = configs.PlayerSettingsConfig.Json.PlayerSettings.ProductName;
            switch (activeBuildTarget)
            {
                case BuildTarget.iOS:
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BundleID);
                    PlayerSettings.bundleVersion = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ClientVersion;
                    PlayerSettings.iOS.buildNumber = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BuildNumber;
                    PlayerSettings.iOS.appleEnableAutomaticSigning = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.AutomaticallySign;
                    iOSBuildPostProcessor.ProvisioningProfile = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ProvisioningProfile;
                    iOSBuildPostProcessor.TeamID = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TeamID;                    
                    PlayerSettings.iOS.cameraUsageDescription = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.CameraUsageDesc;
                    PlayerSettings.iOS.locationUsageDescription = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.LocationUsageDesc;
                    PlayerSettings.iOS.microphoneUsageDescription = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.MicrophoneUsageDesc;
                    iOSBuildPostProcessor.BlueToothUsageDesc = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BlueToothUsageDesc;
                    PlayerSettings.iOS.targetDevice = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetDevice;
                    PlayerSettings.iOS.sdkVersion = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetSDK;
                    PlayerSettings.iOS.targetOSVersionString = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetMinimumIOSVersion;
                    PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, (int)configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.Architecture);
                    PlayerSettings.stripEngineCode = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.StripEngineCode;
                    PlayerSettings.iOS.scriptCallOptimization = configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptCallOptimization;
                    break;
                case BuildTarget.Android:
                    PlayerSettings.preserveFramebufferAlpha = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PreserveFramebufferAlpha;
                    //Resolution Scaling Mode
                    //PlayerSettings.
                    PlayerSettings.Android.blitType = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BlitType;
                    PlayerSettings.protectGraphicsMemory = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ProtectGraphicsMemory;
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PackageName);
                    PlayerSettings.bundleVersion = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ClientVersion;
                    PlayerSettings.Android.bundleVersionCode = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BundleVersionCode;
                    PlayerSettings.Android.minSdkVersion = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.MinimumAPILevel;
                    PlayerSettings.Android.targetSdkVersion = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.TargetAPILevel;
                    PlayerSettings.Android.targetDevice = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.DeviceFilter;
                    PlayerSettings.Android.preferredInstallLocation = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InstallLocation;
                    PlayerSettings.Android.forceInternetPermission = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ForceInternetPermission;
                    PlayerSettings.Android.forceSDCardPermission = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ForceSDCardPermission;
                    PlayerSettings.Android.androidTVCompatibility = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidTVCompatibility;
                    PlayerSettings.Android.androidIsGame = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGame;
                    //16.	Android GamePad Support
                    //PlayerSettings.Android.
                    PlayerSettings.stripEngineCode = configs.PlayerSettingsConfig.Json.PlayerSettings.Android.StripEngineCode;
                    break;
                case BuildTarget.NoTarget:
                    break;
                default:
                    break;
            }
        }
    }
}
