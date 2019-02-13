using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using UnityEditor.iOS.Xcode;

namespace EazyBuildPipeline.PlayerBuilder.Editor
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
        [SerializeField] Vector2 scrollPosition_General,
            scrollPosition1_ScriptDefines_General,
            scrollPosition1_ScriptDefines_IOS,
            scrollPosition1_ScriptDefines_Android,
            scrollPosition_IOS,
            scrollPosition_Android;

        const int defaultHeight = 22;

        private void InitStyles()
        {
            scriptDefineTextStyle = new GUIStyle("RectangleToolVBar");
            scriptDefineTextStyle_red = new GUIStyle("RectangleToolVBar");
            scriptDefineTextStyle_red.normal.textColor = Color.red;
            toggleStyle = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 0, wordWrap = true };
        }

        public void Awake()
        {
            InitStyles();
        }
        public void OnEnable()
        {
        }

        public void OnDestory()
        {

        }

        public void OnGUI()
        {
            int selectedToggle_new = GUILayout.Toolbar(selectedToggle, toggles, toggleStyle, GUILayout.Height(30));
            if (selectedToggle_new != selectedToggle)
            {
                selectedToggle = selectedToggle_new;
                EditorGUIUtility.editingTextField = false;
            }
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

        private void OnValueChanged()
        {
            G.Module.IsDirty = true;
        }

        List<Configs.UserConfig.PlayerSettings.CopyItem> copyList = new List<Configs.UserConfig.PlayerSettings.CopyItem>();
        private void CopyListPanel(BuildTarget buildTarget, ref string directoryRegex, ref string fileRegex)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    copyList = G.Module.UserConfig.Json.PlayerSettings.Android.CopyList;
                    break;
                case BuildTarget.iOS:
                    copyList = G.Module.UserConfig.Json.PlayerSettings.IOS.CopyList;
                    break;
                default:
                    break;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Copy Directory", EditorStyles.boldLabel);
            GUILayout.Label("Active/Revert");
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Copy Now"))
            {
                CopyNow(buildTarget);
            }
            if (GUILayout.Button("Revert Now"))
            {
                RevertNow();
            }
            if (GUILayout.Button("+"))
            {
                copyList.Add(new Configs.UserConfig.PlayerSettings.CopyItem());
                OnValueChanged();
            }
            //EBPEditorGUILayout.TextField("           Directory Regex:", ref directoryRegex, OnValueChanged, GUILayout.MaxWidth(100000));
            //EBPEditorGUILayout.TextField("                    File Regex:", ref fileRegex, OnValueChanged, GUILayout.MaxWidth(100000));
            //GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < copyList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("-");
                EBPEditorGUILayout.Toggle(null, ref copyList[i].Active, OnValueChanged);
                using (var scope = new EditorGUI.DisabledGroupScope(!copyList[i].Active))
                {
                    EBPEditorGUILayout.Toggle(null, ref copyList[i].Revert, OnValueChanged);
                    EBPEditorGUILayout.TextField(null, ref copyList[i].SourcePath, OnValueChanged, GUILayout.MaxWidth(100000));
                    EditorGUILayout.LabelField("→", GUILayout.Width(15));
                    EBPEditorGUILayout.TextField(null, ref copyList[i].TargetPath, OnValueChanged, GUILayout.MaxWidth(100000));
                    copyList[i].CopyMode = (CopyMode)EBPEditorGUILayout.EnumPopup(null, copyList[i].CopyMode, OnValueChanged, GUILayout.Width(70));
                }
                if (GUILayout.Button("-")
                    && ((string.IsNullOrEmpty(copyList[i].SourcePath) && string.IsNullOrEmpty(copyList[i].TargetPath))
                    || G.Module.DisplayDialog("确定删除该项？", "确定", "取消")))
                {
                    copyList.RemoveAt(i);
                    OnValueChanged();
                    EditorGUIUtility.editingTextField = false;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        public static bool RevertNow()
        {
            string logPath = EditorUtility.OpenFilePanel("Open Copy File Log", null, "log");
            string revertLogPath = null;
            if (!string.IsNullOrEmpty(logPath))
            {
                revertLogPath = EditorUtility.SaveFilePanel("Save Revert Log", Path.GetDirectoryName(logPath), "RevertDir", "log");
            }
            if (!string.IsNullOrEmpty(logPath) && !string.IsNullOrEmpty(revertLogPath) && File.Exists(logPath))
            {
                try
                {
                    G.Module.DisplayProgressBar("Start Revert Copied Files", 0f);
                    G.Runner.RevertAllCopiedFiles(File.ReadAllLines(logPath), revertLogPath);
                    return true;
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
            return false;
        }

        public static bool CopyNow(BuildTarget buildTarget)
        {
            string logPath = EditorUtility.SaveFilePanel("Save Copy File Log", null, "CopyDir", "log");
            if (!string.IsNullOrEmpty(logPath))
            {
                try
                {
                    G.Module.DisplayProgressBar("Start Copy Directories", 0);
                    G.Runner.CopyAllDirectories(buildTarget, logPath);
                    return true;
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
            return false;
        }

        private void AndroidPanel()
        {
            var ps = G.Module.UserConfig.Json.PlayerSettings;
            scrollPosition_Android = EditorGUILayout.BeginScrollView(scrollPosition_Android);

            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("Package Name", ref ps.Android.PackageName, OnValueChanged);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Deployment Info", EditorStyles.boldLabel);
            ps.Android.WritePermission = (Configs.UserConfig.PlayerSettings.AndroidSettings.WritePermissionEnum)EBPEditorGUILayout.EnumPopup("Write Permission", ps.Android.WritePermission, OnValueChanged);
            ps.Android.DeviceFilter = (AndroidTargetDevice)EBPEditorGUILayout.EnumPopup("Device Filter", ps.Android.DeviceFilter, OnValueChanged);
            ps.Android.InstallLocation = (AndroidPreferredInstallLocation)EBPEditorGUILayout.EnumPopup("Install Location", ps.Android.InstallLocation, OnValueChanged);
            ps.Android.InternetAccess = (Configs.UserConfig.PlayerSettings.AndroidSettings.InternetAccessEnum)EBPEditorGUILayout.EnumPopup("Internet Access", ps.Android.InternetAccess, OnValueChanged);
            EditorGUILayout.Separator();
            ps.Android.MinimumAPILevel = (AndroidSdkVersions)EBPEditorGUILayout.EnumPopup("Minimum API Level", ps.Android.MinimumAPILevel, OnValueChanged);
            ps.Android.TargetAPILevel = (AndroidSdkVersions)EBPEditorGUILayout.EnumPopup("Target API Level", ps.Android.TargetAPILevel, OnValueChanged);
            EditorGUILayout.Separator();
            ps.Android.BuildSystem = (AndroidBuildSystem)EBPEditorGUILayout.EnumPopup("Build System", ps.Android.BuildSystem, OnValueChanged);
            EBPEditorGUILayout.Toggle("Use OBB Mode", ref ps.Android.UseObbMode, OnValueChanged);
            EBPEditorGUILayout.TextField("Keystore Name", ref ps.Android.KeystoreName, OnValueChanged);
            EBPEditorGUILayout.TextField("Keystore Pass", ref ps.Android.KeystorePass, OnValueChanged);
            EBPEditorGUILayout.TextField("Keyalias Name", ref ps.Android.KeyaliasName, OnValueChanged);
            EBPEditorGUILayout.TextField("Keyalias Pass", ref ps.Android.KeyaliasPass, OnValueChanged);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Support", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Android TV Compatibility", ref ps.Android.AndroidTVCompatibility, OnValueChanged);
            EBPEditorGUILayout.Toggle("Android Game", ref ps.Android.AndroidGame, OnValueChanged);
            //ps.Android.AndroidGameSupprot
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Optimization", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Strip Engine Code", ref ps.Android.StripEngineCode, OnValueChanged);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Preserve Framebuffer Alpha", ref ps.Android.PreserveFramebufferAlpha, OnValueChanged);
            ps.Android.BlitType = (AndroidBlitType)EBPEditorGUILayout.EnumPopup("Blit Type", ps.Android.BlitType, OnValueChanged);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Protect Graphics Memory", ref ps.Android.ProtectGraphicsMemory, OnValueChanged);
            EditorGUILayout.Separator();

            CopyListPanel(BuildTarget.Android, ref ps.Android.CopyDirectoryRegex, ref ps.Android.CopyFileRegex);

            EditorGUILayout.EndScrollView();
        }

        private void IOSPanel()
        {
            var ps = G.Module.UserConfig.Json.PlayerSettings;
            scrollPosition_IOS = EditorGUILayout.BeginScrollView(scrollPosition_IOS);

            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("Bundle Identifier", ref ps.IOS.BundleID, OnValueChanged);
            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Signing", EditorStyles.boldLabel);
                if (GUILayout.Button("Open Provising Profile", GUILayout.MaxWidth(160)))
                {
                    string path = EditorUtility.OpenFilePanel("Select the Provising Profile used for Manual Signing", null, "mobileprovision");
                    if (!string.IsNullOrEmpty(path))
                    {
                        EditorGUIUtility.editingTextField = false;
                        string text = File.ReadAllText(path, System.Text.Encoding.UTF8);
                        int start = text.IndexOf("<plist");
                        int len = text.IndexOf("</plist") - start;
                        string plistStr = text.Substring(start, len) + "</plist>";
                        PlistDocument plist = new PlistDocument();
                        plist.ReadFromString(plistStr.Replace("date", "string")); //Hack: 此处date替换为string是因为这个PlistDocument不能识别date类型导致报错
                        ps.IOS.ProfileID = plist.root.values["UUID"].AsString();
                        ps.IOS.TeamID = plist.root.values["TeamIdentifier"].AsArray().values[0].AsString();
                    }
                }
            }
            EBPEditorGUILayout.TextField("Team ID", ref ps.IOS.TeamID, OnValueChanged);
            EBPEditorGUILayout.Toggle("Automatically Sign", ref ps.IOS.AutomaticallySign, OnValueChanged);

            EditorGUI.BeginDisabledGroup(ps.IOS.AutomaticallySign);
            EBPEditorGUILayout.TextField("Profile ID", ref ps.IOS.ProfileID, OnValueChanged);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("IPA Export Options", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Export IPA", ref ps.IOS.IPAExportOptions.ExportIPA, OnValueChanged);
            EditorGUI.BeginDisabledGroup(!ps.IOS.IPAExportOptions.ExportIPA);
            EBPEditorGUILayout.Toggle("CompileBitcode", ref ps.IOS.IPAExportOptions.CompileBitcode, OnValueChanged);
            using (new EditorGUILayout.HorizontalScope())
            {
                EBPEditorGUILayout.TextField("Method", ref ps.IOS.IPAExportOptions.Method, OnValueChanged);
                if (GUILayout.Button("Options", GUILayout.MaxWidth(60)))
                {
                    EditorGUIUtility.editingTextField = false;
                    GenericMenu menu = new GenericMenu();
                    foreach (var option in ps.IOS.IPAExportOptions.Methods)
                    {
                        menu.AddItem(new GUIContent(option), false, () => { ps.IOS.IPAExportOptions.Method = option; });
                    }
                    menu.ShowAsContext();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Deployment Info", EditorStyles.boldLabel);
            ps.IOS.TargetDevice = (iOSTargetDevice)EBPEditorGUILayout.EnumPopup("Target Device", ps.IOS.TargetDevice, OnValueChanged);
            ps.IOS.TargetSDK = (iOSSdkVersion)EBPEditorGUILayout.EnumPopup("Target SDK", ps.IOS.TargetSDK, OnValueChanged);
            EBPEditorGUILayout.TextField("Target minimum Version", ref ps.IOS.TargetMinimumIOSVersion, OnValueChanged);
            ps.IOS.Architecture = (Configs.UserConfig.PlayerSettings.IOSSettings.ArchitectureEnum)EBPEditorGUILayout.EnumPopup("Architecture", ps.IOS.Architecture, OnValueChanged);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Optimization", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Strip Engine Code", ref ps.IOS.StripEngineCode, OnValueChanged);
            ps.IOS.ScriptCallOptimization = (ScriptCallOptimizationLevel)EBPEditorGUILayout.EnumPopup("Script Call Optimization", ps.IOS.ScriptCallOptimization, OnValueChanged);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("Camera Usage Desc", ref ps.IOS.CameraUsageDesc, OnValueChanged);
            EBPEditorGUILayout.TextField("Location Usage Desc", ref ps.IOS.LocationUsageDesc, OnValueChanged);
            EBPEditorGUILayout.TextField("Microphone Usage Desc", ref ps.IOS.MicrophoneUsageDesc, OnValueChanged);
            EBPEditorGUILayout.TextField("BlueTooth Usage Desc", ref ps.IOS.BlueToothUsageDesc, OnValueChanged);
            EBPEditorGUILayout.TextField("Photo Usage Desc", ref ps.IOS.PhotoUsageDesc, OnValueChanged);
            EBPEditorGUILayout.TextField("Photo Usage Add Desc", ref ps.IOS.PhotoUsageAddDesc, OnValueChanged);
            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Others", EditorStyles.boldLabel);
            //EBPEditorGUILayout.TextField("Third FrameWork Path", ref ps.IOS.ThirdFrameWorkPath, OnValueChanged);
            //EBPEditorGUILayout.Toggle("IsBuildArchive", ref ps.IOS.IsBuildArchive, OnValueChanged);
            //EditorGUI.BeginDisabledGroup(!ps.IOS.IsBuildArchive);
            //EBPEditorGUILayout.TextField("Export Ipa Path", ref ps.IOS.ExportIpaPath, OnValueChanged);
            //EBPEditorGUILayout.TextField("TaskPath", ref ps.IOS.TaskPath, OnValueChanged);
            //EditorGUI.EndDisabledGroup();
            EditorGUILayout.Separator();

            CopyListPanel(BuildTarget.iOS, ref ps.IOS.CopyDirectoryRegex, ref ps.IOS.CopyFileRegex);

            EditorGUILayout.EndScrollView();
        }

        private void GeneralPanel()
        {
            var ps = G.Module.UserConfig.Json.PlayerSettings;
            scrollPosition_General = EditorGUILayout.BeginScrollView(scrollPosition_General);

            EditorGUILayout.LabelField("General Player Settings", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("Company Name", ref ps.General.CompanyName, OnValueChanged);
            EBPEditorGUILayout.TextField("Product Name", ref ps.General.ProductName, OnValueChanged);
            EditorGUILayout.Separator();

            ps.General.Channel = (EazyGameChannel.Channels)EBPEditorGUILayout.EnumPopup("Channel", ps.General.Channel, OnValueChanged);
            EditorGUILayout.Separator();

            EBPEditorGUILayout.TextField("Game Config URL", ref ps.General.ConfigURL_Game, OnValueChanged);
            EBPEditorGUILayout.TextField("Language Config URL", ref ps.General.ConfigURL_Language, OnValueChanged);
            EBPEditorGUILayout.TextField("LanguageVersion Config URL", ref ps.General.ConfigURL_LanguageVersion, OnValueChanged);
            EditorGUILayout.Separator();

            EBPEditorGUILayout.TextField("BuglyAppID", ref ps.General.BuglyAppID, OnValueChanged);
            EBPEditorGUILayout.TextField("BuglyAppKey", ref ps.General.BuglyAppKey, OnValueChanged);
            EditorGUILayout.Separator();

            //EditorGUILayout.LabelField("Download Configs & Language", EditorStyles.boldLabel);
            //ps.General.DownloadConfigType = (Configs.UserConfig.PlayerSettings.GeneralSettings.DownloadConfigTypeEnum)EBPEditorGUILayout.EnumPopup("Configs", ps.General.DownloadConfigType, OnValueChanged);
            //ps.General.DownloadLanguageType = (WowGamePlay.LanguageType)EBPEditorGUILayout.EnumPopup("Language", ps.General.DownloadLanguageType, OnValueChanged);
            //EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            G.Module.UserConfig.Json.BuildSettings.CompressionMethod = (Configs.UserConfig.BuildSettings.CompressionMethodEnum)EBPEditorGUILayout.EnumPopup("Compression Method", G.Module.UserConfig.Json.BuildSettings.CompressionMethod, OnValueChanged);
            EditorGUILayout.Separator();

            //以下为当前配置，不保存在配置文件中
            using (var tempBuildScope = new GUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Temp Build Settings", EditorStyles.boldLabel);
                EBPEditorGUILayout.Toggle("Development Build", ref G.Module.ModuleStateConfig.Json.DevelopmentBuild);
                EditorGUI.BeginDisabledGroup(!G.Module.ModuleStateConfig.Json.DevelopmentBuild);
                EBPEditorGUILayout.Toggle("AutoConnect Profiler", ref G.Module.ModuleStateConfig.Json.ConnectWithProfiler);
                EBPEditorGUILayout.Toggle("Script Debugging", ref G.Module.ModuleStateConfig.Json.AllowDebugging);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndScrollView();
        }

        private void ScriptDefinesPanel()
        {
            var ps = G.Module.UserConfig.Json.PlayerSettings;
            EditorGUILayout.BeginHorizontal();
            selectedPlatformToggle = GUILayout.Toolbar(selectedPlatformToggle, platformToggles, GUILayout.Height(30), GUILayout.MaxWidth(300));
            string platform = platformToggles[selectedPlatformToggle];
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Group", GUILayout.Height(30)))
            {
                var group = new Configs.UserConfig.PlayerSettings.ScriptDefinesGroup() { GroupName = "Script Defines Group", Active = true };
                group.Defines.Add(new Configs.UserConfig.PlayerSettings.ScriptDefine() { Active = true, Define = "" });
                switch (platform)
                {
                    case "General":
                        ps.General.ScriptDefines.Add(group);
                        break;
                    case "iOS":
                        ps.IOS.ScriptDefines.Add(group);
                        break;
                    case "Android":
                        ps.Android.ScriptDefines.Add(group);
                        break;
                }
                G.Module.IsDirty = true;
            }
            if (GUILayout.Button(new GUIContent("Apply Scripting Defines","应用当前平台所有勾选的宏定义"), GUILayout.Height(30)))
            {
                G.Runner.ApplyScriptDefines(EditorUserBuildSettings.activeBuildTarget);
            }
            EditorGUILayout.EndHorizontal();

            switch (platform)
            {
                case "General":
                    scrollPosition1_ScriptDefines_General = EditorGUILayout.BeginScrollView(scrollPosition1_ScriptDefines_General);
                    ScriptDefineGroupPanel(ps.General.ScriptDefines);
                    EditorGUILayout.EndScrollView();
                    break;
                case "iOS":
                    scrollPosition1_ScriptDefines_IOS = EditorGUILayout.BeginScrollView(scrollPosition1_ScriptDefines_IOS);
                    ScriptDefineGroupPanel(ps.IOS.ScriptDefines);
                    EditorGUILayout.EndScrollView();
                    break;
                case "Android":
                    scrollPosition1_ScriptDefines_Android = EditorGUILayout.BeginScrollView(scrollPosition1_ScriptDefines_Android);
                    ScriptDefineGroupPanel(ps.Android.ScriptDefines);
                    EditorGUILayout.EndScrollView();
                    break;
                default:
                    break;
            }
        }

        private void ScriptDefineGroupPanel(List<Configs.UserConfig.PlayerSettings.ScriptDefinesGroup> scriptDefinesGroupList)
        {
            var ps = G.Module.UserConfig.Json.PlayerSettings;

            for (int i = 0; i < scriptDefinesGroupList.Count; i++)
            {
                var group = scriptDefinesGroupList[i];

                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.BeginHorizontal();
                bool b = GUILayout.Toggle(group.Active, GUIContent.none);
                if (group.Active != b)
                {
                    group.Active = b;
                    G.Module.IsDirty = true;
                }

                EditorGUI.BeginDisabledGroup(!group.Active);

                string newDefineStr = EditorGUILayout.TextField(group.GroupName, (GUIStyle)"flow overlay box", GUILayout.MinWidth(100), GUILayout.MaxWidth(2000));
                if (group.GroupName != newDefineStr)
                {
                    group.GroupName = newDefineStr;
                    G.Module.IsDirty = true;
                }
                EditorGUILayout.Space();
                GUILayout.Label("Revert");
                EditorGUILayout.Space();
                if (GUILayout.Button("×"))
                {
                    bool ensure = true;
                    if (group.Defines.Count > 0)
                    {
                        ensure = G.Module.DisplayDialog("确定要删除宏定义组“" + group.GroupName + "”及其所有内容？", "确定", "取消");
                    }
                    if (ensure)
                    {
                        RemoveGroup(scriptDefinesGroupList, i);
                        G.Module.IsDirty = true;
                    }
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("▲") && i > 0)
                {
                    var t = scriptDefinesGroupList[i];
                    scriptDefinesGroupList[i] = scriptDefinesGroupList[i - 1];
                    scriptDefinesGroupList[i - 1] = t;
                    G.Module.IsDirty = true;
                }
                if (GUILayout.Button("▼") && i < scriptDefinesGroupList.Count - 1)
                {
                    var t = scriptDefinesGroupList[i];
                    scriptDefinesGroupList[i] = scriptDefinesGroupList[i + 1];
                    scriptDefinesGroupList[i + 1] = t;
                    G.Module.IsDirty = true;
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
                        G.Module.IsDirty = true;
                    }
                    EditorGUI.BeginDisabledGroup(!define.Active);
                    newDefineStr = EditorGUILayout.TextField(define.Define, define.RepeatList.Count > 0 ? scriptDefineTextStyle_red : scriptDefineTextStyle, GUILayout.MinWidth(100), GUILayout.MaxWidth(2000)).Trim();
                    if (define.Define != newDefineStr)
                    {
                        define.Define = newDefineStr;
                        UpdateRepeatList(scriptDefinesGroupList, define);
                        if (scriptDefinesGroupList != ps.General.ScriptDefines)
                        {
                            UpdateRepeatList(ps.General.ScriptDefines, define);
                        }
                        else
                        {
                            UpdateRepeatList(ps.IOS.ScriptDefines, define);
                            UpdateRepeatList(ps.Android.ScriptDefines, define);
                        }
                        G.Module.IsDirty = true;
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.Space();
                    bool revert_new = GUILayout.Toggle(define.Revert, GUIContent.none);
                    if (revert_new != define.Revert)
                    {
                        define.Revert = revert_new;
                        G.Module.IsDirty = true;
                    }
                    GUILayout.Space(10);
                    if (GUILayout.Button("+"))
                    {
                        if (!group.Defines.Exists(x => x.Define == ""))
                        {
                            group.Defines.Insert(j + 1, new Configs.UserConfig.PlayerSettings.ScriptDefine() { Active = true, Define = "" });
                            G.Module.IsDirty = true;
                        }
                    }
                    if (GUILayout.Button("-"))
                    {
                        if (group.Defines.Count > 1)
                        {
                            bool ensure = true;
                            if (define.Define != "")
                            {
                                ensure = G.Module.DisplayDialog("确定要删除宏定义" + define.Define + "?", "确定", "取消");
                            }
                            if (ensure)
                            {
                                RemoveDefine(group, j);
                                G.Module.IsDirty = true;
                            }
                        }
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("△") && j > 0)
                    {
                        var t = group.Defines[j];
                        group.Defines[j] = group.Defines[j - 1];
                        group.Defines[j - 1] = t;
                        G.Module.IsDirty = true;
                    }
                    if (GUILayout.Button("▽") && j < group.Defines.Count - 1)
                    {
                        var t = group.Defines[j];
                        group.Defines[j] = group.Defines[j + 1];
                        group.Defines[j + 1] = t;
                        G.Module.IsDirty = true;
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndVertical();
            }
        }

        private static void UpdateRepeatList(List<Configs.UserConfig.PlayerSettings.ScriptDefinesGroup> groupList, Configs.UserConfig.PlayerSettings.ScriptDefine currentDefine)
        {
            foreach (var group in groupList)
            {
                foreach (var define in group.Defines)
                {
                    if (define == currentDefine)
                        continue;
                    if (define.Define == currentDefine.Define)
                    {
                        define.RepeatList.Add(currentDefine);
                        currentDefine.RepeatList.Add(define);
                    }
                    else
                    {
                        define.RepeatList.Remove(currentDefine);
                        currentDefine.RepeatList.Remove(define);
                    }
                }
            }
        }

        private static void RemoveGroup(List<Configs.UserConfig.PlayerSettings.ScriptDefinesGroup> groupList, int index)
        {
            var group = groupList[index];
            while (group.Defines.Count > 0)
            {
                RemoveDefine(group, 0);
            }
            groupList.RemoveAt(index);
        }

        private static void RemoveDefine(Configs.UserConfig.PlayerSettings.ScriptDefinesGroup group, int index)
        {
            var define = group.Defines[index];
            for (int i = 0; i < define.RepeatList.Count; i++)
            {
                define.RepeatList[i].RepeatList.Remove(define);
            }
            group.Defines.RemoveAt(index);
        }
    }
}
