﻿using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EazyBuildPipeline.PipelineTotalControl.Editor
{
    [Serializable]
    public class SettingPanel
    {
        enum Step { None, Start, SVNUpdate, PreprocessAssets, BuildBundles, BuildPackages, BuildPlayer, Finish }
        [SerializeField] Step currentStep = Step.None;
        [SerializeField] double startTime;
        [SerializeField] bool creatingNewConfig;
        [SerializeField] int selectedCompressionIndex;
        [SerializeField] Vector2 scrollPosition;
        [SerializeField] string[] assetPreprocessorSavedConfigNames;
        [SerializeField] string[] bundleManagerSavedConfigNames;
        [SerializeField] string[] packageManagerSavedConfigNames;
        [SerializeField] string[] playerBuilderSavedConfigNames;
        [SerializeField] int assetPreprocessorSavedConfigSelectedIndex;
        [SerializeField] int bundleManagerSavedConfigSelectedIndex;
        [SerializeField] int packageManagerSavedConfigSelectedIndex;
        [SerializeField] int playerBuilderSavedConfigSelectedIndex;

        [SerializeField] GUIStyle labelMidRight;
        [SerializeField] Texture2D settingIcon;
        [SerializeField] Texture2D warnIcon;
        [SerializeField] Texture2D fingerIcon;

        [SerializeField] GUIContent settingGUIContent;
        [SerializeField] GUIContent assetPreprocessorContent;
        [SerializeField] GUIContent bundleManagerContent;
        [SerializeField] GUIContent packageManagerContent;
        [SerializeField] GUIContent playerBuilderContent;
        [SerializeField] GUIContent assetPreprocessorWarnContent;
        [SerializeField] GUIContent bundleManagerWarnContent;
        [SerializeField] GUIContent packageManagerWarnContent;
        [SerializeField] GUIContent playerBuilderWarnContent;

        GUILayoutOption[] dropdownOptions = { GUILayout.Width(150) };
        GUILayoutOption[] dropdownOptions2 = { GUILayout.MaxWidth(100) };
        GUILayoutOption[] buttonOptions = { GUILayout.MaxWidth(60) };
        GUILayoutOption[] labelOptions = { GUILayout.MinWidth(20), GUILayout.MaxWidth(110) };
        GUILayoutOption[] miniButtonOptions = { GUILayout.MaxHeight(18), GUILayout.MaxWidth(22) };
        GUILayoutOption[] inputOptions = { GUILayout.Width(50) };

        public void Awake()
        {
            try
            {
                LoadAllConfigs();
            }
            catch (Exception e)
            {
                G.configs.DisplayDialog("加载所有配置时发生错误：" + e.Message);
            }
            SetIcons();
            InitStyles();
        }
        public void OnEnable()
        {
            SetupActions();
        }
        private void Action_AssetPreprocessor_OnChangeCurrentConfig()
        {
            if (AssetPreprocessor.Editor.G.configs.CurrentConfig.Json.CurrentSavedConfigName == G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.CurrentSavedConfigName)
            {
                G.configs.AssetPreprocessorConfigs.LoadCurrentSavedConfig(); //重新加载
            }
        }
        private void Action_OnChangeConfigList()
        {
            LoadSavedConfigs();
            ConfigToIndex();
        }
        private void SetupActions()
        {
            if (AssetPreprocessor.Editor.G.g != null)
            {
                AssetPreprocessor.Editor.G.g.OnChangeCurrentConfig += Action_AssetPreprocessor_OnChangeCurrentConfig;
                AssetPreprocessor.Editor.G.g.OnChangeConfigList += Action_OnChangeConfigList;
            }
            if (BundleManager.Editor.G.g != null)
            {
                BundleManager.Editor.G.g.OnChangeConfigList += Action_OnChangeConfigList;
            }
            if (PackageManager.Editor.G.g != null)
            {
                PackageManager.Editor.G.g.OnChangeConfigList += Action_OnChangeConfigList;
            }
        }
        private void SetdownActions()
        {
            if (AssetPreprocessor.Editor.G.g != null)
            {
                AssetPreprocessor.Editor.G.g.OnChangeCurrentConfig -= Action_AssetPreprocessor_OnChangeCurrentConfig;
                AssetPreprocessor.Editor.G.g.OnChangeConfigList -= Action_OnChangeConfigList;
            }
            if (BundleManager.Editor.G.g != null)
            {
                BundleManager.Editor.G.g.OnChangeConfigList -= Action_OnChangeConfigList;
            }
            if (PackageManager.Editor.G.g != null)
            {
                PackageManager.Editor.G.g.OnChangeConfigList -= Action_OnChangeConfigList;
            }
        }

        private void InitStyles()
        {
            labelMidRight = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };

            settingGUIContent = new GUIContent(settingIcon);
            assetPreprocessorContent = new GUIContent("Preprocess Assets:");
            bundleManagerContent = new GUIContent("Build Bundles:");
            packageManagerContent = new GUIContent("Build Packages:");
            playerBuilderContent = new GUIContent("Build Player:");
            assetPreprocessorWarnContent = new GUIContent(warnIcon, "上次应用配置时发生错误或被强制中断，可能导致对Unity内的文件替换不完全或错误、对meta文件的修改不完全或错误，建议还原meta文件、重新应用配置。");
            bundleManagerWarnContent = new GUIContent(warnIcon, "上次创建Bundles时发生错误或被强制中断，可能导致产生的文件不完全或错误，建议重新创建");
            packageManagerWarnContent = new GUIContent(warnIcon, "上次创建Packages时发生错误或被强制中断，可能导致产生不完整或错误的压缩包、在StreamingAssets下产生不完整或错误的文件，建议重新创建。");
            playerBuilderWarnContent = new GUIContent(warnIcon, "上次执行打包时发生错误或被强制中断，建议重新打包。");
        }

        private void LoadAllConfigs()
        {
            G.configs.LoadAllConfigs();
            InitSelectedIndex();
            LoadSavedConfigs();
            ConfigToIndex();
        }

        void SetIcons()
        {
            try
            {
                warnIcon = EditorGUIUtility.FindTexture("console.warnicon.sml");
                settingIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(G.configs.Common_LocalConfig.SettingIconPath);
                fingerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(G.configs.Common_LocalConfig.FingerIconPath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载Icon时发生错误：" + e.Message, "确定");
            }
        }

        private void ConfigToIndex()
        {
            try
            {
                assetPreprocessorSavedConfigSelectedIndex = assetPreprocessorSavedConfigNames.IndexOf(G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.CurrentSavedConfigName.RemoveExtension());
                bundleManagerSavedConfigSelectedIndex = bundleManagerSavedConfigNames.IndexOf(G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleMap.RemoveExtension());
                packageManagerSavedConfigSelectedIndex = packageManagerSavedConfigNames.IndexOf(G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentPackageMap.RemoveExtension());
                playerBuilderSavedConfigSelectedIndex = playerBuilderSavedConfigNames.IndexOf(G.configs.PlayerBuilderConfigs.CurrentConfig.Json.CurrentPlayerSettingName.RemoveExtension());

                
            }
            catch { }
            string compressionName = G.configs.BundleManagerConfigs.CompressionEnumMap.FirstOrDefault(x => x.Value == (G.configs.BundleManagerConfigs.CurrentConfig.Json.CompressionOption)).Key;
            selectedCompressionIndex = G.configs.BundleManagerConfigs.CompressionEnum.IndexOf(compressionName);
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
            EditorUtility.DisplayDialog("错误", string.Format("加载配置文件时发生错误：\n欲加载的类型“{0}”"
                  + "不存在于第 {1} 个全局类型枚举中！\n", s, count), "确定");
            return -1;
        }

        private void LoadSavedConfigs()
        {
            assetPreprocessorSavedConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.configs.AssetPreprocessorConfigs.LocalConfig.Local_SavedConfigsFolderPath);
            bundleManagerSavedConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.configs.BundleManagerConfigs.LocalConfig.Local_BundleMapsFolderPath);
            packageManagerSavedConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.configs.PackageManagerConfigs.LocalConfig.Local_PackageMapsFolderPath);
            playerBuilderSavedConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.configs.PlayerBuilderConfigs.LocalConfig.Local_PlayerSettingsFolderPath);
        }

        private void InitSelectedIndex()
        {
            selectedCompressionIndex = -1;
            assetPreprocessorSavedConfigSelectedIndex = -1;
            bundleManagerSavedConfigSelectedIndex = -1;
            packageManagerSavedConfigSelectedIndex = -1;
            playerBuilderSavedConfigSelectedIndex = -1;
            assetPreprocessorSavedConfigNames = new string[0];
            bundleManagerSavedConfigNames = new string[0];
            packageManagerSavedConfigNames = new string[0];
            playerBuilderSavedConfigNames = new string[0];
        }

        public void Update()
        {
            if (currentStep != Step.None)
            {
                RunCurrentSetp();
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Root:", GUILayout.Width(45));
            string path = EditorGUILayout.DelayedTextField(G.configs.LocalConfig.Json.RootPath);
            if (GUILayout.Button("...", miniButtonOptions))
            {
                path = EditorUtility.OpenFolderPanel("打开根目录", G.configs.LocalConfig.Json.RootPath, null);
            }
            if (!string.IsNullOrEmpty(path) && path != G.configs.LocalConfig.Json.RootPath)
            {
                ChangeRootPath(path);
                return;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            //SVN Update     
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(Step.SVNUpdate, false, assetPreprocessorWarnContent);
            EditorGUILayout.BeginToggleGroup("SVN Update", false);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //AssetPreprocessor   
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(Step.PreprocessAssets, G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.Applying, assetPreprocessorWarnContent);
            G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline = EditorGUILayout.BeginToggleGroup(
                assetPreprocessorContent, G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline);

            EditorGUILayout.BeginHorizontal();
            int index_new = EditorGUILayout.Popup(assetPreprocessorSavedConfigSelectedIndex, assetPreprocessorSavedConfigNames, dropdownOptions);
            if (assetPreprocessorSavedConfigSelectedIndex != index_new)
            {
                assetPreprocessorSavedConfigSelectedIndex = index_new;
                G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.CurrentSavedConfigName = assetPreprocessorSavedConfigNames[index_new] + ".json";
                G.configs.AssetPreprocessorConfigs.LoadCurrentSavedConfig();
                return;
            }
            if (GUILayout.Button(settingGUIContent, miniButtonOptions))
            {
                AssetPreprocessor.Editor.G.OverrideCurrentSavedConfigName = G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.CurrentSavedConfigName;
                if (AssetPreprocessor.Editor.G.g == null)
                {
                    EditorWindow.GetWindow<AssetPreprocessor.Editor.PreprocessorWindow>();
                    AssetPreprocessor.Editor.G.g.OnChangeCurrentConfig += Action_AssetPreprocessor_OnChangeCurrentConfig;
                    AssetPreprocessor.Editor.G.g.OnChangeConfigList += Action_OnChangeConfigList;
                }
                else
                {
                    EditorWindow.GetWindow<AssetPreprocessor.Editor.PreprocessorWindow>();
                }
                return;
            }
            GUILayout.Space(10);
            GUILayout.Label(G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline ?
                "→ " + EBPUtility.GetTagStr(G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Json.Tags) : EBPUtility.GetTagStr(G.configs.Common_AssetsTagsConfig.Json));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //BundleManager     
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(Step.BuildBundles, G.configs.BundleManagerConfigs.CurrentConfig.Json.Applying, bundleManagerWarnContent);
            G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline = EditorGUILayout.BeginToggleGroup(
                bundleManagerContent, G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline);
            EditorGUILayout.BeginHorizontal();

            index_new = EditorGUILayout.Popup(bundleManagerSavedConfigSelectedIndex, bundleManagerSavedConfigNames, dropdownOptions);
            if (bundleManagerSavedConfigSelectedIndex != index_new)
            {
                bundleManagerSavedConfigSelectedIndex = index_new;
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleMap = bundleManagerSavedConfigNames[index_new] + ".json";
                return;
            }
            if (GUILayout.Button(settingGUIContent, miniButtonOptions))
            {
                if (BundleManager.Editor.G.g == null)
                {
                    EditorWindow.GetWindow<BundleManager.Editor.BundleManagerWindow>();
                    BundleManager.Editor.G.g.OnChangeConfigList += Action_OnChangeConfigList;
                }
                else
                {
                    EditorWindow.GetWindow<BundleManager.Editor.BundleManagerWindow>();
                }
                return;
            }
            GUILayout.Space(10);

            int selectedCompressionIndex_new = EditorGUILayout.Popup(selectedCompressionIndex, G.configs.BundleManagerConfigs.CompressionEnum, dropdownOptions2);
            if (selectedCompressionIndex_new != selectedCompressionIndex)
            {
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBuildAssetBundleOptionsValue -= (int)G.configs.BundleManagerConfigs.CompressionEnumMap[G.configs.BundleManagerConfigs.CompressionEnum[selectedCompressionIndex]];
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBuildAssetBundleOptionsValue += (int)G.configs.BundleManagerConfigs.CompressionEnumMap[G.configs.BundleManagerConfigs.CompressionEnum[selectedCompressionIndex_new]];
                selectedCompressionIndex = selectedCompressionIndex_new;
                return;
            }

            EditorGUILayout.LabelField("Resource Version:", labelMidRight, labelOptions);
            int n = EditorGUILayout.IntField(G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentResourceVersion, inputOptions);
            if (G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentResourceVersion != n)
            {
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentResourceVersion = n;
            }

            EditorGUILayout.LabelField("  Bundle Version:", labelMidRight, labelOptions);
            n = EditorGUILayout.IntField(G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleVersion, inputOptions);
            if (G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleVersion != n)
            {
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleVersion = n;
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //PackageManager
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(Step.BuildPackages, G.configs.PackageManagerConfigs.CurrentConfig.Json.Applying, packageManagerWarnContent);
            G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline = EditorGUILayout.BeginToggleGroup(
                   packageManagerContent, G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline);
            EditorGUILayout.BeginHorizontal();

            index_new = EditorGUILayout.Popup(packageManagerSavedConfigSelectedIndex, packageManagerSavedConfigNames, dropdownOptions);
            if (packageManagerSavedConfigSelectedIndex != index_new)
            {
                packageManagerSavedConfigSelectedIndex = index_new;
                G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentPackageMap = packageManagerSavedConfigNames[index_new] + ".json";
                return;
            }
            if (GUILayout.Button(settingGUIContent, miniButtonOptions))
            {
                PackageManager.Editor.G.OverrideCurrentSavedConfigName = G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentPackageMap;
                if (PackageManager.Editor.G.g == null)
                {
                    EditorWindow.GetWindow<PackageManager.Editor.PackageManagerWindow>();
                    PackageManager.Editor.G.g.OnChangeConfigList += Action_OnChangeConfigList;
                }
                else
                {
                    EditorWindow.GetWindow<PackageManager.Editor.PackageManagerWindow>();
                }
                return;
            }
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Addon Version:", labelOptions);
            string packageVersion_new = EditorGUILayout.TextField(G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentAddonVersion);
            if (G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentAddonVersion != packageVersion_new)
            {
                G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentAddonVersion = packageVersion_new;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            //BuildPlayer    
            EditorGUILayout.BeginHorizontal();
            FrontIndicator(Step.BuildPlayer, PlayerBuilder.Editor.G.configs.CurrentConfig.Json.Applying, playerBuilderWarnContent);
            PlayerBuilder.Editor.G.configs.CurrentConfig.Json.IsPartOfPipeline = EditorGUILayout.BeginToggleGroup(
                  playerBuilderContent, PlayerBuilder.Editor.G.configs.CurrentConfig.Json.IsPartOfPipeline);
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
            GUILayout.Space(10);
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
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndToggleGroup();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Run Pipeline"))) { ClickedRunPipeline(); return; }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndScrollView();
        }

        private void FrontIndicator(Step step, bool applying, GUIContent warnContent)
        {
            if (currentStep == step)
            {
                GUILayout.Label(fingerIcon, GUILayout.Width(20), GUILayout.Height(20));
            }
            else
            {
                GUILayout.Label(applying ? warnContent : GUIContent.none, GUILayout.Width(20), GUILayout.Height(20));
            }
        }

        private void FetchSettings()
        {
            PlayerBuilder.Editor.G.configs.Runner.FetchPlayerSettings();
            PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.InitAllRepeatList();
            PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.Dirty = true;
        }
        private void ClickedApply()
        {
            PlayerBuilder.Editor.G.configs.Runner.ApplyPlayerSettings();
        }

        private void ClickedRunPipeline()
        {
            //更换后续步骤的Tags改为将被AssetPreprocessor改变的Tags或当前的AssetsTags
            if (G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline)
            {
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentTags = G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Json.Tags.ToArray();
                G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentTags = G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Json.Tags.ToArray();
                //PlayerBuilder用Common_AssetsTags作为其运行时的标签，而非CurrentConfig.Json.CurrentTags（暂时无用）
                G.configs.PlayerBuilderConfigs.CurrentConfig.Json.CurrentTags = G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Json.Tags.ToArray();
                G.configs.PlayerBuilderConfigs.Common_AssetsTagsConfig.Json = G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Json.Tags.ToArray();
            }
            else
            {
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentTags = G.configs.Common_AssetsTagsConfig.Json.ToArray();
                G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentTags = G.configs.Common_AssetsTagsConfig.Json.ToArray();

                G.configs.PlayerBuilderConfigs.CurrentConfig.Json.CurrentTags = G.configs.Common_AssetsTagsConfig.Json.ToArray();
                G.configs.PlayerBuilderConfigs.Common_AssetsTagsConfig.Json = G.configs.Common_AssetsTagsConfig.Json.ToArray();
            }
            //重新加载配置并检查
            if (G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline)
            {
                if (!G.configs.AssetPreprocessorConfigs.LoadCurrentSavedConfig()) return;
                if (!G.configs.AssetPreprocessorConfigs.Runner.Check()) return;
            }
            if (G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline)
            {
                if (!G.configs.BundleManagerConfigs.LoadBundleBuildMap()) return;
                if (!G.configs.BundleManagerConfigs.Runner.Check()) return;
            }
            if (G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline)
            {
                if (!G.configs.PackageManagerConfigs.LoadPackageMap()) return;
                if (!G.configs.PackageManagerConfigs.Runner.Check()) return;
            }
            if (G.configs.PlayerBuilderConfigs.CurrentConfig.Json.IsPartOfPipeline)
            {
                //PlayerBuilder镶嵌在TotalControl中，可以实时获得最新参数，因此不需要重载
                //if (!G.configs.PlayerBuilderConfigs.LoadCurrentPlayerSetting()) return; 
                if (!G.configs.PlayerBuilderConfigs.Runner.Check()) return;
            }
            bool ensure = EditorUtility.DisplayDialog("运行Pipeline", "确定开始运行管线？", "确定", "取消");
            if (!ensure) return;

            //开始执行
            currentStep = Step.Start;
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
                        currentStep = Step.SVNUpdate;
                        break;
                    case Step.SVNUpdate:
                        currentStep = Step.PreprocessAssets;
                        break;
                    case Step.PreprocessAssets:
                        if (G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline)
                        {
                            G.configs.AssetPreprocessorConfigs.Runner.Run(true);
                            G.configs.Common_AssetsTagsConfig.Load();
                        }
                        else
                        {
                            G.configs.AssetPreprocessorConfigs.CurrentConfig.Load();
                            G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline = false;
                            G.configs.AssetPreprocessorConfigs.CurrentConfig.Save();
                        }
                        currentStep = Step.BuildBundles;
                        break;
                    case Step.BuildBundles:
                        if (G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline)
                        {
                            G.configs.BundleManagerConfigs.Runner.Run(true);
                        }
                        else
                        {
                            G.configs.BundleManagerConfigs.CurrentConfig.Load();
                            G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline = false;
                            G.configs.BundleManagerConfigs.CurrentConfig.Save();
                        }
                        currentStep = Step.BuildPackages;
                        break;
                    case Step.BuildPackages:
                        if (G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline)
                        {
                            G.configs.PackageManagerConfigs.Runner.BundleVersion = G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleVersion;
                            G.configs.PackageManagerConfigs.Runner.ResourceVersion = G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentResourceVersion;
                            G.configs.PackageManagerConfigs.Runner.Run(true);
                        }
                        else
                        {
                            G.configs.PackageManagerConfigs.CurrentConfig.Load();
                            G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline = false;
                            G.configs.PackageManagerConfigs.CurrentConfig.Save();
                        }
                        currentStep = Step.BuildPlayer;
                        break;
                    case Step.BuildPlayer:
                        if (G.configs.PlayerBuilderConfigs.CurrentConfig.Json.IsPartOfPipeline)
                        {
                            G.configs.PlayerBuilderConfigs.Runner.Run(true);
                        }
                        else
                        {
                            G.configs.PlayerBuilderConfigs.CurrentConfig.Load();
                            G.configs.PlayerBuilderConfigs.CurrentConfig.Json.IsPartOfPipeline = false;
                            G.configs.PlayerBuilderConfigs.CurrentConfig.Save();
                        }
                        currentStep = Step.Finish;
                        break;
                    case Step.Finish:
                        TimeSpan endTime = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - startTime);
                        G.configs.DisplayDialog(string.Format("管线运行成功！用时：{0}时 {1}分 {2}秒", endTime.Hours, endTime.Minutes, endTime.Seconds));
                        currentStep = Step.None;
                        break;
                }
            }
            catch (Exception e)
            {
                TimeSpan endTime = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - startTime);
                G.configs.DisplayDialog(string.Format("管线运行时发生错误! 用时：{0}时 {1}分 {2}秒 \n错误信息：{3}", endTime.Hours, endTime.Minutes, endTime.Seconds, e.Message));
                currentStep = Step.None;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                G.g.MainWindow.Repaint();
            }
        }
        
        private void ChangeRootPath(string path)
        {
            try
            {
                ChangeAllConfigsExceptRef(path);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "更换根目录时发生错误：" + e.ToString(), "确定");
            }
        }

        private void ChangeAllConfigsExceptRef(string rootPath)
        {
            Configs.Configs newConfigs = new Configs.Configs();
            if (!newConfigs.LoadAllConfigs(rootPath)) return;
            G.configs = newConfigs;
            InitSelectedIndex();
            LoadSavedConfigs();
            ConfigToIndex();
            G.configs.LocalConfig.Save();
            OnChangeRootPath();
        }

        private void OnChangeRootPath()
        {
        }

        public void OnFocus()
        {
        }

        public void OnDestory()
        {
            if (G.configs.PlayerBuilderConfigs.PlayerSettingsConfig.Dirty)
            {
                bool result = false;
                try
                {
                    result = EditorUtility.DisplayDialog(
                        "PlayerBuilder", "当前配置未保存，是否保存并覆盖 \" " +
                        playerBuilderSavedConfigNames[playerBuilderSavedConfigSelectedIndex] + " \" ?", "保存并退出", "直接退出");
                }
                catch { }
                if (result == true)
                {
                    SaveCurrentPlayerSetting();
                }
            }

            SetdownActions();
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
                        string path = Path.Combine(PlayerBuilder.Editor.G.configs.LocalConfig.Local_PlayerSettingsFolderPath, s + ".json");
                        if (File.Exists(path))
                            EditorUtility.DisplayDialog("创建失败", "创建新文件失败，该名称已存在！", "确定");
                        else
                        {
                            CreateNewBuildSetting(s, path);
                        }
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("创建失败", "创建时发生错误：" + e.Message, "确定");
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
            if (PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.Dirty)
            {
                try
                {
                    playerBuilderSavedConfigNames[playerBuilderSavedConfigSelectedIndex] += "*";
                }
                catch { }
            }
            int selectedBuildSetting_new = EditorGUILayout.Popup(playerBuilderSavedConfigSelectedIndex, playerBuilderSavedConfigNames, dropdownOptions);
            if (PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.Dirty)
            {
                try
                {
                    playerBuilderSavedConfigNames[playerBuilderSavedConfigSelectedIndex] = playerBuilderSavedConfigNames[playerBuilderSavedConfigSelectedIndex].Remove(playerBuilderSavedConfigNames[playerBuilderSavedConfigSelectedIndex].Length - 1);
                }
                catch { }
            }
            if (selectedBuildSetting_new != playerBuilderSavedConfigSelectedIndex)
            {
                ChangePlayerSetting(selectedBuildSetting_new);
                return true;
            }
            return false;
        }

        private void ClickedRevert()
        {
            ChangePlayerSetting(playerBuilderSavedConfigSelectedIndex);
        }

        private void CreateNewBuildSetting(string name, string path)
        {
            //新建
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Create(path).Close();
            EditorUtility.DisplayDialog("创建成功", "创建成功!", "确定");
            //更新列表
            playerBuilderSavedConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(PlayerBuilder.Editor.G.configs.LocalConfig.Local_PlayerSettingsFolderPath);
            //保存
            PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.JsonPath = path;
            SaveCurrentPlayerSetting();
            //切换
            ChangePlayerSetting(playerBuilderSavedConfigNames.IndexOf(name));
        }

        private void ChangePlayerSetting(int selectedPlayerSettingIndex_new)
        {
            bool ensureLoad = true;
            if (PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.Dirty)
            {
                ensureLoad = EditorUtility.DisplayDialog("切换配置", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
            }
            if (ensureLoad)
            {
                try
                {
                    var newPlayerSettingConfig = new PlayerBuilder.Editor.Configs.PlayerSettingsConfig();
                    string newPlayerSettingConfigName = playerBuilderSavedConfigNames[selectedPlayerSettingIndex_new] + ".json";
                    newPlayerSettingConfig.JsonPath = Path.Combine(PlayerBuilder.Editor.G.configs.LocalConfig.Local_PlayerSettingsFolderPath, newPlayerSettingConfigName);
                    newPlayerSettingConfig.Load();
                    //至此加载成功
                    PlayerBuilder.Editor.G.configs.CurrentConfig.Json.CurrentPlayerSettingName = newPlayerSettingConfigName;
                    PlayerBuilder.Editor.G.configs.PlayerSettingsConfig = newPlayerSettingConfig;
                    playerBuilderSavedConfigSelectedIndex = selectedPlayerSettingIndex_new;
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("切换map", "切换Map配置时发生错误：" + e.Message, "确定");
                }
            }
        }
        private void ClickedShowConfigFile()
        {
            string path = "";
            try
            {
                path = Path.Combine(PlayerBuilder.Editor.G.configs.LocalConfig.Local_PlayerSettingsFolderPath, playerBuilderSavedConfigNames[playerBuilderSavedConfigSelectedIndex] + ".json");
            }
            catch { }
            if (!File.Exists(path))
            {
                path = PlayerBuilder.Editor.G.configs.LocalConfig.Local_PlayerSettingsFolderPath;
            }
            EditorUtility.RevealInFinder(path);
        }

        private void ClickedSave()
        {
            bool ensure = true;
            if (PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.Dirty && playerBuilderSavedConfigSelectedIndex >= 0)
            {
                ensure = EditorUtility.DisplayDialog("保存", "是否保存并覆盖原配置：" + playerBuilderSavedConfigNames[playerBuilderSavedConfigSelectedIndex], "覆盖保存", "取消");
            }
            if (!ensure) return;

            SaveCurrentPlayerSetting();
        }

        private void SaveCurrentPlayerSetting()
        {
            try
            {
                PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.Save();

                EditorUtility.DisplayDialog("保存", "保存配置成功！", "确定");
                PlayerBuilder.Editor.G.configs.PlayerSettingsConfig.Dirty = false;
            }

            catch (Exception e)
            {
                EditorUtility.DisplayDialog("保存", "保存配置时发生错误：\n" + e.Message, "确定");
            }
        }
    }
}