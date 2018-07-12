﻿using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    public class SettingPanel
    {
        string[] assetPreprocessorSavedConfigNames;
        string[] bundleManagerSavedConfigNames;
        string[] packageManagerSavedConfigNames;
        int assetPreprocessorSavedConfigSelectedIndex;
        int bundleManagerSavedConfigSelectedIndex;
        int packageManagerSavedConfigSelectedIndex;
        int selectedCompressionIndex;
        int[] selectedTagIndexs;

        private GUILayoutOption[] miniButtonOptions;
        private GUIStyle toggleStyle;
        private GUILayoutOption[] toggleOptions;
        private GUILayoutOption[] inputOptions;
        private GUILayoutOption[] labelOptions;
        private GUILayoutOption[] shortLabelOptions;
        private GUIContent settingGUIContent;
        private GUIStyle dropdownStyle;
        private GUIStyle buttonStyle;
        private GUILayoutOption[] buttonOptions;
        private GUILayoutOption[] dropdownOptions;
        private GUILayoutOption[] dropdownOptions2;
        private GUILayoutOption[] tagDropdownOptions;
        private GUIStyle labelStyle;
        private Texture2D settingIcon;
        private Texture2D warnIcon;
        private GUIContent assetPreprocessorWarnContent;
        private GUIContent bundleManagerWarnContent;
        private GUIContent packageManagerWarnContent;

        public void Awake()
        {
            try
            {
                LoadAllConfigs();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载配置文件时发生错误：" + e.Message, "确定");
            }
            SetIcons();
            InitStyles();
        }
        private void InitStyles()
        {
            settingGUIContent = new GUIContent(settingIcon);
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(22), GUILayout.MaxWidth(160) };
            dropdownOptions2 = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(100) };
            tagDropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(22), GUILayout.MaxWidth(80) };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            buttonOptions = new GUILayoutOption[] { GUILayout.MaxHeight(22), GUILayout.MaxWidth(70) };
            labelStyle = new GUIStyle(EditorStyles.label) { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };
            labelOptions = new GUILayoutOption[] { GUILayout.MinWidth(20), GUILayout.MaxWidth(110) };
            shortLabelOptions = new GUILayoutOption[] { GUILayout.MinWidth(20), GUILayout.MaxWidth(40) };
            miniButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(22) };
            toggleStyle = new GUIStyle("Toggle") { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };
            toggleOptions = new GUILayoutOption[] { GUILayout.MinWidth(12), GUILayout.MaxWidth(135), GUILayout.MaxHeight(22) };
            inputOptions = new GUILayoutOption[] { GUILayout.Width(40) };

            assetPreprocessorWarnContent = new GUIContent(warnIcon, "上次应用配置时发生错误或被强制中断，可能导致对Unity内的文件替换不完全或错误、对meta文件的修改不完全或错误，建议还原meta文件、重新应用配置。");
            bundleManagerWarnContent = new GUIContent(warnIcon, "上次创建Bundles时发生错误或被强制中断，可能导致产生的文件不完全或错误，建议重新创建");
            packageManagerWarnContent = new GUIContent(warnIcon, "上次执行打包时发生错误或被强制中断，可能导致产生不完整或错误的压缩包、在StreamingAssets下产生不完整或错误的文件，建议重新打包。");
        }

        private void LoadAllConfigs()
        {
            G.configs.LoadAllConfigs();
            InitSelectedIndex();
            LoadSavedConfigs();
            ConfigToIndex();
            HandleApplyingWarning();
        }

        void SetIcons()
        {
            try
            {
                warnIcon = EditorGUIUtility.FindTexture("console.warnicon.sml");
                settingIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(G.configs.Common_LocalConfig.SettingIconPath);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载Icon时发生错误：" + e.Message, "确定");
            }
        }

        private void HandleApplyingWarning()
        {
        }

        private void ConfigToIndex()
        {
            //if (G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Tags == null)
            //{
            //    return;
            //}
            //int length = G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Tags.Length;
            //if (length > G.configs.AssetPreprocessorConfigs.TagEnumConfig.Tags.Count)
            //{
            //    EditorUtility.DisplayDialog("提示", "欲加载的标签种类比全局标签种类多，请检查全局标签类型是否丢失", "确定");
            //}
            //else if (length < G.configs.AssetPreprocessorConfigs.TagEnumConfig.Tags.Count)
            //{
            //    string[] originCurrentTags = G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Tags;
            //    G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Tags = new string[G.configs.AssetPreprocessorConfigs.TagEnumConfig.Tags.Count];
            //    originCurrentTags.CopyTo(G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Tags, 0);
            //}
            //int i = 0;
            //foreach (var item in G.configs.AssetPreprocessorConfigs.TagEnumConfig.Tags.Values)
            //{
            //    selectedTagIndexs[i] = GetTagIndex(item, G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Tags[i], i);
            //    i++;
            //}

            assetPreprocessorSavedConfigSelectedIndex = assetPreprocessorSavedConfigNames.IndexOf(G.configs.AssetPreprocessorConfigs.CurrentConfig.CurrentSavedConfigName.RemoveExtension());
            bundleManagerSavedConfigSelectedIndex = bundleManagerSavedConfigNames.IndexOf(G.configs.BundleManagerConfigs.CurrentConfig.CurrentBundleMap.RemoveExtension());
            packageManagerSavedConfigSelectedIndex = packageManagerSavedConfigNames.IndexOf(G.configs.PackageManagerConfigs.CurrentConfig.CurrentPackageMap.RemoveExtension());

            string compressionName = G.configs.BundleManagerConfigs.CompressionEnumMap.FirstOrDefault(x => x.Value == (G.configs.BundleManagerConfigs.CurrentConfig.CompressionOption)).Key;
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
        }

        private void InitSelectedIndex()
        {
            selectedCompressionIndex = -1;
            assetPreprocessorSavedConfigSelectedIndex = -1;
            bundleManagerSavedConfigSelectedIndex = -1;
            packageManagerSavedConfigSelectedIndex = -1;
            assetPreprocessorSavedConfigNames = new string[0];
            bundleManagerSavedConfigNames = new string[0];
            packageManagerSavedConfigNames = new string[0];
            selectedTagIndexs = new int[G.configs.AssetPreprocessorConfigs.Common_TagEnumConfig.Tags.Count];
            for (int i = 0; i < selectedTagIndexs.Length; i++)
            {
                selectedTagIndexs[i] = -1;
            }
        }

        public void OnGUI()
        {
            //Root
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Root:", GUILayout.Width(45));
                string path = EditorGUILayout.DelayedTextField(G.configs.LocalConfig.RootPath);
                if (GUILayout.Button("...", miniButtonOptions))
                {
                    path = EditorUtility.OpenFolderPanel("打开根目录", G.configs.LocalConfig.RootPath, null);
                }
                if (!string.IsNullOrEmpty(path) && path != G.configs.LocalConfig.RootPath)
                {
                    ChangeRootPath(path);
                    return;
                }
            }
            GUILayout.FlexibleSpace();

            //AssetPreprocessor
            using (new GUILayout.HorizontalScope())
            {
                if (G.configs.AssetPreprocessorConfigs.CurrentConfig.Applying)
                {
                    GUILayout.Label(assetPreprocessorWarnContent);
                }
                G.configs.AssetPreprocessorConfigs.CurrentConfig.IsPartOfPipeline = GUILayout.Toggle(G.configs.AssetPreprocessorConfigs.CurrentConfig.IsPartOfPipeline, "Preprocess Assets:", toggleStyle, toggleOptions);
                int index_new = EditorGUILayout.Popup(assetPreprocessorSavedConfigSelectedIndex, assetPreprocessorSavedConfigNames, dropdownStyle, dropdownOptions);
                if (assetPreprocessorSavedConfigSelectedIndex != index_new)
                {
                    assetPreprocessorSavedConfigSelectedIndex = index_new;
                    G.configs.AssetPreprocessorConfigs.CurrentConfig.CurrentSavedConfigName = assetPreprocessorSavedConfigNames[index_new] + ".json";
                    G.configs.AssetPreprocessorConfigs.LoadCurrentSavedConfig();
                }
                if (GUILayout.Button(settingGUIContent, miniButtonOptions))
                {
                    AssetPreprocessor.Editor.G.OverrideCurrentSavedConfigName = G.configs.AssetPreprocessorConfigs.CurrentConfig.CurrentSavedConfigName;
                    EditorWindow.GetWindow<AssetPreprocessor.Editor.PreprocessorWindow>();
                }
                GUILayout.Space(10);
                GUILayout.Label(G.configs.AssetPreprocessorConfigs.CurrentConfig.IsPartOfPipeline ?
                    "→ " + EBPUtility.GetTagStr(G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Tags) : EBPUtility.GetTagStr(G.configs.Common_AssetsTagsConfig.CurrentTags));
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();

            //BundleManager
            using (new GUILayout.HorizontalScope())
            {
                if (G.configs.BundleManagerConfigs.CurrentConfig.Applying)
                {
                    GUILayout.Label(bundleManagerWarnContent);
                }
                G.configs.BundleManagerConfigs.CurrentConfig.IsPartOfPipeline = GUILayout.Toggle(G.configs.BundleManagerConfigs.CurrentConfig.IsPartOfPipeline, "Build Bundles:", toggleStyle, toggleOptions);
                int index_new = EditorGUILayout.Popup(bundleManagerSavedConfigSelectedIndex, bundleManagerSavedConfigNames, dropdownStyle, dropdownOptions);
                if (bundleManagerSavedConfigSelectedIndex != index_new)
                {
                    bundleManagerSavedConfigSelectedIndex = index_new;
                    G.configs.BundleManagerConfigs.CurrentConfig.CurrentBundleMap = bundleManagerSavedConfigNames[index_new] + ".json";
                }
                if (GUILayout.Button(settingGUIContent, miniButtonOptions))
                {
                    EditorWindow.GetWindow<BundleManager.Editor.BundleManagerWindow>();
                }
                GUILayout.Space(10);

                int selectedCompressionIndex_new = EditorGUILayout.Popup(selectedCompressionIndex, G.configs.BundleManagerConfigs.CompressionEnum, dropdownStyle, dropdownOptions2);
                if (selectedCompressionIndex_new != selectedCompressionIndex)
                {
                    G.configs.BundleManagerConfigs.CurrentConfig.CurrentBuildAssetBundleOptionsValue -= (int)G.configs.BundleManagerConfigs.CompressionEnumMap[G.configs.BundleManagerConfigs.CompressionEnum[selectedCompressionIndex]];
                    G.configs.BundleManagerConfigs.CurrentConfig.CurrentBuildAssetBundleOptionsValue += (int)G.configs.BundleManagerConfigs.CompressionEnumMap[G.configs.BundleManagerConfigs.CompressionEnum[selectedCompressionIndex_new]];
                    selectedCompressionIndex = selectedCompressionIndex_new;
                    return;
                }

                EditorGUILayout.LabelField("Resource Version:", labelStyle, labelOptions);
                int n = EditorGUILayout.IntField(G.configs.BundleManagerConfigs.CurrentConfig.CurrentResourceVersion, inputOptions);
                if (G.configs.BundleManagerConfigs.CurrentConfig.CurrentResourceVersion != n)
                {
                    G.configs.BundleManagerConfigs.CurrentConfig.CurrentResourceVersion = n;
                }

                EditorGUILayout.LabelField("  Bundle Version:", labelStyle, labelOptions);
                n = EditorGUILayout.IntField(G.configs.BundleManagerConfigs.CurrentConfig.CurrentBundleVersion, inputOptions);
                if (G.configs.BundleManagerConfigs.CurrentConfig.CurrentBundleVersion != n)
                {
                    G.configs.BundleManagerConfigs.CurrentConfig.CurrentBundleVersion = n;
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();

            //PackageManager
            using (new GUILayout.HorizontalScope())
            {
                if (G.configs.PackageManagerConfigs.CurrentConfig.Applying)
                {
                    GUILayout.Label(packageManagerWarnContent);
                }
                G.configs.PackageManagerConfigs.CurrentConfig.IsPartOfPipeline = GUILayout.Toggle(G.configs.PackageManagerConfigs.CurrentConfig.IsPartOfPipeline, "Build Packages:", toggleStyle, toggleOptions);
                int index_new = EditorGUILayout.Popup(packageManagerSavedConfigSelectedIndex, packageManagerSavedConfigNames, dropdownStyle, dropdownOptions);
                if (packageManagerSavedConfigSelectedIndex != index_new)
                {
                    packageManagerSavedConfigSelectedIndex = index_new;
                    G.configs.PackageManagerConfigs.CurrentConfig.CurrentPackageMap = packageManagerSavedConfigNames[index_new] + ".json";
                }
                if (GUILayout.Button(settingGUIContent, miniButtonOptions))
                {
                    PackageManager.Editor.G.OverrideCurrentSavedConfigName = G.configs.PackageManagerConfigs.CurrentConfig.CurrentPackageMap;
                    EditorWindow.GetWindow<PackageManager.Editor.PackageManagerWindow>();
                }
                GUILayout.Space(10);

                EditorGUILayout.LabelField("Addon Version:", labelStyle, labelOptions);
                string packageVersion_new = EditorGUILayout.TextField(G.configs.PackageManagerConfigs.CurrentConfig.CurrentAddonVersion);
                if (G.configs.PackageManagerConfigs.CurrentConfig.CurrentAddonVersion != packageVersion_new)
                {
                    G.configs.PackageManagerConfigs.CurrentConfig.CurrentAddonVersion = packageVersion_new;
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();

            //SceneBuilder
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Toggle(true, "Build Scenes:", toggleStyle, toggleOptions);
                EditorGUILayout.Popup(3, assetPreprocessorSavedConfigNames, dropdownStyle, dropdownOptions);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Run"), buttonStyle, buttonOptions))
                { ClickedApply(); return; }
            }
            GUILayout.FlexibleSpace();
        }

        private void ClickedApply()
        {
            //更换其Tags为AssetsTags
            G.configs.BundleManagerConfigs.CurrentConfig.CurrentTags = G.configs.Common_AssetsTagsConfig.CurrentTags.ToArray();
            G.configs.PackageManagerConfigs.CurrentConfig.CurrentTags = G.configs.Common_AssetsTagsConfig.CurrentTags.ToArray();

            //检查
            if (G.configs.AssetPreprocessorConfigs.CurrentConfig.IsPartOfPipeline &&
                !G.configs.AssetPreprocessorConfigs.Runner.Check()) { return; }
            if (G.configs.BundleManagerConfigs.CurrentConfig.IsPartOfPipeline &&
                !G.configs.BundleManagerConfigs.Runner.Check()) { return; }
            if (G.configs.PackageManagerConfigs.CurrentConfig.IsPartOfPipeline)
            {
                G.configs.PackageManagerConfigs.LoadMap();
                if (!G.configs.PackageManagerConfigs.Runner.Check()) { return; }
            }
            bool ensure = EditorUtility.DisplayDialog("运行Pipeline", "确定开始运行管线？", "确定", "取消");
            if (!ensure) return;

            float startTime = Time.realtimeSinceStartup;

            //AssetPreprocessor
            if (G.configs.AssetPreprocessorConfigs.CurrentConfig.IsPartOfPipeline)
            {
                G.configs.AssetPreprocessorConfigs.Runner.ApplyOptions(true);
            }
            //BundleManager
            if (G.configs.BundleManagerConfigs.CurrentConfig.IsPartOfPipeline)
            {
                //获取BuildMap
                string bundleMapPath = Path.Combine(G.configs.BundleManagerConfigs.LocalConfig.Local_BundleMapsFolderPath, G.configs.BundleManagerConfigs.CurrentConfig.CurrentBundleMap);
                string content = File.ReadAllText(bundleMapPath);
                AssetBundleBuild[] bundleBuildMap = JsonConvert.DeserializeObject<AssetBundleBuild[]>(content);
                
                G.configs.BundleManagerConfigs.Runner.Apply(bundleBuildMap, true);
            }
            //PackageManager
            if (G.configs.PackageManagerConfigs.CurrentConfig.IsPartOfPipeline)
            {
                G.configs.PackageManagerConfigs.Runner.ApplyAllPackages(G.configs.BundleManagerConfigs.CurrentConfig.CurrentBundleVersion, G.configs.BundleManagerConfigs.CurrentConfig.CurrentResourceVersion);
            }

            TimeSpan time = TimeSpan.FromSeconds(Time.realtimeSinceStartup - startTime);
            EditorUtility.ClearProgressBar();
            G.configs.DisplayDialog("全部完成！用时：" + string.Format("{0}时 {1}分 {2}秒", time.Hours, time.Minutes, time.Seconds));
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
            HandleApplyingWarning();
            OnChangeRootPath();
        }

        private void OnChangeRootPath()
        {
        }

        public void OnFocus()
        {
            ConfigToIndex();
        }

        public void OnDestory()
        {

        }
    }
}