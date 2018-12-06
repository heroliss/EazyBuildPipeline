#pragma warning disable 0649
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace EazyBuildPipeline.PlayerBuilder.Configs
{
    [Serializable]
    public class ModuleConfig : Common.Configs.ModuleConfig<ModuleConfig.JsonClass>
    {
        public override string UserConfigsFolderPath { get { return CommonModule.CommonConfig.UserConfigsFolderPath_PlayerBuilder; } }

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

            public int ResourceVersion;
            public string ClientVersion;
            public int BuildNumber;
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
                //Extension
                [JsonConverter(typeof(StringEnumConverter))] public EazyGameChannel.Channels Channel;
                public string ConfigURL_Game, ConfigURL_Language, ConfigURL_LanguageVersion;
                //For BuglyInit
                public string BuglyAppID;
                public string BuglyAppKey;
            }

            [Serializable]
            public class IOSSettings
            {
                public enum ArchitectureEnum { ARMv7, ARM64, Universal } //TODO: 没有找到该枚举类型，所以自己创建了一个
                public List<ScriptDefinesGroup> ScriptDefines = new List<ScriptDefinesGroup>();
                public string CopyDirectoryRegex, CopyFileRegex;
                public List<CopyItem> CopyList = new List<CopyItem>();
                public string BundleID;
                public bool AutomaticallySign;
                public string ProvisioningProfile;
                public string TeamID;
                public string CameraUsageDesc;
                public string LocationUsageDesc;
                public string MicrophoneUsageDesc;
                //public string BlueToothUsageDesc; //TODO:这个是否应该归类到后处理中?
                [JsonConverter(typeof(StringEnumConverter))] public iOSTargetDevice TargetDevice;
                [JsonConverter(typeof(StringEnumConverter))] public iOSSdkVersion TargetSDK;
                public string TargetMinimumIOSVersion;
                public bool StripEngineCode;
                [JsonConverter(typeof(StringEnumConverter))] public ScriptCallOptimizationLevel ScriptCallOptimization;
                [JsonConverter(typeof(StringEnumConverter))] public ArchitectureEnum Architecture;

                //For iOSPostprocessor
                public string ThirdFrameWorkPath;
                public string BlueToothUsageDesc;
                public string PhotoUsageDesc;
                public string PhotoUsageAddDesc;
                //public string ExportIpaPath;
                //public bool IsBuildArchive;
                //public string TaskPath;
            }

            [Serializable]
            public class AndroidSettings
            {
                public enum InternetAccessEnum { Auto, Require }
                public enum WritePermissionEnum { Internal, External_SDCard }

                public List<ScriptDefinesGroup> ScriptDefines = new List<ScriptDefinesGroup>();
                public string CopyDirectoryRegex, CopyFileRegex;
                public List<CopyItem> CopyList = new List<CopyItem>();
                public bool PreserveFramebufferAlpha;
                [JsonConverter(typeof(StringEnumConverter))] public AndroidBlitType BlitType;
                public bool ProtectGraphicsMemory;
                [JsonConverter(typeof(StringEnumConverter))] public AndroidSdkVersions MinimumAPILevel;
                [JsonConverter(typeof(StringEnumConverter))] public AndroidSdkVersions TargetAPILevel;
                [JsonConverter(typeof(StringEnumConverter))] public AndroidPreferredInstallLocation InstallLocation;
                [JsonIgnore]
                public InternetAccessEnum InternetAccess
                {
                    get { return ForceInternetPermission ? InternetAccessEnum.Require : InternetAccessEnum.Auto; }
                    set { ForceInternetPermission = (value == InternetAccessEnum.Require);  }
                }
                public bool ForceInternetPermission;
                [JsonIgnore]
                public WritePermissionEnum WritePermission
                {
                    get { return ForceSDCardPermission ? WritePermissionEnum.External_SDCard : WritePermissionEnum.Internal; }
                    set { ForceSDCardPermission = (value == WritePermissionEnum.External_SDCard); }
                }
                public bool ForceSDCardPermission;
                public bool AndroidTVCompatibility;
                public bool AndroidGame;
                public bool StripEngineCode;
                //Unity 2018 AndroidArchitecture ，2017 为 AndroidTargetDevice
                [JsonConverter(typeof(StringEnumConverter))] public AndroidTargetDevice DeviceFilter;
                public string PackageName;
                public bool UseObbMode;
                public string KeystoreName;
                public string KeystorePass;
                public string KeyaliasName;
                public string KeyaliasPass;
            }

            [Serializable]
            public class ScriptDefine
            {
                public bool Active;
                public string Define;
                public bool IsTemp;
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

            [Serializable]
            public class CopyItem
            {
                public string SourcePath;
                [JsonConverter(typeof(StringEnumConverter))] public CopyMode CopyMode;
                public string TargetPath;
            }
        }
    }
}