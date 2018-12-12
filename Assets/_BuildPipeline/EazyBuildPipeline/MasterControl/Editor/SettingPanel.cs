using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EazyBuildPipeline.MasterControl.Editor
{
    [Serializable]
    public class SettingPanel
    {
        enum Step { None, Start, SVNUpdate, PreprocessAssets, BuildBundles, BuildPackages, PrepareBuildPlayer, BuildPlayer, Finish }
        [SerializeField] string SVNMessage;
        [SerializeField] Step currentStep = Step.None;
        [SerializeField] double startTime;
        [SerializeField] bool creatingNewConfig;
        [SerializeField] int selectedCompressionIndex;
        [SerializeField] Vector2 scrollPosition;
        [SerializeField] string[] assetPreprocessorUserConfigNames;
        [SerializeField] string[] bundleManagerUserConfigNames;
        [SerializeField] string[] packageManagerUserConfigNames;
        [SerializeField] string[] userConfigNames;
        [SerializeField] int assetPreprocessorUserConfigSelectedIndex;
        [SerializeField] int bundleManagerUserConfigSelectedIndex;
        [SerializeField] int packageManagerUserConfigSelectedIndex;
        [SerializeField] int playerBuilderUserConfigSelectedIndex;

        [SerializeField] GUIStyle richTextLabel;
        [SerializeField] Texture2D settingIcon;
        [SerializeField] Texture2D warnIcon;
        [SerializeField] Texture2D fingerIcon;
        [SerializeField] Texture2D disableIcon;
        [SerializeField] Texture2D needUpdateIcon;
        [SerializeField] Texture2D okIcon;
        [SerializeField] Texture2D refreshIcon;
        [SerializeField] Texture2D unknowIcon;
        [SerializeField] Texture2D changeIcon;

        [SerializeField] GUIContent settingGUIContent;
        [SerializeField] GUIContent assetPreprocessorContent;
        [SerializeField] GUIContent bundleManagerContent;
        [SerializeField] GUIContent packageManagerContent;
        [SerializeField] GUIContent playerBuilderContent;

        GUILayoutOption[] dropdownOptions = { GUILayout.Width(150) };
        GUILayoutOption[] dropdownOptions2 = { GUILayout.MaxWidth(100) };
        GUILayoutOption[] buttonOptions = { GUILayout.MaxWidth(60) };
        GUILayoutOption[] labelOptions = { GUILayout.MinWidth(20), GUILayout.MaxWidth(110) };
        GUILayoutOption[] miniButtonOptions = { GUILayout.MaxHeight(18), GUILayout.MaxWidth(22) };
        GUILayoutOption[] inputOptions = { GUILayout.Width(50) };
        GUILayoutOption[] iconOptions = { GUILayout.Width(20), GUILayout.Height(20) };
        bool needRepaint;

        public void Awake()
        {
            LoadAllConfigs();
            SetIcons();
            InitStyles();
        }
        public void OnEnable()
        {
            SetupActions();
            RunSVNCheckProcess();
            if (currentStep != Step.None)
            {
                EditorApplication.update += UpdateForRun;
            }
        }
        public void OnProjectChange()
        {
            RunSVNCheckProcess();
        }
        private void RunSVNCheckProcess()
        {
            SVNMessage = "正在获取SVN信息...";
            G.Module.SVNUpdateRunner.RunCheckProcess();
        }
        private void Action_AssetPreprocessor_OnChangeCurrentConfig()
        {
            if (AssetPreprocessor.G.Module.ModuleStateConfig.Json.CurrentUserConfigName == G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.CurrentUserConfigName)
            {
                G.Module.AssetPreprocessorModule.LoadUserConfig(); //重新加载
            }
        }
        private void Action_OnChangeConfigList()
        {
            LoadAllModulesUserConfigList();
            ConfigToIndex();
        }
        private void SetupActions()
        {
            G.Module.SVNUpdateRunner.InfoExitedAction += (success) =>
            {
                SVNMessage = success ? "正在检查本地修改..." : "SVN不可用！";
                if (!G.Module.SVNUpdateModule.ModuleConfig.Json.EnableCheckDiff)
                    G.Module.SVNUpdateRunner.DiffExitedAction(true);
                needRepaint = true; //由于非主线程中不能使用 G.g.MainWindow.Repaint(); 所以只能出此下策，用变量标记需要刷新，然后在Update中刷新界面
            };
            G.Module.SVNUpdateRunner.DiffExitedAction += (success) =>
            {
                if (success)
                {
                    switch (G.Module.SVNUpdateRunner.VersionState)
                    {
                        case SVNUpdate.Runner.VersionStateEnum.Unknow:
                            SVNMessage = "SVN不可用!";
                            break;
                        case SVNUpdate.Runner.VersionStateEnum.Obsolete:
                            SVNMessage = "需要更新!";
                            break;
                        case SVNUpdate.Runner.VersionStateEnum.Latest:
                            SVNMessage = "已最新";
                            break;
                    }
                    if (G.Module.SVNUpdateModule.ModuleConfig.Json.EnableCheckDiff)
                    {
                        switch (G.Module.SVNUpdateRunner.LocalChangeState)
                        {
                            case SVNUpdate.Runner.ChangeStateEnum.Unknow:
                                SVNMessage = "检查本地修改失败！";
                                break;
                            case SVNUpdate.Runner.ChangeStateEnum.Changed:
                                SVNMessage = "本地文件有改动！";
                                break;
                        }
                    }
                }
                needRepaint = true;
            };
            if (AssetPreprocessor.G.g != null)
            {
                AssetPreprocessor.G.g.OnChangeCurrentUserConfig += Action_AssetPreprocessor_OnChangeCurrentConfig;
                AssetPreprocessor.G.g.OnChangeConfigList += Action_OnChangeConfigList;
            }
            if (BundleManager.G.g != null)
            {
                BundleManager.G.g.OnChangeConfigList += Action_OnChangeConfigList;
            }
            if (PackageManager.G.g != null)
            {
                PackageManager.G.g.OnChangeConfigList += Action_OnChangeConfigList;
            }
        }
        private void SetdownActions()
        {
            if (AssetPreprocessor.G.g != null)
            {
                AssetPreprocessor.G.g.OnChangeCurrentUserConfig -= Action_AssetPreprocessor_OnChangeCurrentConfig;
                AssetPreprocessor.G.g.OnChangeConfigList -= Action_OnChangeConfigList;
            }
            if (BundleManager.G.g != null)
            {
                BundleManager.G.g.OnChangeConfigList -= Action_OnChangeConfigList;
            }
            if (PackageManager.G.g != null)
            {
                PackageManager.G.g.OnChangeConfigList -= Action_OnChangeConfigList;
            }
        }

        private void InitStyles()
        {
            richTextLabel = new GUIStyle(EditorStyles.label) { richText = true };

            settingGUIContent = new GUIContent(settingIcon);
            assetPreprocessorContent = new GUIContent("Preprocess Assets:");
            bundleManagerContent = new GUIContent("Build Bundles:");
            packageManagerContent = new GUIContent("Build Packages:");
            playerBuilderContent = new GUIContent("Build Player:");
        }

        private void LoadAllConfigs()
        {
            CommonModule.LoadCommonConfig();
            G.Module.LoadAllModules();
            InitSelectedIndex();
            LoadAllModulesUserConfigList();
            ConfigToIndex();
        }

        void SetIcons()
        {
            try
            {
                warnIcon = EditorGUIUtility.FindTexture("console.warnicon.sml");
                settingIcon = CommonModule.GetIcon("SettingIcon.png");
                fingerIcon = CommonModule.GetIcon("FingerIcon.png");
                disableIcon = CommonModule.GetIcon("DisableIcon.png");
                needUpdateIcon = CommonModule.GetIcon("NeedUpdateIcon.png");
                okIcon = CommonModule.GetIcon("OKIcon.png");
                refreshIcon = CommonModule.GetIcon("RefreshIcon.png");
                unknowIcon = CommonModule.GetIcon("UnknowIcon.png");
                changeIcon = CommonModule.GetIcon("ChangeIcon.png");
            }
            catch (Exception e)
            {
                G.Module.DisplayDialog("加载Icon时发生错误：" + e.Message);
            }
        }

        private void ConfigToIndex()
        {
            assetPreprocessorUserConfigSelectedIndex = assetPreprocessorUserConfigNames.IndexOf(G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.CurrentUserConfigName.RemoveExtension());
            bundleManagerUserConfigSelectedIndex = bundleManagerUserConfigNames.IndexOf(G.Module.BundleManagerModule.ModuleStateConfig.Json.CurrentUserConfigName.RemoveExtension());
            packageManagerUserConfigSelectedIndex = packageManagerUserConfigNames.IndexOf(G.Module.PackageManagerModule.ModuleStateConfig.Json.CurrentUserConfigName.RemoveExtension());
            playerBuilderUserConfigSelectedIndex = userConfigNames.IndexOf(G.Module.PlayerBuilderModule.ModuleStateConfig.Json.CurrentUserConfigName.RemoveExtension());

            string compressionName = G.Module.BundleManagerModule.CompressionEnumMap.FirstOrDefault(x => x.Value == (G.Module.BundleManagerModule.ModuleStateConfig.Json.CompressionOption)).Key;
            selectedCompressionIndex = G.Module.BundleManagerModule.CompressionEnum.IndexOf(compressionName);
        }

        private int GetTagIndex(string[] sList, string s, int count)
        {
            if (string.IsNullOrEmpty(s)) return -1;
            for (int i = 0; i < sList.Length; i++)
            {
                if (s == sList[i])
                {
                    return i;
                }
            }
            G.Module.DisplayDialog(string.Format("加载配置文件时发生错误：\n欲加载的类型“{0}”"
                  + "不存在于第 {1} 个全局类型枚举中！\n", s, count));
            return -1;
        }

        private void LoadAllModulesUserConfigList()
        {
            assetPreprocessorUserConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.AssetPreprocessorModule.ModuleConfig.UserConfigsFolderPath);
            bundleManagerUserConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.BundleManagerModule.ModuleConfig.UserConfigsFolderPath);
            packageManagerUserConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.PackageManagerModule.ModuleConfig.UserConfigsFolderPath);
            userConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.PlayerBuilderModule.ModuleConfig.UserConfigsFolderPath);
        }

        private void InitSelectedIndex()
        {
            selectedCompressionIndex = -1;
            assetPreprocessorUserConfigSelectedIndex = -1;
            bundleManagerUserConfigSelectedIndex = -1;
            packageManagerUserConfigSelectedIndex = -1;
            playerBuilderUserConfigSelectedIndex = -1;
            assetPreprocessorUserConfigNames = new string[0];
            bundleManagerUserConfigNames = new string[0];
            packageManagerUserConfigNames = new string[0];
            userConfigNames = new string[0];
        }

        public void Update()
        {
            if (needRepaint)
            {
                G.g.MainWindow.Repaint();
                needRepaint = false;
            }
        }

        public void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (creatingNewConfig == true && GUI.GetNameOfFocusedControl() != "InputField1")
            {
                creatingNewConfig = false;
            }
            //Root
            GUILayout.FlexibleSpace();
            EBPEditorGUILayout.RootSettingLine(PlayerBuilder.G.Module, ChangeRootPath);
            GUILayout.FlexibleSpace();

            //SVN Update
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(currentStep == Step.SVNUpdate, false, G.Module.SVNUpdateRunner.errorMessage);
            G.Module.SVNUpdateRunner.IsPartOfPipeline = GUILayout.Toggle(G.Module.SVNUpdateRunner.IsPartOfPipeline, "SVN Update", GUILayout.Width(200)) && G.Module.SVNUpdateRunner.Available;
            SVNInfo();
            if (GUILayout.Button(refreshIcon, miniButtonOptions))
            {
                RunSVNCheckProcess();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //AssetPreprocessor
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(currentStep == Step.PreprocessAssets, G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.Applying,
                           G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.ErrorMessage);
            G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.IsPartOfPipeline = EditorGUILayout.BeginToggleGroup(
                assetPreprocessorContent, G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.IsPartOfPipeline);

            EditorGUILayout.BeginHorizontal();
            int index_new = EditorGUILayout.Popup(assetPreprocessorUserConfigSelectedIndex, assetPreprocessorUserConfigNames, dropdownOptions);
            if (assetPreprocessorUserConfigSelectedIndex != index_new)
            {
                assetPreprocessorUserConfigSelectedIndex = index_new;
                G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.CurrentUserConfigName = assetPreprocessorUserConfigNames[index_new] + ".json";
                G.Module.AssetPreprocessorModule.LoadUserConfig();
                return;
            }
            if (GUILayout.Button(settingGUIContent, miniButtonOptions))
            {
                AssetPreprocessor.G.OverrideCurrentUserConfigName = G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.CurrentUserConfigName;
                if (AssetPreprocessor.G.g == null)
                {
                    EditorWindow.GetWindow<AssetPreprocessor.Editor.PreprocessorWindow>();
                    AssetPreprocessor.G.g.OnChangeCurrentUserConfig += Action_AssetPreprocessor_OnChangeCurrentConfig;
                    AssetPreprocessor.G.g.OnChangeConfigList += Action_OnChangeConfigList;
                }
                else
                {
                    EditorWindow.GetWindow<AssetPreprocessor.Editor.PreprocessorWindow>();
                }
                return;
            }
            GUILayout.Space(10);
            GUILayout.Label(G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.IsPartOfPipeline ?
                            "<color=orange>→  " + EBPUtility.GetTagStr(G.Module.AssetPreprocessorModule.UserConfig.Json.Tags) + "</color>" :
                            "<color=cyan>" + EBPUtility.GetTagStr(CommonModule.CommonConfig.Json.CurrentAssetTag) + "</color>", richTextLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //BundleManager
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(currentStep == Step.BuildBundles, G.Module.BundleManagerModule.ModuleStateConfig.Json.Applying,
                           G.Module.BundleManagerModule.ModuleStateConfig.Json.ErrorMessage);
            G.Module.BundleManagerModule.ModuleStateConfig.Json.IsPartOfPipeline = EditorGUILayout.BeginToggleGroup(
                bundleManagerContent, G.Module.BundleManagerModule.ModuleStateConfig.Json.IsPartOfPipeline);
            EditorGUILayout.BeginHorizontal();

            index_new = EditorGUILayout.Popup(bundleManagerUserConfigSelectedIndex, bundleManagerUserConfigNames, dropdownOptions);
            if (bundleManagerUserConfigSelectedIndex != index_new)
            {
                bundleManagerUserConfigSelectedIndex = index_new;
                G.Module.BundleManagerModule.ModuleStateConfig.Json.CurrentUserConfigName = bundleManagerUserConfigNames[index_new] + ".json";
                return;
            }
            if (GUILayout.Button(settingGUIContent, miniButtonOptions))
            {
                if (BundleManager.G.g == null)
                {
                    EditorWindow.GetWindow<BundleManager.Editor.BundleManagerWindow>();
                    BundleManager.G.g.OnChangeConfigList += Action_OnChangeConfigList;
                }
                else
                {
                    EditorWindow.GetWindow<BundleManager.Editor.BundleManagerWindow>();
                }
                return;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Resource Version:", labelOptions);
            int n = EditorGUILayout.IntField(G.Module.BundleManagerModule.ModuleStateConfig.Json.ResourceVersion, inputOptions);
            if (G.Module.BundleManagerModule.ModuleStateConfig.Json.ResourceVersion != n)
            {
                G.Module.BundleManagerModule.ModuleStateConfig.Json.ResourceVersion = n;
            }

            int selectedCompressionIndex_new = EditorGUILayout.Popup(selectedCompressionIndex, G.Module.BundleManagerModule.CompressionEnum, dropdownOptions2);
            if (selectedCompressionIndex_new != selectedCompressionIndex)
            {
                G.Module.BundleManagerModule.ModuleStateConfig.Json.CompressionOption = G.Module.BundleManagerModule.CompressionEnumMap[G.Module.BundleManagerModule.CompressionEnum[selectedCompressionIndex_new]];
                selectedCompressionIndex = selectedCompressionIndex_new;
                return;
            }

            G.Module.BundleManagerModule.ModuleStateConfig.Json.CleanUpBundles = GUILayout.Toggle(G.Module.BundleManagerModule.ModuleStateConfig.Json.CleanUpBundles, "CleanUp");

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //PackageManager
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(currentStep == Step.BuildPackages, G.Module.PackageManagerModule.ModuleStateConfig.Json.Applying,
                          G.Module.PackageManagerModule.ModuleStateConfig.Json.ErrorMessage);
            G.Module.PackageManagerModule.ModuleStateConfig.Json.IsPartOfPipeline = EditorGUILayout.BeginToggleGroup(
                   packageManagerContent, G.Module.PackageManagerModule.ModuleStateConfig.Json.IsPartOfPipeline);
            EditorGUILayout.BeginHorizontal();

            index_new = EditorGUILayout.Popup(packageManagerUserConfigSelectedIndex, packageManagerUserConfigNames, dropdownOptions);
            if (packageManagerUserConfigSelectedIndex != index_new)
            {
                packageManagerUserConfigSelectedIndex = index_new;
                G.Module.PackageManagerModule.ModuleStateConfig.Json.CurrentUserConfigName = packageManagerUserConfigNames[index_new] + ".json";
                return;
            }
            if (GUILayout.Button(settingGUIContent, miniButtonOptions))
            {
                PackageManager.G.OverrideCurrentUserConfigName = G.Module.PackageManagerModule.ModuleStateConfig.Json.CurrentUserConfigName;
                if (PackageManager.G.g == null)
                {
                    EditorWindow.GetWindow<PackageManager.Editor.PackageManagerWindow>();
                    PackageManager.G.g.OnChangeConfigList += Action_OnChangeConfigList;
                }
                else
                {
                    EditorWindow.GetWindow<PackageManager.Editor.PackageManagerWindow>();
                }
                return;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Client Version:", labelOptions);
            string packageVersion_new = EditorGUILayout.TextField(G.Module.PackageManagerModule.ModuleStateConfig.Json.ClientVersion, inputOptions);
            if (G.Module.PackageManagerModule.ModuleStateConfig.Json.ClientVersion != packageVersion_new)
            {
                G.Module.PackageManagerModule.ModuleStateConfig.Json.ClientVersion = packageVersion_new;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //BuildPlayer
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(currentStep == Step.BuildPlayer || currentStep == Step.PrepareBuildPlayer, G.Module.PlayerBuilderModule.ModuleStateConfig.Json.Applying,
                          G.Module.PlayerBuilderModule.ModuleStateConfig.Json.ErrorMessage);
            G.Module.PlayerBuilderModule.ModuleStateConfig.Json.IsPartOfPipeline = EditorGUILayout.BeginToggleGroup(
                  playerBuilderContent, G.Module.PlayerBuilderModule.ModuleStateConfig.Json.IsPartOfPipeline);
            EditorGUILayout.BeginHorizontal();
            if (creatingNewConfig)
            {
                ShowInputField();
            }
            else
            {
                if (ShowBuildSettingDropdown()) return;
            }
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("ViewToolOrbit"), "查看该文件"), miniButtonOptions))
            { ClickedShowConfigFile(); return; }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Build Number:", labelOptions);
            int buildNum = EditorGUILayout.IntField(G.Module.PlayerBuilderModule.ModuleStateConfig.Json.BuildNumber, inputOptions);
            if (G.Module.PlayerBuilderModule.ModuleStateConfig.Json.BuildNumber != buildNum)
            {
                G.Module.PlayerBuilderModule.ModuleStateConfig.Json.BuildNumber = buildNum;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(30);
            if (GUILayout.Button(new GUIContent("New", "新建配置文件"), buttonOptions))
            { ClickedNew(); return; }
            if (GUILayout.Button(new GUIContent("Save", "保存配置文件"), buttonOptions))
            { ClickedSave(); return; }
            if (GUILayout.Button(new GUIContent("Revert", "重新载入配置文件"), buttonOptions))
            { ClickedRevert(); return; }
            if (GUILayout.Button(new GUIContent("Apply", "应用下面的PlayerSettings"), buttonOptions))
            { ClickedApply(); return; }
            if (GUILayout.Button(new GUIContent("Fetch", "获取当前的PlayerSettings"), buttonOptions))
            { FetchSettings(); return; }
            GUILayout.FlexibleSpace();

            //Run Button
            if (PlayerBuilder.G.Module.StateConfigAvailable)
            {
                if (GUILayout.Button(new GUIContent("Run Pipeline"))) { ClickedRunPipeline(); return; }
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Check", "检查所有勾选的模块配置"))) { ClickedCheckAll(); return; }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void SVNInfo()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(155));
            if (G.Module.SVNUpdateRunner.Available)
            {
                switch (G.Module.SVNUpdateRunner.VersionState)
                {
                    case SVNUpdate.Runner.VersionStateEnum.Unknow:
                        GUILayout.Label(new GUIContent(unknowIcon, "版本信息未知！\n错误信息:\n" + G.Module.SVNUpdateRunner.InfoErrorMessage), iconOptions);
                        break;
                    case SVNUpdate.Runner.VersionStateEnum.Obsolete:
                        GUILayout.Label(new GUIContent(needUpdateIcon, "版本已过时，需要更新！\n详细信息:\n" + G.Module.SVNUpdateRunner.SVNInfo), iconOptions);
                        break;
                    case SVNUpdate.Runner.VersionStateEnum.Latest:
                        GUILayout.Label(new GUIContent(okIcon, "版本已最新！\n详细信息:\n" + G.Module.SVNUpdateRunner.SVNInfo), iconOptions);
                        break;
                    default:
                        break;
                }
                if (G.Module.SVNUpdateModule.ModuleConfig.Json.EnableCheckDiff)
                {
                    switch (G.Module.SVNUpdateRunner.LocalChangeState)
                    {
                        case SVNUpdate.Runner.ChangeStateEnum.Unknow:
                            GUILayout.Label(new GUIContent(unknowIcon, "本地文件改动情况未知！\n错误信息:\n" + G.Module.SVNUpdateRunner.DiffErrorMessage), iconOptions);
                            break;
                        case SVNUpdate.Runner.ChangeStateEnum.Changed:
                            GUILayout.Label(new GUIContent(changeIcon, "本地文件有改动！\n有改动的文件：\n" + G.Module.SVNUpdateRunner.ChangedFiles), iconOptions);
                            break;
                        case SVNUpdate.Runner.ChangeStateEnum.NoChange:
                            GUILayout.Label(new GUIContent(okIcon, "本地文件无改动"), iconOptions);
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                GUILayout.Label(new GUIContent(disableIcon, "SVN不可用！\n错误信息:\n" + G.Module.SVNUpdateRunner.InfoErrorMessage), iconOptions);
            }
            GUILayout.Label(SVNMessage);
            GUILayout.EndHorizontal();
        }

        private void FrontIndicator(bool running, bool applying, string errorMessage)
        {
            GUILayout.Label(!running ? applying ?
                            new GUIContent(warnIcon, "上次运行时发生错误：" + errorMessage) :
                            GUIContent.none :
                            new GUIContent(fingerIcon), iconOptions);
        }

        private void FetchSettings()
        {
            //由于这些功能逻辑上属于PlayerSetting而非TotalControl,因此使用PlayerBuilder.G来访问
            PlayerBuilder.G.Runner.FetchPlayerSettings();
            PlayerBuilder.G.Module.UserConfig.InitAllRepeatList();
            PlayerBuilder.G.Module.IsDirty = true;
        }

        private void ClickedCheckAll()
        {
            if (ReloadAllUserConfigsAndCheck(true))
            {
                G.Module.DisplayDialog("所有勾选的模块检查正常！");
            }
        }
        private void ClickedApply()
        {
            PlayerBuilder.G.Runner.ApplyScriptDefines(EditorUserBuildSettings.activeBuildTarget);
            PlayerBuilder.G.Runner.ApplyPlayerSettings(EditorUserBuildSettings.activeBuildTarget);
        }

        private void ClickedRunPipeline()
        {
            if (ReloadAllUserConfigsAndCheck(false))
            {
                bool ensure = G.Module.DisplayDialog("确定开始运行管线？", "确定", "取消");
                if (ensure)
                {
                    //开始执行
                    currentStep = Step.Start;
                    EditorApplication.update += UpdateForRun;
                }
            }
        }

        //float compilingProgress;
        void UpdateForRun()
        {
            #region 编译等待
            //if (EditorApplication.isCompiling)
            //{
            //    compilingProgress += 0.0002f;
            //    EditorUtility.DisplayProgressBar("Compiling...", null, compilingProgress % 1);
            //    return;
            //}
            //EditorUtility.ClearProgressBar();
            //compilingProgress = 0;
            #endregion

            if (currentStep == Step.None)
            {
                G.Module.DisplayDialog("逻辑错误：不应该执行到这句！");
            }
            RunCurrentSetp();
        }

        private bool ReloadAllUserConfigsAndCheck(bool onlyCheckConfig)
        {
            foreach (var runner in G.Module.Runners)
            {
                if (runner.BaseModule.BaseModuleStateConfig.BaseJson.IsPartOfPipeline)
                {
                    //重新加载用户配置
                    if (runner.BaseModule.ModuleName != G.Module.PlayerBuilderModule.ModuleName)//PlayerBuilder镶嵌在TotalControl中，可以实时获得最新参数，因此不需要重载用户配置
                    {
                        if (!runner.BaseModule.LoadUserConfig()) return false;
                    }
                    //重设CurrentTag
                    ResetCurrentTag(runner.BaseModule);
                    //检查配置
                    try
                    {
                        CommonModule.ClearLogFolderPath();
                        runner.Check(onlyCheckConfig);
                    }
                    catch (EBPCheckFailedException e)
                    {
                        runner.BaseModule.DisplayDialog(e.Message);
                        return false;
                    }
                }
            }
            return true;
        }

        private void ResetCurrentTag(BaseModule baseModule)
        {
            //重设后续步骤的Tag为
            //将被AssetPreprocessor改变的Tag
            if (G.Module.AssetPreprocessorModule.ModuleStateConfig.Json.IsPartOfPipeline)
            {
                baseModule.BaseModuleStateConfig.BaseJson.CurrentTag = G.Module.AssetPreprocessorModule.UserConfig.Json.Tags.ToArray();
            }
            //当前Assets的Tag
            else
            {
                baseModule.BaseModuleStateConfig.BaseJson.CurrentTag = CommonModule.CommonConfig.Json.CurrentAssetTag.ToArray();
            }
        }

        private void RunCurrentSetp()
        {
            try
            {
                switch (currentStep)
                {
                    case Step.Start:
                        scrollPosition.x = 0;
                        startTime = EditorApplication.timeSinceStartup;
                        CommonModule.GenerateLogFolderPath();
                        currentStep = Step.SVNUpdate;
                        break;
                    case Step.SVNUpdate:
                        if (G.Module.SVNUpdateRunner.IsPartOfPipeline)
                        {
                            G.Module.SVNUpdateRunner.Run();
                        }
                        currentStep = Step.PreprocessAssets;
                        break;
                    case Step.PreprocessAssets:
                        var state = G.Module.AssetPreprocessorModule.ModuleStateConfig; //TODO:每个Setp中都是重复的内容，可优化
                        if (state.Json.IsPartOfPipeline)
                        {
                            G.Module.AssetPreprocessorRunner.Run(true);
                        }
                        else if (!string.IsNullOrEmpty(state.Json.CurrentUserConfigName))
                        {
                            state.Load(); //TODO:为什么要load？
                            state.Json.IsPartOfPipeline = false;
                            state.Save();
                        }
                        currentStep = Step.BuildBundles;
                        break;
                    case Step.BuildBundles:
                        var state2 = G.Module.BundleManagerModule.ModuleStateConfig;
                        if (state2.Json.IsPartOfPipeline)
                        {
                            G.Module.BundleManagerRunner.Run(true);
                        }
                        else if (!string.IsNullOrEmpty(state2.Json.CurrentUserConfigName))
                        {
                            state2.Load();
                            state2.Json.IsPartOfPipeline = false;
                            state2.Save();
                        }
                        currentStep = Step.BuildPackages;
                        break;
                    case Step.BuildPackages:
                        var state3 = G.Module.PackageManagerModule.ModuleStateConfig;
                        if (state3.Json.IsPartOfPipeline)
                        {
                            state3.Json.ResourceVersion = G.Module.BundleManagerModule.ModuleStateConfig.Json.ResourceVersion;
                            G.Module.PackageManagerRunner.Run(true);
                        }
                        else if (!string.IsNullOrEmpty(state3.Json.CurrentUserConfigName))
                        {
                            state3.Load();
                            state3.Json.IsPartOfPipeline = false;
                            state3.Save();
                        }
                        currentStep = Step.PrepareBuildPlayer;
                        break;
                    case Step.PrepareBuildPlayer:
                        var state4_pre = G.Module.PlayerBuilderModule.ModuleStateConfig;
                        if (state4_pre.Json.IsPartOfPipeline)
                        {
                            state4_pre.Json.ResourceVersion = G.Module.BundleManagerModule.ModuleStateConfig.Json.ResourceVersion;
                            state4_pre.Json.ClientVersion = G.Module.PackageManagerModule.ModuleStateConfig.Json.ClientVersion;
                            G.Module.PlayerBuilderRunner.Prepare();
                        }
                        currentStep = Step.BuildPlayer;
                        break;
                    case Step.BuildPlayer:
                        var state4 = G.Module.PlayerBuilderModule.ModuleStateConfig;
                        if (state4.Json.IsPartOfPipeline)
                        {
                            G.Module.PlayerBuilderRunner.Run(true);
                        }
                        else if (!string.IsNullOrEmpty(state4.Json.CurrentUserConfigName))
                        {
                            state4.Load();
                            state4.Json.IsPartOfPipeline = false;
                            state4.Save();
                        }
                        currentStep = Step.Finish;
                        break;
                    case Step.Finish:
                        TimeSpan endTime = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - startTime);
                        G.Module.DisplayDialog(string.Format("管线运行成功！用时：{0}时 {1}分 {2}秒", endTime.Hours, endTime.Minutes, endTime.Seconds));
                        currentStep = Step.None;
                        EditorApplication.update -= UpdateForRun;
                        break;
                }
            }
            catch (Exception e)
            {
                TimeSpan endTime = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - startTime);
                BaseModule currentModule = null;
                switch (currentStep)
                {
                    case Step.SVNUpdate:
                        currentModule = G.Module.SVNUpdateModule;
                        break;
                    case Step.PreprocessAssets:
                        currentModule = G.Module.AssetPreprocessorModule;
                        break;
                    case Step.BuildBundles:
                        currentModule = G.Module.BundleManagerModule;
                        break;
                    case Step.BuildPackages:
                        currentModule = G.Module.PackageManagerModule;
                        break;
                    case Step.BuildPlayer:
                        currentModule = G.Module.PlayerBuilderModule;
                        break;
                    default:
                        break;
                }
                string timeInfo = string.Format("用时：{0}时 {1}分 {2}秒\n", endTime.Hours, endTime.Minutes, endTime.Seconds);
                if (currentModule == null)
                {
                    G.Module.DisplayDialog("管线运行时发生错误!" + timeInfo + "错误信息：" + e.Message);
                }
                else
                {
                    currentModule.DisplayRunError(timeInfo);
                }
                currentStep = Step.None;
                EditorApplication.update -= UpdateForRun;
            }
            finally
            {
                G.g.MainWindow.Repaint();
            }
        }
        private void ChangeRootPath(string path)
        {
            CommonModule.ChangeRootPath(path);
            G.Module.LoadAllModules();
            InitSelectedIndex();
            LoadAllModulesUserConfigList();
            ConfigToIndex();
            OnChangeRootPath();
        }

        private void OnChangeRootPath()
        {
            PlayerBuilder.G.Module.IsDirty = false;
        }

        public void OnFocus()
        {
        }

        public void OnDestory()
        {
            SetdownActions();

            if (PlayerBuilder.G.Module.IsDirty)
            {
                bool result = false;
                try
                {
                    result = G.Module.DisplayDialog("当前配置未保存，是否保存并覆盖 \" " +
                        userConfigNames[playerBuilderUserConfigSelectedIndex] + " \" ?", "保存并退出", "直接退出");
                }
                catch { }
                if (result == true)
                {
                    SaveUserConfig();
                }
            }
        }

        private void ShowInputField()
        {
            GUI.SetNextControlName("InputField1");
            string tip = "<输入名称>(回车确定，空串取消)";
            string s = EditorGUILayout.DelayedTextField(tip, dropdownOptions);
            GUI.FocusControl("InputField1");
            s = s.Trim().Replace('\\', '/');
            if (s != tip)
            {
                if (s != "")
                {
                    try
                    {
                        string path = Path.Combine(PlayerBuilder.G.Module.ModuleConfig.UserConfigsFolderPath, s + ".json");
                        if (File.Exists(path))
                            G.Module.DisplayDialog("创建新文件失败，该名称已存在！");
                        else
                        {
                            CreateNewBuildSetting(s, path);
                        }
                    }
                    catch (Exception e)
                    {
                        G.Module.DisplayDialog("创建时发生错误：" + e.Message);
                    }
                }
                creatingNewConfig = false;
            }
        }

        private void ClickedNew()
        {
            creatingNewConfig = true;
            ShowInputField();
        }

        private bool ShowBuildSettingDropdown()
        {
            if (PlayerBuilder.G.Module.IsDirty)
            {
                try
                {
                    userConfigNames[playerBuilderUserConfigSelectedIndex] += "*";
                }
                catch { }
            }
            int selectedBuildSetting_new = EditorGUILayout.Popup(playerBuilderUserConfigSelectedIndex, userConfigNames, dropdownOptions);
            if (PlayerBuilder.G.Module.IsDirty)
            {
                try
                {
                    userConfigNames[playerBuilderUserConfigSelectedIndex] = userConfigNames[playerBuilderUserConfigSelectedIndex].Remove(userConfigNames[playerBuilderUserConfigSelectedIndex].Length - 1);
                }
                catch { }
            }
            if (selectedBuildSetting_new != playerBuilderUserConfigSelectedIndex)
            {
                ChangeUserConfig(selectedBuildSetting_new);
                return true;
            }
            return false;
        }

        private void ClickedRevert()
        {
            ChangeUserConfig(playerBuilderUserConfigSelectedIndex);
        }

        private void CreateNewBuildSetting(string name, string path)
        {
            //新建
            if (!Directory.Exists(CommonModule.CommonConfig.UserConfigsRootPath))
            {
                PlayerBuilder.G.Module.DisplayDialog("创建失败！用户配置根目录不存在：" + CommonModule.CommonConfig.UserConfigsRootPath);
                return;
            }
            //保存
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PlayerBuilder.G.Module.UserConfig.JsonPath = path;
            SaveUserConfig();
            //更新列表
            userConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(PlayerBuilder.G.Module.ModuleConfig.UserConfigsFolderPath);
            //切换
            PlayerBuilder.G.Module.IsDirty = false;
            ChangeUserConfig(userConfigNames.IndexOf(name));
            //用于总控
            //G.g.OnChangeConfigList();
        }

        private void ChangeUserConfig(int selectedUserConfigIndex_new)
        {
            bool ensureLoad = true;
            if (PlayerBuilder.G.Module.IsDirty)
            {
                ensureLoad = G.Module.DisplayDialog("更改未保存，是否要放弃更改？", "放弃保存", "返回");
            }
            if (ensureLoad)
            {
                try
                {
                    var newUserConfig = new PlayerBuilder.Configs.UserConfig();
                    string newUserConfigName = userConfigNames[selectedUserConfigIndex_new] + ".json";
                    newUserConfig.JsonPath = Path.Combine(PlayerBuilder.G.Module.ModuleConfig.UserConfigsFolderPath, newUserConfigName);
                    newUserConfig.Load();
                    //至此加载成功
                    PlayerBuilder.G.Module.ModuleStateConfig.Json.CurrentUserConfigName = newUserConfigName;
                    PlayerBuilder.G.Module.UserConfig = newUserConfig;
                    playerBuilderUserConfigSelectedIndex = selectedUserConfigIndex_new;
                    PlayerBuilder.G.Module.IsDirty = false;
                }
                catch (Exception e)
                {
                    G.Module.DisplayDialog("切换Map配置时发生错误：" + e.Message);
                }
            }
        }
        private void ClickedShowConfigFile()
        {
            string path = "";
            try
            {
                path = Path.Combine(PlayerBuilder.G.Module.ModuleConfig.UserConfigsFolderPath, userConfigNames[playerBuilderUserConfigSelectedIndex] + ".json");
            }
            catch { }
            if (!File.Exists(path))
            {
                path = PlayerBuilder.G.Module.ModuleConfig.UserConfigsFolderPath;
            }
            EditorUtility.RevealInFinder(path);
        }

        private void ClickedSave()
        {
            bool ensure = true;
            if (PlayerBuilder.G.Module.IsDirty && playerBuilderUserConfigSelectedIndex >= 0)
            {
                ensure = G.Module.DisplayDialog("是否保存并覆盖原配置：" + userConfigNames[playerBuilderUserConfigSelectedIndex], "覆盖保存", "取消");
            }
            if (!ensure) return;

            SaveUserConfig();
        }

        private void SaveUserConfig()
        {
            try
            {
                PlayerBuilder.G.Module.UserConfig.Save();

                G.Module.DisplayDialog("保存配置成功！");
                PlayerBuilder.G.Module.IsDirty = false;
            }

            catch (Exception e)
            {
                G.Module.DisplayDialog("保存配置时发生错误：\n" + e.Message);
            }
        }
    }
}