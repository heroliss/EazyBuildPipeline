using System.Collections.Generic;
using UnityEditor;
using System;
using System.IO;
using EazyBuildPipeline.PlayerBuilder.Configs;

namespace EazyBuildPipeline.PlayerBuilder
{
    public partial class Runner : EBPRunner<Module,
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public BuildPlayerOptions BuildPlayerOptions;
        public Runner(Module module) : base(module)
        {
        }
        protected override void CheckProcess(bool onlyCheckConfig = false)
        {
            if (!onlyCheckConfig)
            {
                if (Module.ModuleStateConfig.Json.CurrentTag.Length == 0)
                {
                    throw new EBPCheckFailedException("错误：Assets Tags为空");
                }
                var target = BuildTarget.NoTarget;
                string targetStr = Module.ModuleStateConfig.Json.CurrentTag[0];
                try
                {
                    target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetStr, true);
                }
                catch
                {
                    throw new EBPCheckFailedException("没有此平台：" + targetStr);
                }

                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    throw new EBPCheckFailedException(string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target));
                }
            }
            base.CheckProcess(onlyCheckConfig);
        }

        protected override void RunProcess()
        {
            string tagsPath = Path.GetDirectoryName(BuildPlayerOptions.locationPathName);
            //重建目录
            Module.DisplayProgressBar("正在重建目录", tagsPath, 0, true);
            if (Directory.Exists(tagsPath))
            {
                Directory.Delete(tagsPath, true);
            }
            Directory.CreateDirectory(tagsPath);
            //Build Player
            Module.DisplayProgressBar("Starting BuildPlayer...", 0, true);
            var report = BuildPipeline.BuildPlayer(BuildPlayerOptions);
            if (!string.IsNullOrEmpty(report))
            {
                throw new EBPException("BuildPlayer时发生错误：" + report);
            }
        }

        public void FetchPlayerSettings()
        {
            var ps = Module.UserConfig.Json.PlayerSettings;

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
            ps.IOS.Architecture = (UserConfig.PlayerSettings.IOSSettings.ArchitectureEnum)PlayerSettings.GetArchitecture(BuildTargetGroup.iOS);
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
            ps.Android.UseObbMode = PlayerSettings.Android.useAPKExpansionFiles;
        }

        public void ApplyPlayerSettings(BuildTarget buildTarget)
        {
            var ps = Module.UserConfig.Json.PlayerSettings;
            PlayerSettings.companyName = ps.General.CompanyName;
            PlayerSettings.productName = ps.General.ProductName;
            switch (buildTarget)
            {
                case BuildTarget.iOS:
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
                    break;
                case BuildTarget.Android:
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
                    PlayerSettings.Android.useAPKExpansionFiles = ps.Android.UseObbMode;
                    PlayerSettings.Android.keystoreName = ps.Android.KeystoreName;
                    PlayerSettings.Android.keystorePass = ps.Android.KeystorePass;
                    PlayerSettings.Android.keyaliasName = ps.Android.KeyaliasName;
                    PlayerSettings.Android.keyaliasPass = ps.Android.KeyaliasPass;
                    break;
                default:
                    throw new EBPException("意外的平台：" + buildTarget.ToString());
            }
        }

        public void FetchAllScriptDefines()
        {
            var ps = Module.UserConfig.Json.PlayerSettings;
            ps.IOS.ScriptDefines.Remove(ps.IOS.ScriptDefines.Find(x => x.GroupName == "Current Script Defines"));
            ps.IOS.ScriptDefines.Add(FetchScriptDefineGroup(BuildTargetGroup.iOS));
            ps.Android.ScriptDefines.Remove(ps.Android.ScriptDefines.Find(x => x.GroupName == "Current Script Defines"));
            ps.Android.ScriptDefines.Add(FetchScriptDefineGroup(BuildTargetGroup.Android));
        }

        public UserConfig.PlayerSettings.ScriptDefinesGroup FetchScriptDefineGroup(BuildTargetGroup buildTargetGroup)
        {
            var definesGroup = new UserConfig.PlayerSettings.ScriptDefinesGroup()
            {
                Active = true,
                GroupName = "Current Script Defines",
            };
            foreach (var item in PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';'))
            {
                definesGroup.Defines.Add(new UserConfig.PlayerSettings.ScriptDefine() { Active = true, Define = item });
            }
            return definesGroup;
        }

        public void ApplyScriptDefines(BuildTarget buildTarget, bool disableTemp)
        {
            var ps = Module.UserConfig.Json.PlayerSettings;

            switch (buildTarget)
            {
                case BuildTarget.iOS:
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS,
                        GetScriptDefinesStr(ps.General.ScriptDefines, disableTemp) +
                        GetScriptDefinesStr(ps.IOS.ScriptDefines, disableTemp));
                    break;
                case BuildTarget.Android:
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android,
                        GetScriptDefinesStr(ps.General.ScriptDefines, disableTemp) +
                        GetScriptDefinesStr(ps.Android.ScriptDefines, disableTemp));
                    break;
                default:
                    throw new EBPException("意外的平台：" + buildTarget.ToString());
            }
        }

        private string GetScriptDefinesStr(List<UserConfig.PlayerSettings.ScriptDefinesGroup> scriptDefines, bool disableTemp)
        {
            string s = "";
            foreach (var definesGroup in scriptDefines)
            {
                if (definesGroup.Active)
                {
                    foreach (var define in definesGroup.Defines)
                    {
                        if (define.Active && (!disableTemp || !define.IsTemp))
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
