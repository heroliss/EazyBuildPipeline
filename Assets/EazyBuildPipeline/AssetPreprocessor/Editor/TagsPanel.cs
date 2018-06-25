using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiXuFeng.AssetPreprocessor.Editor
{
    public class TagsPanel
    {
        public Action OnToggleChanged = () => { };
        public bool Dirty;
        public int[] selectedTagIndexs;
        int[] selectedTagIndexs_origin;
        float OptionWidth = 70;
        public void OnEnable()
        {
            Reset();
            Configs.g.OnChangeCurrentConfig += Reset;
        }

        public void Reset()
        {
            Dirty = false;
            PullCurrentTags();
            selectedTagIndexs_origin = selectedTagIndexs.ToArray();
        }

        public void OnGUI(float panelWidth)
        {
            TagDropdownPanel(panelWidth);
        }

        private void TagDropdownPanel(float panelWidth)
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
            int[] selectedTagIndexs_new = new int[Configs.configs.TagEnumConfig.Tags.Count];

            int i = 0;
            foreach (var tagGroup in Configs.configs.TagEnumConfig.Tags.Values)
            {
                selectedTagIndexs_new[i] = EditorGUILayout.Popup(selectedTagIndexs[i], tagGroup,
                    new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 }, GUILayout.Height(25), GUILayout.MaxWidth(OptionWidth));
                if (selectedTagIndexs_new[i] != selectedTagIndexs[i])
                {
                    selectedTagIndexs[i] = selectedTagIndexs_new[i];
                    UpdateCurrentConfig();
                    UpdateDirty();
                    OnToggleChanged();
                }

                sumWidth += width;
                if (sumWidth > panelWidth - width)
                {
                    sumWidth = headSpace;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(headSpace);
                }
                i++;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void UpdateCurrentConfig()
        {
            List<string> tags = new List<string>();
            for (int i = 0; i < selectedTagIndexs.Length; i++)
            {
                int j = selectedTagIndexs[i];
                tags.Add(j == -1 ? "" : Configs.configs.TagEnumConfig.Tags.Values.ToArray()[i][j]);
            }
            Configs.configs.CurrentSavedConfig.Tags = tags;
        }

        private void UpdateDirty()
        {
            for (int i = 0; i < selectedTagIndexs.Length; i++)
            {
                if (selectedTagIndexs[i] != selectedTagIndexs_origin[i])
                {
                    Dirty = true;
                    return;
                }
            }
            Dirty = false;
        }

        private void PullCurrentTags()
        {
            selectedTagIndexs = Enumerable.Repeat(-1, Configs.configs.TagEnumConfig.Tags.Count).ToArray();
            if (Configs.configs.TagEnumConfig.Tags.Count >= Configs.configs.CurrentSavedConfig.Tags.Count)
            {
                for (int i = 0; i < Configs.configs.CurrentSavedConfig.Tags.Count; i++)
                {
                    selectedTagIndexs[i] = GetIndex(Configs.configs.TagEnumConfig.Tags.Values.ToArray()[i], Configs.configs.CurrentSavedConfig.Tags[i], i);
                }
            }
            else if (Configs.configs.TagEnumConfig.Tags.Count < Configs.configs.CurrentSavedConfig.Tags.Count)
            {
                EditorUtility.DisplayDialog("Preprocessor", "全局标签枚举类型少于欲加载的标签配置类型", "确定");
                for (int i = 0; i < Configs.configs.TagEnumConfig.Tags.Count; i++)
                {
                    selectedTagIndexs[i] = GetIndex(Configs.configs.TagEnumConfig.Tags.Values.ToArray()[i], Configs.configs.CurrentSavedConfig.Tags[i], i);
                }
            }
        }

        private int GetIndex(string[] sList, string s, int count)
        {
            for (int i = 0; i < sList.Length; i++)
            {
                if (s == sList[i])
                {
                    return i;
                }
            }
            EditorUtility.DisplayDialog("错误", string.Format("加载保存的配置文件时发生错误：\n欲加载的类型“{0}”"
                  + "不存在于第 {1} 个全局类型枚举中！\n"
                  + "\n请检查配置文件：{2} 和全局类型配置文件：{3}  中的类型名是否匹配",
                  s, count, Configs.configs.TagEnumConfig.Path, Configs.configs.CurrentSavedConfig.Path), "确定");
            return -1;
        }
    }
}
