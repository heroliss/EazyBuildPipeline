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
        readonly string[] platformToggles = { "General", "iOS", "Android" };
        [SerializeField] int selectedToggle;
        [SerializeField] int selectedPlatformToggle;
        [SerializeField] GUIStyle scriptDefineTextStyle;
        [SerializeField] GUIStyle scriptDefineTextStyle_red;
        [SerializeField] GUIStyle toggleStyle;
        [SerializeField] GUIStyle dropdownStyle;
        [SerializeField] GUIStyle buttonStyle;
        [SerializeField] GUIStyle labelStyle;
        [SerializeField] Vector2 scrollPosition_General,
            scrollPosition1_ScriptDefines_General,
            scrollPosition1_ScriptDefines_IOS,
            scrollPosition1_ScriptDefines_Android, 
            scrollPosition_IOS, 
            scrollPosition_Android;

        const int defaultHeight = 22;
        GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.MaxHeight(defaultHeight), GUILayout.MaxWidth(70) };
        GUILayoutOption[] dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(defaultHeight), GUILayout.MaxWidth(80) };

        private void InitStyles()
        {
            scriptDefineTextStyle = new GUIStyle("RectangleToolVBar");
            scriptDefineTextStyle_red = new GUIStyle("RectangleToolVBar");
            scriptDefineTextStyle_red.normal.textColor = Color.red;
            toggleStyle = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 0, wordWrap = true };
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            labelStyle = new GUIStyle(EditorStyles.label) { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft, wordWrap = true };
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
        }

        public void OnDestory()
        {

        }

        public void OnGUI()
        {
            selectedToggle = GUILayout.Toolbar(selectedToggle, toggles, toggleStyle, GUILayout.Height(30));
            switch (toggles[selectedToggle])
            {
                case "General":
                    GeneralPanel();
                    break;
                case "Script Defines":
                    ScriptDefinesPanel();
                    break;
                case "iOS":
                    IOSPanel();
                    break;
                case "Android":
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
                string s = EditorGUILayout.TextField("Package Name", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PackageName);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PackageName != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PackageName = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Client Version", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ClientVersion);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ClientVersion != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ClientVersion = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                int n = EditorGUILayout.IntField("Bundle Version Code", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BundleVersionCode);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BundleVersionCode != n)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BundleVersionCode = n;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                var e = (AndroidSdkVersions)EditorGUILayout.EnumPopup("Minimum API Level", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.MinimumAPILevel);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.MinimumAPILevel != e)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.MinimumAPILevel = e;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                e = (AndroidSdkVersions)EditorGUILayout.EnumPopup("Target API Level", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.TargetAPILevel);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.TargetAPILevel != e)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.TargetAPILevel = e;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
                var e2 = (AndroidTargetDevice)EditorGUILayout.EnumPopup("Device Filter", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.DeviceFilter);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.DeviceFilter != e2)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.DeviceFilter = e2;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                var e3 = (AndroidPreferredInstallLocation)EditorGUILayout.EnumPopup("Install Location", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InstallLocation);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InstallLocation != e3)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InstallLocation = e3;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                var e4 = (Configs.PlayerSettingsConfig.PlayerSettings.AndroidSettings.InternetAccessEnum)EditorGUILayout.EnumPopup("Internet Access", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InternetAccess);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InternetAccess != e4)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InternetAccess = e4;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                var e5 = (Configs.PlayerSettingsConfig.PlayerSettings.AndroidSettings.WritePermissionEnum)EditorGUILayout.EnumPopup("Write Permission", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.WritePermission);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.WritePermission != e5)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.WritePermission = e5;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                EditorGUILayout.Separator();
                bool b = EditorGUILayout.Toggle("Android TV Compatibility", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidTVCompatibility);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidTVCompatibility != b)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidTVCompatibility = b;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                b = EditorGUILayout.Toggle("Android Game", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGame);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGame != b)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGame = b;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                //G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGameSupprot
                b = EditorGUILayout.Toggle("Strip Engine Code", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.StripEngineCode);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.StripEngineCode != b)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.StripEngineCode = b;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
                b = EditorGUILayout.Toggle("Preserve Framebuffer Alpha", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PreserveFramebufferAlpha);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PreserveFramebufferAlpha != b)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PreserveFramebufferAlpha = b;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                var e6 = (AndroidBlitType)EditorGUILayout.EnumPopup("Blit Type", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BlitType);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BlitType != e6)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BlitType = e6;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
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
                string s = "";
                EditorGUILayout.LabelField("Identification", EditorStyles.boldLabel);
                s = EditorGUILayout.TextField("Bundle ID", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BundleID);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BundleID != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BundleID = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Client Version", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ClientVersion);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ClientVersion != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ClientVersion = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Build Number", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BuildNumber);
                if (G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BuildNumber != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BuildNumber = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                bool b = EditorGUILayout.Toggle("Automatically Sign", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.AutomaticallySign);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.AutomaticallySign != b)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.AutomaticallySign = b;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("PostProcessing", EditorStyles.boldLabel);
                s = EditorGUILayout.TextField("iOS Provisioning Profile", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ProvisioningProfile);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ProvisioningProfile != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ProvisioningProfile = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Team ID", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TeamID);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TeamID != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TeamID = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Blue Tooth Usage Desc", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BlueToothUsageDesc);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BlueToothUsageDesc != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BlueToothUsageDesc = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
                s = EditorGUILayout.TextField("Camera Usage Desc", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.CameraUsageDesc);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.CameraUsageDesc != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.CameraUsageDesc = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Location Usage Desc", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.LocationUsageDesc);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.LocationUsageDesc != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.LocationUsageDesc = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Microphone Usage Desc", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.MicrophoneUsageDesc);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.MicrophoneUsageDesc != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.MicrophoneUsageDesc = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                EditorGUILayout.Separator();
                var e = (iOSTargetDevice)EditorGUILayout.EnumPopup("Target Device", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetDevice);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetDevice != e)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetDevice = e;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Target minimum Version", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetMinimumIOSVersion);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetMinimumIOSVersion != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetMinimumIOSVersion = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                var e2 = (Configs.PlayerSettingsConfig.PlayerSettings.IOSSettings.ArchitectureEnum)EditorGUILayout.EnumPopup("Architecture", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.Architecture);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.Architecture != e2)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.Architecture = e2;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Optimization", EditorStyles.boldLabel);
                b = EditorGUILayout.Toggle("Strip Engine Code", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.StripEngineCode);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.StripEngineCode != b)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.StripEngineCode = b;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                var e3 = (ScriptCallOptimizationLevel)EditorGUILayout.EnumPopup("Script Call Optimization", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptCallOptimization);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptCallOptimization != e3)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptCallOptimization = e3;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
            }
        }

        private void ScriptDefinesPanel()
        {
            EditorGUILayout.BeginHorizontal();
            selectedPlatformToggle = GUILayout.Toolbar(selectedPlatformToggle, platformToggles, GUILayout.Height(30), GUILayout.MaxWidth(300));
            string platform = platformToggles[selectedPlatformToggle];
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Group", GUILayout.Height(30)))
            {
                switch (platform)
                {
                    case "General":
                        G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ScriptDefines.Add(new Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup() { GroupName = "Script Defines Group", Active = true });
                        break;
                    case "iOS":
                        G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptDefines.Add(new Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup() { GroupName = "Script Defines Group", Active = true });
                        break;
                    case "Android":
                        G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ScriptDefines.Add(new Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup() { GroupName = "Script Defines Group", Active = true });
                        break;
                }
                G.configs.PlayerSettingsConfig.Dirty = true;
            }
            if (GUILayout.Button("Apply All Defines", GUILayout.Height(30)))
            {
                G.configs.Runner.ApplyScriptDefines(BuildTargetGroup.iOS);
                G.configs.Runner.ApplyScriptDefines(BuildTargetGroup.Android);
            }
            EditorGUILayout.EndHorizontal();

            switch (platform)
            {
                case "General":
                    using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition1_ScriptDefines_General))
                    {
                        scrollPosition1_ScriptDefines_General = scrollScope.scrollPosition;
                        ScriptDefineGroupPanel(G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ScriptDefines);
                    }
                    break;
                case "iOS":
                    using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition1_ScriptDefines_IOS))
                    {
                        scrollPosition1_ScriptDefines_IOS = scrollScope.scrollPosition;
                        ScriptDefineGroupPanel(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptDefines);
                    }
                    break;
                case "Android":
                    using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition1_ScriptDefines_Android))
                    {
                        scrollPosition1_ScriptDefines_Android = scrollScope.scrollPosition;
                        ScriptDefineGroupPanel(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ScriptDefines);
                    }
                    break;
                default:
                    break;
            }
        }

        private void GeneralPanel()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPosition_General))
            {
                scrollPosition_General = scrollScope.scrollPosition;
                EditorGUILayout.LabelField("General Player Settings", EditorStyles.boldLabel);
                string s = EditorGUILayout.TextField("Company Name", G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.CompanyName);
                if (G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.CompanyName != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.CompanyName = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                s = EditorGUILayout.TextField("Product Name", G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ProductName);
                if(G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ProductName != s)
                {
                    G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ProductName = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
                var e = (Configs.PlayerSettingsConfig.BuildSettings.CompressionMethodEnum)EditorGUILayout.EnumPopup(
                    "Compression Method", G.configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod);
                if(G.configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod != e)
                {
                    G.configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod = e;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                //以下为当前配置，不保存在配置文件中
                EditorGUILayout.Separator();
                G.configs.CurrentConfig.Json.DevelopmentBuild = EditorGUILayout.Toggle("Development Build", G.configs.CurrentConfig.Json.DevelopmentBuild);
                G.configs.CurrentConfig.Json.AllowDebugging = EditorGUILayout.Toggle("Script Debugging", G.configs.CurrentConfig.Json.AllowDebugging);
                G.configs.CurrentConfig.Json.ConnectWithProfiler = EditorGUILayout.Toggle("AutoConnect Profiler", G.configs.CurrentConfig.Json.ConnectWithProfiler);
            }
        }

        private void ScriptDefineGroupPanel(List<Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup> scriptDefinesGroupList)
        {
            for (int i = 0; i < scriptDefinesGroupList.Count; i++)
            {
                var group = scriptDefinesGroupList[i];

                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.BeginHorizontal();
                bool b = GUILayout.Toggle(group.Active, GUIContent.none);
                if(group.Active != b)
                {
                    group.Active = b;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                string s = GUILayout.TextField(group.GroupName, "flow overlay header upper left", GUILayout.MinWidth(100), GUILayout.MaxWidth(2000));
                if(group.GroupName != s)
                {
                    group.GroupName = s;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+"))
                {
                    if (!group.Defines.Exists(x => x.Define == ""))
                    {
                        group.Defines.Add(new Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefine() { Active = true, Define = "" });
                        G.configs.PlayerSettingsConfig.Dirty = true;
                        return;
                    }
                }
                if (GUILayout.Button("-"))
                {
                    bool ensure = true;
                    if (group.Defines.Count > 0)
                    {
                        ensure = EditorUtility.DisplayDialog(G.configs.ModuleName, "确定要删除宏定义组“" + group.GroupName + "”及其所有内容？", "确定", "取消");
                    }
                    if (ensure)
                    {
                        scriptDefinesGroupList.RemoveAt(i);
                        G.configs.PlayerSettingsConfig.Dirty = true;
                        return;
                    }
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("▲") && i > 0)
                {
                    var t = scriptDefinesGroupList[i];
                    scriptDefinesGroupList[i] = scriptDefinesGroupList[i - 1];
                    scriptDefinesGroupList[i - 1] = t;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                    return;
                }
                if (GUILayout.Button("▼") && i < scriptDefinesGroupList.Count - 1)
                {
                    var t = scriptDefinesGroupList[i];
                    scriptDefinesGroupList[i] = scriptDefinesGroupList[i + 1];
                    scriptDefinesGroupList[i + 1] = t;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                    return;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                for (int j = 0; j < group.Defines.Count; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    var define = group.Defines[j];
                    b = GUILayout.Toggle(define.Active, GUIContent.none, "OL Toggle");
                    if (define.Active != b)
                    {
                        define.Active = b;
                        G.configs.PlayerSettingsConfig.Dirty = true;
                    }
                    s = GUILayout.TextField(define.Define, define.RepeatList.Count > 0 ? scriptDefineTextStyle_red : scriptDefineTextStyle ,GUILayout.MinWidth(100), GUILayout.MaxWidth(2000)).Trim();
                    if(define.Define != s)
                    {
                        foreach (var g in scriptDefinesGroupList)
                        {
                            foreach (var d in g.Defines)
                            {
                                if (d.Define == s)
                                {
                                    d.RepeatList.Add(define);
                                    define.RepeatList.Add(d);
                                }
                                else
                                {
                                    d.RepeatList.Remove(define);
                                    define.RepeatList.Remove(d);
                                }
                            }
                        }
                        define.Define = s;
                        G.configs.PlayerSettingsConfig.Dirty = true;
       
                    }
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("-"))
                    {
                        bool ensure = true;
                        if (define.Define != "")
                        {
                            ensure = EditorUtility.DisplayDialog(G.configs.ModuleName, "确定要删除宏定义“" + define.Define + "”?", "确定", "取消");
                        }
                        if (ensure)
                        {
                            group.Defines.RemoveAt(j);
                            G.configs.PlayerSettingsConfig.Dirty = true;
                        }
                        return;
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("△") && j > 0)
                    {
                        var t = group.Defines[j];
                        group.Defines[j] = group.Defines[j - 1];
                        group.Defines[j - 1] = t;
                        G.configs.PlayerSettingsConfig.Dirty = true;
                        return;
                    }
                    if (GUILayout.Button("▽") && j < group.Defines.Count - 1)
                    {
                        var t = group.Defines[j];
                        group.Defines[j] = group.Defines[j + 1];
                        group.Defines[j + 1] = t;
                        G.configs.PlayerSettingsConfig.Dirty = true;
                        return;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void ConfigToIndex()
        {
            
        }
    }
}
