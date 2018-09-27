using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace EazyBuildPipeline.AssetPreprocessor.Editor
{
    public class PreprocessorWindow : EditorWindow, ISerializationCallbackReceiver
    {
        Module module; //仅用于提供给Unity自动序列化
        [Serializable] public class GroupDictionary1 : SerializableDictionary<string, GroupPanel> { }
        [Serializable] public class GroupDictionary2 : SerializableDictionary<string, GroupDictionary1> { }
        [Serializable] public class GroupDictionary3 : SerializableDictionary<string, GroupDictionary2> { }
        float settingPanelHeight = 90;
        Vector2 scrollPosition;
        SettingPanel settingPanel;
        TagsPanel tagsPanel;
        GroupDictionary3 groupPanels;
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
        private void Awake()
        {
            G.Init();

            G.Module.LoadAllConfigs();

            settingPanel = new SettingPanel();
            tagsPanel = new TagsPanel();
            CreateOptionGroupPanels();
            settingPanel.Awake();
            tagsPanel.Awake();
        }
        private void OnEnable()
        {
            tagsPanel.OnToggleChanged += OnToggleChanged;
            foreach (var g1 in groupPanels)
            {
                foreach (var g2 in g1.Value)
                {
                    foreach (var g3 in g2.Value)
                    {
                        g3.Value.OnToggleChanged += OnToggleChanged;
                        g3.Value.OnEnable();
                    }
                }
            }

            settingPanel.OnEnable();
            tagsPanel.OnEnable();
        }
        private void OnDisable()
        {
            settingPanel.OnDisable();
        }
        private void OnDestroy()
        {
            settingPanel.OnDestory();
            G.Clear();
        }

        private void CreateOptionGroupPanels()
        {
            groupPanels = new GroupDictionary3();
            foreach (var group in G.Module.OptionsEnumConfig.Json)
            {
                if (group.Platform != null && group.Platform.Length != 0 && !group.Platform.Contains(currentPlatform.ToLower()))
                {
                    continue;
                }
                string[] s = group.FullGroupName.Split('/');
                string g0 = s.Length > 0 ? s[0] : "";
                if (!groupPanels.ContainsKey(s[0]))
                {
                    groupPanels.Add(g0, new GroupDictionary2());
                }
                string g1 = s.Length > 1 ? s[1] : "";
                if (!groupPanels[g0].ContainsKey(g1))
                {
                    groupPanels[g0].Add(g1, new GroupDictionary1());
                }
                string g2 = s.Length > 2 ? s[2] : "";
                if (!groupPanels[g0][g1].ContainsKey(g2))
                {
                    var optionsPanel = new GroupPanel()
                    {
                        Group = group,
                        Options = (GroupPanel.OptionsDictionary)new GroupPanel.OptionsDictionary().CopyFrom(group.Options.ToDictionary((x) => x, (x) => false)),
                    };
                    optionsPanel.OnToggleChanged += OnToggleChanged;
                    optionsPanel.Awake();
                    groupPanels[g0][g1].Add(g2, optionsPanel);
                }
            }
        }        

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(6, 6, position.width - 12, settingPanelHeight), GUIContent.none, EditorStyles.helpBox);
			settingPanel.OnGUI();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(6, settingPanelHeight + 6 + 3, position.width - 12, position.height - settingPanelHeight - 12 - 3),
                                GUIContent.none, EditorStyles.helpBox);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
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
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
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
                G.Module.IsDirty = true;
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
                            G.Module.IsDirty = true;
                            return;
                        }
                    }
                }
            }
            G.Module.IsDirty = false;
        }

        public void OnBeforeSerialize()
        {
            module = G.Module;
        }

        public void OnAfterDeserialize()
        {
            G.Module = module;
            G.g = new G.GlobalReference();
        }
    }
}
