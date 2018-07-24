using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    [Serializable]
    public class SettingPanel
    {
        [SerializeField] string[] assetPreprocessorSavedConfigNames;
        [SerializeField] string[] bundleManagerSavedConfigNames;
        [SerializeField] string[] packageManagerSavedConfigNames;
        [SerializeField] int assetPreprocessorSavedConfigSelectedIndex;
        [SerializeField] int bundleManagerSavedConfigSelectedIndex;
        [SerializeField] int packageManagerSavedConfigSelectedIndex;
        [SerializeField] int selectedCompressionIndex;
        [SerializeField] int[] selectedTagIndexs;

        [SerializeField] GUIStyle toggleStyle;
        [SerializeField] GUIContent settingGUIContent;
        [SerializeField] GUIStyle dropdownStyle;
        [SerializeField] GUIStyle buttonStyle;
        [SerializeField] GUIStyle labelStyle;
        [SerializeField] Texture2D settingIcon;
        [SerializeField] Texture2D warnIcon;

        [SerializeField] GUIContent assetPreprocessorContent;
        [SerializeField] GUIContent bundleManagerContent;
        [SerializeField] GUIContent packageManagerContent;
        [SerializeField] GUIContent buildSettingsContent;
        [SerializeField] GUIContent assetPreprocessorWarnContent;
        [SerializeField] GUIContent bundleManagerWarnContent;
        [SerializeField] GUIContent packageManagerWarnContent;
        [SerializeField] GUIContent buildSettingsWarnContent;

        GUILayoutOption[] dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(22), GUILayout.MaxWidth(160) };
        GUILayoutOption[] dropdownOptions2 = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(100) };
        GUILayoutOption[] tagDropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(22), GUILayout.MaxWidth(80) };
        GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.MaxHeight(22), GUILayout.MaxWidth(70) };
        GUILayoutOption[] labelOptions = new GUILayoutOption[] { GUILayout.MinWidth(20), GUILayout.MaxWidth(110) };
        GUILayoutOption[] labelOptions2 = new GUILayoutOption[] { GUILayout.MinWidth(20), GUILayout.MaxWidth(135), GUILayout.MaxHeight(22) };
        GUILayoutOption[] shortLabelOptions = new GUILayoutOption[] { GUILayout.MinWidth(20), GUILayout.MaxWidth(40) };
        GUILayoutOption[] miniButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(22) };
        GUILayoutOption[] inputOptions = new GUILayoutOption[] { GUILayout.Width(40) };

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
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            labelStyle = new GUIStyle(EditorStyles.label) { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };
            toggleStyle = new GUIStyle("Toggle") { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };

            settingGUIContent = new GUIContent(settingIcon);
            assetPreprocessorContent = new GUIContent("Preprocess Assets:");
            bundleManagerContent = new GUIContent("Build Bundles:");
            packageManagerContent = new GUIContent("Build Packages:");
            buildSettingsContent = new GUIContent("Build Scenes:");
            assetPreprocessorWarnContent = new GUIContent("Preprocess Assets:", warnIcon, "上次应用配置时发生错误或被强制中断，可能导致对Unity内的文件替换不完全或错误、对meta文件的修改不完全或错误，建议还原meta文件、重新应用配置。");
            bundleManagerWarnContent = new GUIContent("Build Bundles:", warnIcon, "上次创建Bundles时发生错误或被强制中断，可能导致产生的文件不完全或错误，建议重新创建");
            packageManagerWarnContent = new GUIContent("Build Packages:", warnIcon, "上次创建Packages时发生错误或被强制中断，可能导致产生不完整或错误的压缩包、在StreamingAssets下产生不完整或错误的文件，建议重新创建。");
            buildSettingsWarnContent = new GUIContent("Build Scenes:", warnIcon, "上次执行打包时发生错误或被强制中断，建议重新打包。");
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
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载Icon时发生错误：" + e.Message, "确定");
            }
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
            try
            {
                assetPreprocessorSavedConfigSelectedIndex = assetPreprocessorSavedConfigNames.IndexOf(G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.CurrentSavedConfigName.RemoveExtension());
                bundleManagerSavedConfigSelectedIndex = bundleManagerSavedConfigNames.IndexOf(G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleMap.RemoveExtension());
                packageManagerSavedConfigSelectedIndex = packageManagerSavedConfigNames.IndexOf(G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentPackageMap.RemoveExtension());
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
            }
            GUILayout.FlexibleSpace();

            //AssetPreprocessor
            using (new GUILayout.HorizontalScope())
            {
                G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline = GUILayout.Toggle(G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline, GUIContent.none);
                GUILayout.Label(G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.Applying ?
                                assetPreprocessorWarnContent : assetPreprocessorContent, labelStyle, labelOptions2);
                int index_new = EditorGUILayout.Popup(assetPreprocessorSavedConfigSelectedIndex, assetPreprocessorSavedConfigNames, dropdownStyle, dropdownOptions);
                if (assetPreprocessorSavedConfigSelectedIndex != index_new)
                {
                    assetPreprocessorSavedConfigSelectedIndex = index_new;
                    G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.CurrentSavedConfigName = assetPreprocessorSavedConfigNames[index_new] + ".json";
                    G.configs.AssetPreprocessorConfigs.LoadCurrentSavedConfig();
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
                }
                GUILayout.Space(10);
                GUILayout.Label(G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline ?
                    "→ " + EBPUtility.GetTagStr(G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Json.Tags) : EBPUtility.GetTagStr(G.configs.Common_AssetsTagsConfig.Json));
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();

            //BundleManager
            using (new GUILayout.HorizontalScope())
            {
                G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline = GUILayout.Toggle(G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline, GUIContent.none);
                GUILayout.Label(G.configs.BundleManagerConfigs.CurrentConfig.Json.Applying ?
                                bundleManagerWarnContent : bundleManagerContent, labelStyle, labelOptions2);
                int index_new = EditorGUILayout.Popup(bundleManagerSavedConfigSelectedIndex, bundleManagerSavedConfigNames, dropdownStyle, dropdownOptions);
                if (bundleManagerSavedConfigSelectedIndex != index_new)
                {
                    bundleManagerSavedConfigSelectedIndex = index_new;
                    G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleMap = bundleManagerSavedConfigNames[index_new] + ".json";
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
                }
                GUILayout.Space(10);

                int selectedCompressionIndex_new = EditorGUILayout.Popup(selectedCompressionIndex, G.configs.BundleManagerConfigs.CompressionEnum, dropdownStyle, dropdownOptions2);
                if (selectedCompressionIndex_new != selectedCompressionIndex)
                {
                    G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBuildAssetBundleOptionsValue -= (int)G.configs.BundleManagerConfigs.CompressionEnumMap[G.configs.BundleManagerConfigs.CompressionEnum[selectedCompressionIndex]];
                    G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBuildAssetBundleOptionsValue += (int)G.configs.BundleManagerConfigs.CompressionEnumMap[G.configs.BundleManagerConfigs.CompressionEnum[selectedCompressionIndex_new]];
                    selectedCompressionIndex = selectedCompressionIndex_new;
                    return;
                }

                EditorGUILayout.LabelField("Resource Version:", labelStyle, labelOptions);
                int n = EditorGUILayout.IntField(G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentResourceVersion, inputOptions);
                if (G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentResourceVersion != n)
                {
                    G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentResourceVersion = n;
                }

                EditorGUILayout.LabelField("  Bundle Version:", labelStyle, labelOptions);
                n = EditorGUILayout.IntField(G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleVersion, inputOptions);
                if (G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleVersion != n)
                {
                    G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleVersion = n;
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();

            //PackageManager
            using (new GUILayout.HorizontalScope())
            {
                G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline = GUILayout.Toggle(G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline, GUIContent.none);
                GUILayout.Label(G.configs.PackageManagerConfigs.CurrentConfig.Json.Applying ?
                                packageManagerWarnContent : packageManagerContent, labelStyle, labelOptions2);
                int index_new = EditorGUILayout.Popup(packageManagerSavedConfigSelectedIndex, packageManagerSavedConfigNames, dropdownStyle, dropdownOptions);
                if (packageManagerSavedConfigSelectedIndex != index_new)
                {
                    packageManagerSavedConfigSelectedIndex = index_new;
                    G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentPackageMap = packageManagerSavedConfigNames[index_new] + ".json";
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
                }
                GUILayout.Space(10);

                EditorGUILayout.LabelField("Addon Version:", labelStyle, labelOptions);
                string packageVersion_new = EditorGUILayout.TextField(G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentAddonVersion);
                if (G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentAddonVersion != packageVersion_new)
                {
                    G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentAddonVersion = packageVersion_new;
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();

            //SceneBuilder
            using (new GUILayout.HorizontalScope())
            {
                G.configs.CurrentConfig.Json.IsPartOfPipeline = GUILayout.Toggle(G.configs.CurrentConfig.Json.IsPartOfPipeline, GUIContent.none);
                GUILayout.Label(G.configs.CurrentConfig.Json.Applying ?
                               buildSettingsWarnContent : buildSettingsContent, labelStyle, labelOptions2);
                //EditorGUILayout.Popup(3, assetPreprocessorSavedConfigNames, dropdownStyle, dropdownOptions);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Run"), buttonStyle, buttonOptions))
                { ClickedApply(); return; }
            }
            GUILayout.FlexibleSpace();
        }

        private void ClickedApply()
        {
            //更换后续步骤的Tags改为将被AssetPreprocessor改变的Tags或当前的AssetsTags
            if (G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline)
            {
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentTags = G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Json.Tags.ToArray();
                G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentTags = G.configs.AssetPreprocessorConfigs.CurrentSavedConfig.Json.Tags.ToArray();
            }
            else
            {
                G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentTags = G.configs.Common_AssetsTagsConfig.Json.ToArray();
                G.configs.PackageManagerConfigs.CurrentConfig.Json.CurrentTags = G.configs.Common_AssetsTagsConfig.Json.ToArray();
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
            bool ensure = EditorUtility.DisplayDialog("运行Pipeline", "确定开始运行管线？", "确定", "取消");
            if (!ensure) return;

            //开始执行
            try
            {
                float startTime = Time.realtimeSinceStartup;
                //AssetPreprocessor
                if (G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline)
                {
                    G.configs.AssetPreprocessorConfigs.Runner.ApplyOptions(true);
                }
                else
                {
                    G.configs.AssetPreprocessorConfigs.CurrentConfig.Load();
                    G.configs.AssetPreprocessorConfigs.CurrentConfig.Json.IsPartOfPipeline = false;
                    G.configs.AssetPreprocessorConfigs.CurrentConfig.Save();
                }
                //BundleManager
                if (G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline)
                {
                    G.configs.BundleManagerConfigs.Runner.Apply(true);
                }
                else
                {
                    G.configs.BundleManagerConfigs.CurrentConfig.Load();
                    G.configs.BundleManagerConfigs.CurrentConfig.Json.IsPartOfPipeline = false;
                    G.configs.BundleManagerConfigs.CurrentConfig.Save();
                }
                //PackageManager
                if (G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline)
                {
                    G.configs.PackageManagerConfigs.Runner.ApplyAllPackages(
                        G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentBundleVersion, 
                        G.configs.BundleManagerConfigs.CurrentConfig.Json.CurrentResourceVersion, 
                        true);
                }
                else
                {
                    G.configs.PackageManagerConfigs.CurrentConfig.Load();
                    G.configs.PackageManagerConfigs.CurrentConfig.Json.IsPartOfPipeline = false;
                    G.configs.PackageManagerConfigs.CurrentConfig.Save();
                }
                //BuildPlayer
                if (G.configs.CurrentConfig.Json.IsPartOfPipeline)
                {
                    //bool compileOccoured = false;
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(BuildTarget.iOS), "aaa;bbb,ccc");

                    //float process = 0;
                    //float lastTime = 0;
                    //while (EditorApplication.isCompiling)
                    //{
                    //    if (Time.realtimeSinceStartup - lastTime > 0.1f)
                    //    {
                    //        compileOccoured = true;
                    //        EditorUtility.DisplayProgressBar("正在编译...", "", process++ % 100);
                    //        lastTime = Time.realtimeSinceStartup;
                    //    }
                    //}
                    //if (compileOccoured == false)
                    //{
                    //    G.configs.DisplayDialog("没有发生编译!");
                    //}
                    //Debug.Log("完成");
                    //G.configs.Runner.Apply(true);
                }
                else
                {
                    G.configs.CurrentConfig.Load();
                    G.configs.CurrentConfig.Json.IsPartOfPipeline = false;
                    G.configs.CurrentConfig.Save();
                }
                TimeSpan time = TimeSpan.FromSeconds(Time.realtimeSinceStartup - startTime);
                G.configs.DisplayDialog("全部完成！用时：" + string.Format("{0}时 {1}分 {2}秒", time.Hours, time.Minutes, time.Seconds));
            }
            catch (Exception e)
            {
                G.configs.DisplayDialog("管线运行时发生错误：" + e.Message);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                
            }
        }

        //private static Action OnScriptReloadedAction;
        //[UnityEditor.Callbacks.DidReloadScripts]
        //private static void OnScriptReloaded()
        //{
        //    Debug.Log("eeeee");
        //    if (OnScriptReloadedAction != null)
        //    {
        //        OnScriptReloadedAction();
        //    }
        //}
        
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
            SetdownActions();
        }
    }
}