#pragma warning disable 0649
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace EazyBuildPipeline.PlayerBuilder.Configs
{
    [Serializable]
    public class ModuleConfig : Common.Configs.ModuleConfig<ModuleConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass : Common.Configs.ModuleConfigJsonClass
        {
        }
    }

    [Serializable]
    public class ModuleStateConfig : Common.Configs.ModuleStateConfig<ModuleStateConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass : Common.Configs.ModuleStateConfigJsonClass
        {
            public bool DevelopmentBuild;
            public bool ConnectWithProfiler;
            public bool AllowDebugging;
        }
    }

    [Serializable]
    public class UserConfig : EBPConfig<UserConfig.JsonClass>, UnityEngine.ISerializationCallbackReceiver
    {
        public override void Load(string path = null)
        {
            base.Load(path);
            InitAllRepeatList();
        }
        public void InitAllRepeatList()
        {
            InitDefinesConflictList(Json.PlayerSettings.IOS.ScriptDefines);
            InitDefinesConflictList(Json.PlayerSettings.Android.ScriptDefines);
            InitDefinesConflictList(Json.PlayerSettings.General.ScriptDefines);
        }
        public static void InitDefinesConflictList(List<PlayerSettings.ScriptDefinesGroup> scriptDefines)
        {
            foreach (var group in scriptDefines)
            {
                foreach (var define in group.Defines)
                {
                    foreach (var group2 in scriptDefines)
                    {
                        foreach (var define2 in group2.Defines)
                        {
                            if (define.Define == define2.Define && define != define2)
                            {
                                define2.RepeatList.Add(define);
                            }
                        }
                    }
                }
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            InitAllRepeatList();
        }

        public bool IsDirty;
        [Serializable]
        public class JsonClass
        {
            public PlayerSettings PlayerSettings = new PlayerSettings();
            public BuildSettings BuildSettings = new BuildSettings();
        }
        [Serializable]
        public class BuildSettings
        {
            public enum CompressionMethodEnum
            {
                Default, LZ4, LZ4HC
            }
            public CompressionMethodEnum CompressionMethod;
        }
        [Serializable]
        public class PlayerSettings
        {
            public GeneralSettings General = new GeneralSettings();
            public IOSSettings IOS = new IOSSettings();
            public AndroidSettings Android = new AndroidSettings();

            [Serializable]
            public class GeneralSettings
            {
                public List<ScriptDefinesGroup> ScriptDefines = new List<ScriptDefinesGroup>();
                public string CompanyName;
                public string ProductName;
            }

            [Serializable]
            public class IOSSettings
            {
                public List<ScriptDefinesGroup> ScriptDefines = new List<ScriptDefinesGroup>();
                public string BundleID;
                public string ClientVersion;
                public string BuildNumber;
                public bool AutomaticallySign;
                public string TeamID;
                public string CameraUsageDesc;
                public string LocationUsageDesc;
                public string MicrophoneUsageDesc;
                //public string BlueToothUsageDesc; //TODO:这个是否应该归类到后处理中?
                public string TargetDevice_str;
                public iOSTargetDevice TargetDevice
                {
                    get { return TargetDevice_str.ToEnum<iOSTargetDevice>(); }
                    set { TargetDevice_str = value.ToString(); }
                }
                public string TargetSDK_str;
                public iOSSdkVersion TargetSDK
                {
                    get { return TargetSDK_str.ToEnum<iOSSdkVersion>(); }
                    set { TargetSDK_str = value.ToString(); }
                }
                public string TargetMinimumIOSVersion;
                public bool StripEngineCode;
                public string ScriptCallOptimization_str;
                public ScriptCallOptimizationLevel ScriptCallOptimization
                {
                    get { return ScriptCallOptimization_str.ToEnum<ScriptCallOptimizationLevel>(); }
                    set { ScriptCallOptimization_str = value.ToString(); }
                }
                public string ProvisioningProfile;
                public string Architecture_str;
                public enum ArchitectureEnum { ARMv7, ARM64, Universal } //TODO: 没有找到该枚举类型，所以自己创建了一个
                public ArchitectureEnum Architecture
                {
                    get { return Architecture_str.ToEnum<ArchitectureEnum>(); }
                    set { Architecture_str = value.ToString(); }
                }
                //For iOSPostprocessor
                public string ThirdFrameWorkPath;
                public string ExportIpaPath;
                public bool IsBuildArchive;
                public string BlueToothUsageDesc;
                public string PhotoUsageDesc;
                public string PhotoUsageAddDesc;
                public string TaskPath;
                //For BuglyInit
                public string BuglyAppID;
                public string BuglyAppKey;
            }

            [Serializable]
            public class AndroidSettings
            {
                public List<ScriptDefinesGroup> ScriptDefines = new List<ScriptDefinesGroup>();
                public bool PreserveFramebufferAlpha;
                public string BlitType_str;
                public AndroidBlitType BlitType
                {
                    get { return BlitType_str.ToEnum<AndroidBlitType>(); }
                    set { BlitType_str = value.ToString(); }
                }
                public bool ProtectGraphicsMemory;
                public string ClientVersion;
                public int BundleVersionCode;
                public string MinimumAPILevel_str;
                public AndroidSdkVersions MinimumAPILevel
                {
                    get { return MinimumAPILevel_str.ToEnum<AndroidSdkVersions>(); }
                    set { MinimumAPILevel_str = value.ToString(); }
                }
                public string TargetAPILevel_str;
                public AndroidSdkVersions TargetAPILevel
                {
                    get { return TargetAPILevel_str.ToEnum<AndroidSdkVersions>(); }
                    set { TargetAPILevel_str = value.ToString(); }
                }
                public string InstallLocation_str;
                public AndroidPreferredInstallLocation InstallLocation
                {
                    get { return InstallLocation_str.ToEnum<AndroidPreferredInstallLocation>(); }
                    set { InstallLocation_str = value.ToString(); }
                }
                public enum InternetAccessEnum { Auto, Require }
                public InternetAccessEnum InternetAccess
                {
                    get { return ForceInternetPermission ? InternetAccessEnum.Require : InternetAccessEnum.Auto; }
                    set { ForceInternetPermission = (value == InternetAccessEnum.Require);  }
                }
                public bool ForceInternetPermission;
                public enum WritePermissionEnum { Internal, External_SDCard }
                public WritePermissionEnum WritePermission
                {
                    get { return ForceSDCardPermission ? WritePermissionEnum.External_SDCard : WritePermissionEnum.Internal; }
                    set { ForceSDCardPermission = (value == WritePermissionEnum.External_SDCard); }
                }
                public bool ForceSDCardPermission;
                public bool AndroidTVCompatibility;
                public bool AndroidGame;
                public bool StripEngineCode;
                public string DeviceFilter_str;
                //Unity 2018 AndroidArchitecture ，2017 为 AndroidTargetDevice
                public AndroidTargetDevice DeviceFilter
                {
                    get { return DeviceFilter_str.ToEnum<AndroidTargetDevice>(); }
                    set { DeviceFilter_str = value.ToString(); }
                }

                public string PackageName;
            }

            [Serializable]
            public class ScriptDefine
            {
                public bool Active;
                public string Define;
                [NonSerialized]
                public List<ScriptDefine> RepeatList = new List<ScriptDefine>();
            }

            [Serializable]
            public class ScriptDefinesGroup
            {
                public bool Active;
                public string GroupName;
                public List<ScriptDefine> Defines = new List<ScriptDefine>();
            }
        }
    }
}