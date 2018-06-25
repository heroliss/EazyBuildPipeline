using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace EazyBuildPipeline.AssetPreprocessor.Editor
{
    public class GroupPanel
    {
        public Action OnToggleChanged = () => { };
        public Config.OptionsEnumConfig.Group Group;
        public bool Dirty;
        public float OptionWidth;
        public string SelectedOption;
        string SelectedOption_origin;
        public Dictionary<string, bool> Options;
        Dictionary<string, bool> Options_origin;
        GUILayoutOption[] toggleOptions;

        public void OnEnable()
        {
            InitStyle();
            Reset();
            Configs.g.OnChangeCurrentConfig += Reset;
        }

        public void Reset()
        {
            Dirty = false;
            PullCurrentOptions();
            Options_origin = new Dictionary<string, bool>(Options);
            SelectedOption_origin = SelectedOption;
        }

        private void InitStyle()
        {
            foreach (var optionName in Options.Keys)
            {
                if (optionName.Length * 7 > OptionWidth)
                {
                    OptionWidth = optionName.Length * 6 + 60;
                }
            }
            toggleOptions = new GUILayoutOption[]
            {
                GUILayout.MaxHeight(30),
                GUILayout.MinHeight(20),
                GUILayout.Width(OptionWidth)
            };
        }

        public void OnGUI(float panelWidth)
        {
            TogglePanel(panelWidth);
        }

        private void TogglePanel(float panelWidth)
        {
            float headSpace = 20;
            EditorGUILayout.BeginHorizontal();
            float sumWidth = headSpace;
            float width = OptionWidth;
            if (Dirty)
            {
                GUILayout.Label("*", new GUIStyle(EditorStyles.label) { fixedWidth = headSpace - 8 });
            }
            else
            {
                GUILayout.Space(headSpace);
            }
            foreach (var optionName in Options.Keys.ToArray())
            {
                if (Group.MultiSelect)
                {
                    bool check = EditorGUILayout.ToggleLeft(GetDisplayStr(optionName), Options[optionName], toggleOptions);
                    if (check!= Options[optionName])
                    {
                        Options[optionName] = check;
                        UpdateDirty();
                        UpdateCurrentConfig();
                        OnToggleChanged();
                    }
                }
                else
                {
                    bool selected = SelectedOption == optionName;
                    bool check = EditorGUILayout.ToggleLeft(GetDisplayStr(optionName), selected, toggleOptions);
                    if (check != selected)
                    {
                        SelectedOption = optionName;
                        Dirty = SelectedOption != SelectedOption_origin;
                        UpdateCurrentConfig();
                        OnToggleChanged();
                    }
                    
                }
                sumWidth += width;
                if (sumWidth > panelWidth - width)
                {
                    sumWidth = headSpace;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(headSpace);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void UpdateCurrentConfig()
        {
            var group = Configs.configs.CurrentSavedConfig.Groups.Find(x => x.FullGroupName == Group.FullGroupName);
            if (group == null)
            {
                group = new Config.CurrentSavedConfig.Group
                {
                    FullGroupName = Group.FullGroupName
                };
                Configs.configs.CurrentSavedConfig.Groups.Add(group);
            }
            if (Group.MultiSelect)
            {
                if (Options.Count == 0)
                {
                    Configs.configs.CurrentSavedConfig.Groups.Remove(group);
                }
                else
                {
                    group.Options = new List<string>();
                    foreach (var option in Options.Where(x => x.Value))
                    {
                        group.Options.Add(option.Key);
                    }
                }
            }
            else
            {
                group.Options = new List<string> { SelectedOption };
            }
        }

        private void UpdateDirty()
        {
            foreach (var key in Options.Keys)
            {
                if (Options[key] != Options_origin[key])
                {
                    Dirty = true;
                    return;
                }
            }
            Dirty = false;
        }

        public void PullCurrentOptions()
        {
            try
            {
                foreach (string key in Options.Keys.ToArray())
                {
                    Options[key] = false;
                }
                var group = Configs.configs.CurrentSavedConfig.Groups.Find(x => x.FullGroupName == Group.FullGroupName);
                //if (group.Options != null)
                {
                    if (Group.MultiSelect)
                    {
                        foreach (var option in group.Options.ToArray())
                        {
                            if (Options.ContainsKey(option))
                            {
                                Options[option] = true;
                            }
                            else
                            {
                                group.Options.Remove(option);
                            }
                        }
                    }
                    else
                    {
                        if (Options.ContainsKey(group.Options[0]))
                        {
                            SelectedOption = group.Options[0];
                        }
                        else
                        {
                            group.Options.Remove(group.Options[0]);
                        }
                    }
                }
            }
            catch
            {
                //EditorUtility.DisplayDialog("Preprocessor", "设置配置" + GroupName + "时发生错误：" + e.Message, "确定");
            }
        }

        private static string GetDisplayStr(string s)
        {
            return s.Replace("->", " → ").Replace('_', ' ');
        }
    }
}
