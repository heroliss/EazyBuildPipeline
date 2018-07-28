using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    [Serializable]
    public class PlayerSettingsPanel
    {
        readonly string[] toggles = { "General", "Script Defines", "iOS", "Android" };
        [SerializeField] int selectedToggle;
        [SerializeField] GUIStyle dropdownStyle;
        [SerializeField] GUIStyle buttonStyle;
        [SerializeField] GUIStyle labelStyle;
        [SerializeField] Vector2 scrollPosition_General, scrollPosition1_ScriptDefines, scrollPosition_IOS, scrollPosition_Android;

        const int defaultHeight = 22;
        GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.MaxHeight(defaultHeight), GUILayout.MaxWidth(70) };
        GUILayoutOption[] dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(defaultHeight), GUILayout.MaxWidth(80) };

        private void InitStyles()
        {
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            labelStyle = new GUIStyle(EditorStyles.label) { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };
        }

        public void Awake()
        {
            InitStyles();
            try
            {
                LoadAllConfigs();
            }
            catch (Exception e)
            {
                G.configs.DisplayDialog("加载配置文件时发生错误：" + e.Message);
            }
        }
        public void OnEnable()
        {
        }

        private void LoadAllConfigs()
        {
            //G.configs.LoadAllConfigs();
            InitSelectedIndex();
            ConfigToIndex();
        }

        private void InitSelectedIndex()
        {
            selectedToggle = 0;
        }

        public void OnDestory()
        {

        }

        public void OnGUI()
        {
            selectedToggle = GUILayout.Toolbar(selectedToggle, toggles, GUILayout.MaxHeight(30));
            switch (selectedToggle)
            {
                case 0:
                    GeneralPanel();
                    break;
                case 1:
                    ScriptDefinesPanel();
                    break;
                case 2:
                    IOSPanel();
                    break;
                case 3:
                    AndroidPanel();
                    break;
                default:
                    break;
            }
        }

        private void AndroidPanel()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition_Android))
            {
                scrollPosition_Android = scrollScope.scrollPosition;
                EditorGUILayout.LabelField("Identification", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PackageName = EditorGUILayout.TextField("Package Name", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PackageName);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ClientVersion = EditorGUILayout.TextField("Client Version", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ClientVersion);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BundleVersionCode = EditorGUILayout.IntField("Bundle Version Code", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BundleVersionCode);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.MinimumAPILevel = (AndroidSdkVersions)EditorGUILayout.EnumPopup("Minimum API Level", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.MinimumAPILevel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.TargetAPILevel = (AndroidSdkVersions)EditorGUILayout.EnumPopup("Target API Level", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.TargetAPILevel);
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.DeviceFilter = (AndroidTargetDevice)EditorGUILayout.EnumPopup("Device Filter", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.DeviceFilter);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InstallLocation = (AndroidPreferredInstallLocation)EditorGUILayout.EnumPopup("Install Location", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InstallLocation);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InternetAccess = (Configs.PlayerSettingsConfig.PlayerSettings.AndroidSettings.InternetAccessEnum)EditorGUILayout.EnumPopup("Internet Access", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InternetAccess);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.WritePermission = (Configs.PlayerSettingsConfig.PlayerSettings.AndroidSettings.WritePermissionEnum)EditorGUILayout.EnumPopup("Write Permission", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.WritePermission);
                EditorGUILayout.Separator();
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidTVCompatibility = EditorGUILayout.Toggle("Android TV Compatibility", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidTVCompatibility);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGame = EditorGUILayout.Toggle("Android Game", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGame);
                //G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGameSupprot
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.StripEngineCode = EditorGUILayout.Toggle("Strip Engine Code", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.StripEngineCode);
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PreserveFramebufferAlpha = EditorGUILayout.Toggle("Preserve Framebuffer Alpha", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PreserveFramebufferAlpha);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BlitType = (AndroidBlitType)EditorGUILayout.EnumPopup("Blit Type", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BlitType);
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ProtectGraphicsMemory = EditorGUILayout.Toggle("Protect Graphics Memory", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ProtectGraphicsMemory);
                EditorGUILayout.Separator();

            }
        }

        private void IOSPanel()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition_IOS))
            {
                scrollPosition_IOS = scrollScope.scrollPosition;
                EditorGUILayout.LabelField("Identification", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BundleID = EditorGUILayout.TextField("Bundle ID", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BundleID);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ClientVersion = EditorGUILayout.TextField("Client Version", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ClientVersion);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BuildNumber = EditorGUILayout.TextField("Build Number", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BuildNumber);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.AutomaticallySign = EditorGUILayout.Toggle("Automatically Sign", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.AutomaticallySign);
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("PostProcessing", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ProvisioningProfile = EditorGUILayout.TextField("iOS Provisioning Profile", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ProvisioningProfile);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TeamID = EditorGUILayout.TextField("Team ID", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TeamID);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BlueToothUsageDesc = EditorGUILayout.TextField("Blue Tooth Usage Desc", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BlueToothUsageDesc);
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.CameraUsageDesc = EditorGUILayout.TextField("Camera Usage Desc", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.CameraUsageDesc);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.LocationUsageDesc = EditorGUILayout.TextField("Location Usage Desc", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.LocationUsageDesc);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.MicrophoneUsageDesc = EditorGUILayout.TextField("Microphone Usage Desc", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.MicrophoneUsageDesc);
                EditorGUILayout.Separator();
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetDevice = (iOSTargetDevice)EditorGUILayout.EnumPopup("Target Device", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetDevice);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetMinimumIOSVersion = EditorGUILayout.TextField("Target minimum Version", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetMinimumIOSVersion);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.Architecture = (Configs.PlayerSettingsConfig.PlayerSettings.IOSSettings.ArchitectureEnum)EditorGUILayout.EnumPopup("Architecture", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.Architecture);
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Optimization", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.StripEngineCode = EditorGUILayout.Toggle("Strip Engine Code", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.StripEngineCode);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptCallOptimization = (ScriptCallOptimizationLevel)EditorGUILayout.EnumPopup("Script Call Optimization", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptCallOptimization);
            }
        }

        private void ScriptDefinesPanel()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition1_ScriptDefines))
            {
                scrollPosition1_ScriptDefines = scrollScope.scrollPosition;
                if (GUILayout.Button("Apply"))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(BuildTarget.iOS), "aaa;bbb,ccc,ddd eeefff");
                }
                //string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(BuildTarget.iOS));
                //Debug.Log(defines);
            }
        }

        private void GeneralPanel()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition_General))
            {
                scrollPosition_General = scrollScope.scrollPosition;
                EditorGUILayout.LabelField("General Player Settings", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.CompanyName = EditorGUILayout.TextField("Company Name", G.configs.PlayerSettingsConfig.Json.PlayerSettings.CompanyName);
                G.configs.PlayerSettingsConfig.Json.PlayerSettings.ProductName = EditorGUILayout.TextField("Product Name", G.configs.PlayerSettingsConfig.Json.PlayerSettings.ProductName);
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
                G.configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod = (Configs.PlayerSettingsConfig.BuildSettings.CompressionMethodEnum)EditorGUILayout.EnumPopup(
                    "Compression Method", G.configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod);
            }
        }

        
        private void ConfigToIndex()
        {
            
        }
    }
}
