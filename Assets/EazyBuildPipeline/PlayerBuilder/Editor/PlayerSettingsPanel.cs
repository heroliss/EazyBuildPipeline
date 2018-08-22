using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

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
            //try
            //{
            //    LoadAllConfigs();
            //}
            //catch (Exception e)
            //{
            //    G.configs.DisplayDialog("加载配置文件时发生错误：" + e.Message);
            //}
        }
        public void OnEnable()
        {
        }

        public void OnDestory()
        {

        }

        public void OnGUI()
        {
            //这里当切换Panel时改变焦点，是用来解决当焦点在某个TextField上时输入框遗留显示的问题
            //GUI.SetNextControlName("Toggle1"); //如果有这句话则会影响到SettingsPanel中New时的输入框的焦点，使输入框不能显示
            int selectedToggle_new = GUILayout.Toolbar(selectedToggle, toggles, toggleStyle, GUILayout.Height(30));
            if (selectedToggle_new != selectedToggle)
            {
                selectedToggle = selectedToggle_new;
                GUI.FocusControl("Toggle1"); //这里可能什么都没focus到，但是可以取消当前的focus
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

        private void OnChangeValue()
        {
            G.configs.PlayerSettingsConfig.Dirty = true;
        }

        private void AndroidPanel()
        {
            scrollPosition_Android = EditorGUILayout.BeginScrollView(scrollPosition_Android);

            EditorGUILayout.LabelField("Identification", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("Package Name", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PackageName, OnChangeValue);
            EBPEditorGUILayout.TextField("Client Version", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ClientVersion, OnChangeValue);
            EBPEditorGUILayout.IntField("Bundle Version Code", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BundleVersionCode, OnChangeValue);
            EBPEditorGUILayout.EnumPopup("Minimum API Level", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.MinimumAPILevel, OnChangeValue);
            EBPEditorGUILayout.EnumPopup("Target API Level", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.TargetAPILevel, OnChangeValue);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EBPEditorGUILayout.EnumPopup("Device Filter", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.DeviceFilter, OnChangeValue);
            EBPEditorGUILayout.EnumPopup("Install Location", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InstallLocation, OnChangeValue);
            EBPEditorGUILayout.EnumPopup("Internet Access", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.InternetAccess, OnChangeValue);
            EBPEditorGUILayout.EnumPopup("Write Permission", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.WritePermission, OnChangeValue);
            
            EditorGUILayout.Separator();
            EBPEditorGUILayout.Toggle("Android TV Compatibility", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidTVCompatibility, OnChangeValue);
            EBPEditorGUILayout.Toggle("Android Game", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGame, OnChangeValue);
            //G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.AndroidGameSupprot
            EBPEditorGUILayout.Toggle("Strip Engine Code", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.StripEngineCode, OnChangeValue);
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Preserve Framebuffer Alpha", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.PreserveFramebufferAlpha, OnChangeValue);
            EBPEditorGUILayout.EnumPopup("Blit Type", G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.BlitType, OnChangeValue);
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Protect Graphics Memory", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ProtectGraphicsMemory, OnChangeValue);

            EditorGUILayout.Separator();
            EditorGUILayout.EndScrollView();
        }

        private void IOSPanel()
        {
            scrollPosition_IOS = EditorGUILayout.BeginScrollView(scrollPosition_IOS);

            EditorGUILayout.LabelField("Identification", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("Bundle ID", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BundleID, OnChangeValue);
            EBPEditorGUILayout.TextField("Client Version", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ClientVersion, OnChangeValue);
            EBPEditorGUILayout.TextField("Build Number", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BuildNumber, OnChangeValue);
            EBPEditorGUILayout.Toggle("Automatically Sign", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.AutomaticallySign, OnChangeValue);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("PostProcessing", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("iOS Provisioning Profile", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ProvisioningProfile, OnChangeValue);
            EBPEditorGUILayout.TextField("Team ID", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TeamID, OnChangeValue);
            EBPEditorGUILayout.TextField("Blue Tooth Usage Desc", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.BlueToothUsageDesc, OnChangeValue);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("Camera Usage Desc", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.CameraUsageDesc, OnChangeValue);
            EBPEditorGUILayout.TextField("Location Usage Desc", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.LocationUsageDesc, OnChangeValue);
            EBPEditorGUILayout.TextField("Microphone Usage Desc", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.MicrophoneUsageDesc, OnChangeValue);

            EditorGUILayout.Separator();
            EBPEditorGUILayout.EnumPopup("Target Device", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetDevice, OnChangeValue);
            EBPEditorGUILayout.TextField("Target minimum Version", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.TargetMinimumIOSVersion, OnChangeValue);
            EBPEditorGUILayout.EnumPopup("Architecture", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.Architecture, OnChangeValue);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Optimization", EditorStyles.boldLabel);
            EBPEditorGUILayout.Toggle("Strip Engine Code", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.StripEngineCode, OnChangeValue);
            EBPEditorGUILayout.EnumPopup("Script Call Optimization", G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptCallOptimization, OnChangeValue);
            
            EditorGUILayout.EndScrollView();
        }

        private void GeneralPanel()
        {
            scrollPosition_General = EditorGUILayout.BeginScrollView(scrollPosition_General);

            EditorGUILayout.LabelField("General Player Settings", EditorStyles.boldLabel);
            EBPEditorGUILayout.TextField("Company Name", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.CompanyName, OnChangeValue);
            EBPEditorGUILayout.TextField("Product Name", ref G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ProductName, OnChangeValue);

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            EBPEditorGUILayout.EnumPopup("Compression Method", G.configs.PlayerSettingsConfig.Json.BuildSettings.CompressionMethod, OnChangeValue);
            
            //以下为当前配置，不保存在配置文件中
            EditorGUILayout.Separator();
            EBPEditorGUILayout.Toggle("Development Build", ref G.configs.CurrentConfig.Json.DevelopmentBuild);
            EditorGUI.BeginDisabledGroup(!G.configs.CurrentConfig.Json.DevelopmentBuild);
            EBPEditorGUILayout.Toggle("AutoConnect Profiler", ref G.configs.CurrentConfig.Json.ConnectWithProfiler);
            EBPEditorGUILayout.Toggle("Script Debugging", ref G.configs.CurrentConfig.Json.AllowDebugging);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
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
                    scrollPosition1_ScriptDefines_General = EditorGUILayout.BeginScrollView(scrollPosition1_ScriptDefines_General);
                    ScriptDefineGroupPanel(G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ScriptDefines);
                    EditorGUILayout.EndScrollView();
                    break;
                case "iOS":
                    scrollPosition1_ScriptDefines_IOS = EditorGUILayout.BeginScrollView(scrollPosition1_ScriptDefines_IOS);
                    ScriptDefineGroupPanel(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptDefines);
                    EditorGUILayout.EndScrollView();
                    break;
                case "Android":
                    scrollPosition1_ScriptDefines_Android = EditorGUILayout.BeginScrollView(scrollPosition1_ScriptDefines_Android);
                    ScriptDefineGroupPanel(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ScriptDefines);
                    EditorGUILayout.EndScrollView();
                    break;
                default:
                    break;
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

                EditorGUI.BeginDisabledGroup(!group.Active);

                string newDefineStr = GUILayout.TextField(group.GroupName, "flow overlay header upper left", GUILayout.MinWidth(100), GUILayout.MaxWidth(2000));
                if(group.GroupName != newDefineStr)
                {
                    group.GroupName = newDefineStr;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("+"))
                {
                    if (!group.Defines.Exists(x => x.Define == ""))
                    {
                        group.Defines.Add(new Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefine() { Active = true, Define = "" });
                        G.configs.PlayerSettingsConfig.Dirty = true;
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
                        RemoveGroup(scriptDefinesGroupList, i);
                        G.configs.PlayerSettingsConfig.Dirty = true;
                    }
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("▲") && i > 0)
                {
                    var t = scriptDefinesGroupList[i];
                    scriptDefinesGroupList[i] = scriptDefinesGroupList[i - 1];
                    scriptDefinesGroupList[i - 1] = t;
                    G.configs.PlayerSettingsConfig.Dirty = true;
                }
                if (GUILayout.Button("▼") && i < scriptDefinesGroupList.Count - 1)
                {
                    var t = scriptDefinesGroupList[i];
                    scriptDefinesGroupList[i] = scriptDefinesGroupList[i + 1];
                    scriptDefinesGroupList[i + 1] = t;
                    G.configs.PlayerSettingsConfig.Dirty = true;
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
                    EditorGUI.BeginDisabledGroup(!define.Active);
                    newDefineStr = GUILayout.TextField(define.Define, define.RepeatList.Count > 0 ? scriptDefineTextStyle_red : scriptDefineTextStyle ,GUILayout.MinWidth(100), GUILayout.MaxWidth(2000)).Trim();
                    if(define.Define != newDefineStr)
                    {
                        define.Define = newDefineStr;
                        UpdateRepeatList(scriptDefinesGroupList, define);
                        if (scriptDefinesGroupList != G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ScriptDefines)
                        {
                            UpdateRepeatList(G.configs.PlayerSettingsConfig.Json.PlayerSettings.General.ScriptDefines, define);
                        }
                        else
                        {
                            UpdateRepeatList(G.configs.PlayerSettingsConfig.Json.PlayerSettings.IOS.ScriptDefines, define);
                            UpdateRepeatList(G.configs.PlayerSettingsConfig.Json.PlayerSettings.Android.ScriptDefines, define);
                        }
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
                            RemoveDefine(group, j);
                            G.configs.PlayerSettingsConfig.Dirty = true;
                        }
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("△") && j > 0)
                    {
                        var t = group.Defines[j];
                        group.Defines[j] = group.Defines[j - 1];
                        group.Defines[j - 1] = t;
                        G.configs.PlayerSettingsConfig.Dirty = true;
                    }
                    if (GUILayout.Button("▽") && j < group.Defines.Count - 1)
                    {
                        var t = group.Defines[j];
                        group.Defines[j] = group.Defines[j + 1];
                        group.Defines[j + 1] = t;
                        G.configs.PlayerSettingsConfig.Dirty = true;
                    }
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndVertical();
            }
        }

        private static void UpdateRepeatList(List<Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup> groupList, Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefine currentDefine)
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

        private static void RemoveGroup(List<Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup> groupList, int index)
        {
            var group = groupList[index];
            while(group.Defines.Count > 0)
            {
                RemoveDefine(group, 0);
            }
            groupList.RemoveAt(index);
        }

        private static void RemoveDefine(Configs.PlayerSettingsConfig.PlayerSettings.ScriptDefinesGroup group, int index)
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
