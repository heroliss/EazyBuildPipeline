using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Assertions;
using System;

namespace EazyBuildPipeline.AssetPreprocessor.Editor
{
    public class PreprocessorWindow : EditorWindow
    {
        float settingPanelHeight = 90;
        Vector2 scrollPosition;
        SettingPanel settingPanel;
        TagsPanel tagsPanel;
        Dictionary<string, Dictionary<string, Dictionary<string, GroupPanel>>> groupPanels;
        string currentPlatform = "";

        [MenuItem("Window/EazyBuildPipeline/AssetPreprocessor")]
        public static void ShowWindow()
        {
            GetWindow<PreprocessorWindow>();
        }
        public PreprocessorWindow()
        {
#if UNITY_IOS
            currentPlatform = "ios";
#elif UNITY_ANDROID
            currentPlatform = "android";
#endif
            titleContent = new GUIContent("Preprocessor");
        }
        private void OnEnable()
        {
            Configs.Init();
            Configs.configs.LoadLocalConfig();
            Configs.configs.LoadAllConfigsByLocalConfig();
            settingPanel = new SettingPanel();
            tagsPanel = new TagsPanel();
            tagsPanel.OnToggleChanged += OnToggleChanged;
            CreateOptionGroupPanels();
            settingPanel.OnEnable();
            tagsPanel.OnEnable();

        }

        private void OnDisable()
        {
            settingPanel.OnDisable();
            Configs.Clear();
        }

        private void CreateOptionGroupPanels()
        {
            groupPanels = new Dictionary<string, Dictionary<string, Dictionary<string, GroupPanel>>>();
            foreach (var group in Configs.configs.OptionsEnumConfig.Groups)
            {
                if (!string.IsNullOrEmpty(group.Platform) && group.Platform.ToLower() != currentPlatform)
                {
                    continue;
                }
                string[] s = group.FullGroupName.Split('/');
                string g0 = s.Length > 0 ? s[0] : "";
                if (!groupPanels.ContainsKey(s[0]))
                {
                    groupPanels.Add(g0, new Dictionary<string, Dictionary<string, GroupPanel>>());
                }
                string g1 = s.Length > 1 ? s[1] : "";
                if (!groupPanels[g0].ContainsKey(g1))
                {
                    groupPanels[g0].Add(g1, new Dictionary<string, GroupPanel>());
                }
                string g2 = s.Length > 2 ? s[2] : "";
                if (!groupPanels[g0][g1].ContainsKey(g2))
                {
                    var optionsPanel = new GroupPanel()
                    {
                        Group = group,
                        Options = group.Options.ToDictionary((x) => x, (x) => false),
                    };
                    optionsPanel.OnToggleChanged += OnToggleChanged;
                    optionsPanel.OnEnable();
                    groupPanels[g0][g1].Add(g2, optionsPanel);
                }
            }
        }        

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(6, 6, position.width - 12, settingPanelHeight), GUIContent.none, EditorStyles.helpBox))
            {
				settingPanel.OnGUI();
			}
            using (new GUILayout.AreaScope(new Rect(6, settingPanelHeight + 6 + 3,
                position.width - 12, position.height - settingPanelHeight - 12 - 3),
                GUIContent.none, EditorStyles.helpBox))
            {
                using (var scrollScope = new GUILayout.ScrollViewScope(scrollPosition))
                {
                    scrollPosition = scrollScope.scrollPosition;

                    ShowTitle("", "Tags", new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold });
                    GUILayout.Space(10);
                    tagsPanel.OnGUI(position.width - 12);

                    foreach (var title1 in groupPanels.Keys)
                    {
                        ShowTitle("", title1, new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold });
                        foreach (var title2 in groupPanels[title1].Keys)
                        {
                            ShowTitle(" ", title2, new GUIStyle(EditorStyles.label) { });
                            foreach (var title3 in groupPanels[title1][title2].Keys)
                            {
                                ShowTitle("   ", title3, new GUIStyle(EditorStyles.label) { });
                                GUILayout.Space(10);
                                groupPanels[title1][title2][title3].OnGUI(position.width - 12);
                            }
                        }
                    }
                }
            }
        }

        private void ShowTitle(string prefix, string title, GUIStyle style)
        {
            if (!string.IsNullOrEmpty(title))
            {
                GUILayout.Space(10);
                GUILayout.Label(prefix + title + ":", style);
            }
        }

        private void OnToggleChanged()
        {
            UpdateDirty();
        }

        private void UpdateDirty()
        {
            if (tagsPanel.Dirty)
            {
                Configs.configs.Dirty = true;
                return;
            }
            foreach (var t1 in groupPanels.Values)
            {
                foreach (var t2 in t1.Values)
                {
                    foreach (var t3 in t2.Values)
                    {
                        if (t3.Dirty)
                        {
                            Configs.configs.Dirty = true;
                            return;
                        }
                    }
                }
            }
            Configs.configs.Dirty = false;
        }
    }
}
