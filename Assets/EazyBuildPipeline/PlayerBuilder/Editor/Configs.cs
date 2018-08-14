#pragma warning disable 0649
using EazyBuildPipeline.Common.Editor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace EazyBuildPipeline.PlayerBuilder.Editor
{
    public static class G
    {
        public static Configs.Configs configs;
        public static GlobalReference g;
        public class GlobalReference
        {
        }

        public static void Init()
        {
            configs = new Configs.Configs();
            g = new GlobalReference();
        }
        
        public static void Clear()
        {
            configs = null;
            g = null;
        }
    }
}
namespace EazyBuildPipeline.PlayerBuilder.Editor.Configs
{
    [Serializable]
    public class Configs : EBPConfigs
    {
        public override string ModuleName { get { return "PlayerBuilder"; } }
        private readonly string localConfigSearchText = "EazyBuildPipeline PlayerBuilder LocalConfig";
        public Runner Runner;
        public LocalConfig LocalConfig = new LocalConfig();
        public CurrentConfig CurrentConfig = new CurrentConfig();
        public PlayerSettingsConfig PlayerSettingsConfig = new PlayerSettingsConfig();
        
        public Configs()
        {
            Runner = new Runner(this);
        }

        public bool LoadAllConfigs(string rootPath = null)
        {
            if (!LoadCommonLocalConfig()) return false;
            if (!LoadCommonTagEnumConfig()) return false;
            if (!LoadCommonAssetsTagsConfig()) return false;

            if (!LoadLocalConfig(rootPath)) return false;
 
            bool success = true;
            success &= LoadCurrentConfig();
            success &= LoadCurrentPlayerSetting();
            return success;
        }

        public bool LoadLocalConfig(string rootPath = null)
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets(localConfigSearchText);
                if (guids.Length == 0)
                {
                    throw new ApplicationException("未能找到本地配置文件! 搜索文本：" + localConfigSearchText);
                }
                LocalConfig.JsonPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                LocalConfig.LocalRootPath = Path.GetDirectoryName(LocalConfig.JsonPath);
                LocalConfig.Load();
                if (rootPath != null)
                {
                    LocalConfig.Json.RootPath = rootPath;
                }
            }
            catch (Exception e)
            {
                DisplayDialog("加载本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + LocalConfig.JsonPath
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + new LocalConfig().ToString());
                return false;
            }
            return true;
        }

        public bool LoadCurrentConfig()
        {
            try
            {
                if (Directory.Exists(LocalConfig.Json.RootPath))
                {
                    CurrentConfig.JsonPath = LocalConfig.PlayersConfigPath;
                    if (Directory.Exists(Path.GetDirectoryName(CurrentConfig.JsonPath)))
                    {
                        if (!File.Exists(CurrentConfig.JsonPath))
                        {
                            File.Create(CurrentConfig.JsonPath).Close();
                            CurrentConfig.Save();
                        }
                        CurrentConfig.Load();
                        return true;
                    }
                    else
                    {
                        DisplayDialog("不是有效的Pipeline根目录:" + LocalConfig.Json.RootPath +
                           "\n\n若要新建一个此工具可用的Pipeline根目录，确保存在如下目录即可：" + Path.GetDirectoryName(CurrentConfig.JsonPath));
                        return false;
                    }
                }
                else
                {
                    DisplayDialog("根目录不存在:" + LocalConfig.Json.RootPath);
                    return false;
                }
            }
            catch (Exception e)
            {
                DisplayDialog("加载当前配置时发生错误：" + e.Message);
                return false;
            }
        }

        public bool LoadCurrentPlayerSetting()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentConfig.Json.CurrentPlayerSettingName))
                {
                    string currentBuildSettingPath = Path.Combine(LocalConfig.Local_PlayerSettingsFolderPath, CurrentConfig.Json.CurrentPlayerSettingName);
                    PlayerSettingsConfig.JsonPath = currentBuildSettingPath;
                    PlayerSettingsConfig.Load();
                    return true;
                }
                else
                {
                    CurrentConfig.Json.CurrentPlayerSettingName = null;
                    PlayerSettingsConfig.JsonPath = null;
                    return false;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "载入BuildSetting文件：" + CurrentConfig.Json.CurrentPlayerSettingName + " 时发生错误：" + e.Message, "确定");
                CurrentConfig.Json.CurrentPlayerSettingName = null;
                PlayerSettingsConfig.JsonPath = null;
                return false;
            }
        }
    }
    [Serializable]
    public class LocalConfig : EBPConfig<LocalConfig.JsonClass>
    {
        public LocalConfig()
        {
            Json = new JsonClass();
        }
        //本地配置路径
        public string LocalRootPath;
        public string Local_PlayerSettingsFolderPath { get { return Path.Combine(LocalRootPath, Json.Local_PlayerSettingsFolderRelativePath); } }
        //Pipeline配置路径
        public string PlayersFolderPath { get { return Path.Combine(Json.RootPath, Json.PlayersFolderRelativePath); } }
        public string PlayersConfigPath { get { return Path.Combine(Json.RootPath, Json.PlayersConfigRelativePath); } }
        [Serializable]
        public class JsonClass
        {
            public string Local_PlayerSettingsFolderRelativePath;
            public string RootPath;
            public string PlayersFolderRelativePath;
            public string PlayersConfigRelativePath;
        }
    }

    [Serializable]
    public class CurrentConfig : EBPConfig<CurrentConfig.JsonClass>
    {
        public CurrentConfig()
        {
            Json = new JsonClass();
        }
        [Serializable]
        public class JsonClass
        {
            public string[] CurrentTags = new string[0];
            public string CurrentPlayerSettingName;
            public bool Applying;
            public bool IsPartOfPipeline;
            public bool DevelopmentBuild;
            public bool ConnectWithProfiler;
            public bool AllowDebugging;
        }
    }

    [Serializable]
    public class PlayerSettingsConfig : EBPConfig<PlayerSettingsConfig.JsonClass>, UnityEngine.ISerializationCallbackReceiver
    {
        public PlayerSettingsConfig()
        {
            Json = new JsonClass();
        }
        public override void Load(string path = null)
        {
            base.Load(path);
            InitAllRepeatList();
        }
        public void InitAllRepeatList()
        {
            InitRepeatList(Json.PlayerSettings.IOS.ScriptDefines);
            InitRepeatList(Json.PlayerSettings.Android.ScriptDefines);
            InitRepeatList(Json.PlayerSettings.General.ScriptDefines);
        }
        public static void InitRepeatList(List<PlayerSettings.ScriptDefinesGroup> scriptDefines)
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

        public bool Dirty;
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
                public string BlueToothUsageDesc;
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
                    set { ForceInternetPermission = (value == InternetAccessEnum.Auto) ? false : true;  }
                }
                public bool ForceInternetPermission;
                public enum WritePermissionEnum { Internal, External_SDCard }
                public WritePermissionEnum WritePermission
                {
                    get { return ForceSDCardPermission ? WritePermissionEnum.External_SDCard : WritePermissionEnum.Internal; }
                    set { ForceSDCardPermission = (value == WritePermissionEnum.Internal) ? false : true; }
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