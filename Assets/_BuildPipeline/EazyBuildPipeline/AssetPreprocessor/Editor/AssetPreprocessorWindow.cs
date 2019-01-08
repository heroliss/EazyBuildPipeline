using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace EazyBuildPipeline.AssetPreprocessor.Editor
{
    public class AssetPreprocessorWindow : EditorWindow, ISerializationCallbackReceiver
    {
        Module module; //仅用于提供给Unity自动序列化
        Common.Configs.CommonConfig commonConfig; //仅用于提供给Unity自动序列化

        float settingPanelHeight = 90;
        Vector2 scrollPosition;
        SettingPanel settingPanel = new SettingPanel();
        TagsPanel tagsPanel = new TagsPanel();
        ImporterSettingPanel importerSettingPanel = new ImporterSettingPanel();

        [MenuItem("Window/EazyBuildPipeline/AssetPreprocessor")]
        public static void ShowWindow()
        {
            GetWindow<AssetPreprocessorWindow>();
        }
        public AssetPreprocessorWindow()
        {
            titleContent = new GUIContent("Preprocessor");
        }
        private void Awake()
        {
            G.Init();

            CommonModule.LoadCommonConfig();
            G.Module.LoadAllConfigs();

            settingPanel.Awake();
            tagsPanel.Awake();
            importerSettingPanel.Awake();
        }
        private void OnEnable()
        {
            settingPanel.OnEnable();
            tagsPanel.OnEnable();
            importerSettingPanel.OnEnable(this);
        }
        private void OnDisable()
        {
            settingPanel.OnDisable();
            tagsPanel.OnDisable();
            importerSettingPanel.OnDisable();
        }
        private void OnDestroy()
        {
            settingPanel.OnDestory();
            G.Clear();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(6, 6, position.width - 12, settingPanelHeight), GUIContent.none, EditorStyles.helpBox);
			settingPanel.OnGUI();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(6, settingPanelHeight + 6 + 3, position.width - 12, position.height - settingPanelHeight - 12 - 3),
                                GUIContent.none, EditorStyles.helpBox);
            using (new GUILayout.HorizontalScope("flow overlay box"))
            {
                EditorGUILayout.PrefixLabel("Tags:");
                tagsPanel.OnGUI(position.width - 12);
                GUILayout.FlexibleSpace();
            }
            using (new GUILayout.HorizontalScope("flow overlay box"))
            {
                EditorGUILayout.PrefixLabel("Copy File Tags:");
                string copyFileTags_new = EditorGUILayout.TextField(G.Module.UserConfig.Json.CopyFileTags);
                if (copyFileTags_new != G.Module.UserConfig.Json.CopyFileTags)
                {
                    G.Module.UserConfig.Json.CopyFileTags = copyFileTags_new;
                    G.Module.IsDirty = true;
                }
            }
            GUILayout.Space(10);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            importerSettingPanel.OnGUI();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public void OnBeforeSerialize()
        {
            module = G.Module;
            commonConfig = CommonModule.CommonConfig;
        }

        public void OnAfterDeserialize()
        {
            CommonModule.CommonConfig = commonConfig;
            G.Module = module;
            G.Runner = new Runner(module);
            G.g = new G.GlobalReference();
        }
    }
}