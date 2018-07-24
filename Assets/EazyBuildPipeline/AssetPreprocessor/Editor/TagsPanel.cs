using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.AssetPreprocessor.Editor
{
    [Serializable]
    public class TagsPanel
    {
        public Action OnToggleChanged = () => { };
        public bool Dirty;
        public int[] selectedTagIndexs;
        [SerializeField] int[] selectedTagIndexs_origin;
        [SerializeField] float OptionWidth = 70;
        [SerializeField] float headSpace = 20;
        [SerializeField] GUIStyle labelStyle;
        [SerializeField] GUIStyle dropDownStyle;

        public void Awake()
        {
            labelStyle = new GUIStyle(EditorStyles.label) { fixedWidth = headSpace - 8 };
            dropDownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            Reset();
        }
        public void OnEnable()
        {
            G.g.OnChangeCurrentConfig += Reset;
        }
        
        public void Reset()
        {
            Dirty = false;
            PullCurrentTags();
            selectedTagIndexs_origin = selectedTagIndexs.ToArray();
        }
 
        public void OnGUI(float panelWidth)
        {
            EditorGUILayout.BeginHorizontal();
            float sumWidth = headSpace;
            float width = OptionWidth;
            if (Dirty)
            {
                GUILayout.Label("*", labelStyle);
            }
            else
            {
                GUILayout.Space(headSpace);
            }
            int[] selectedTagIndexs_new = new int[G.configs.Common_TagEnumConfig.Tags.Count];

            int i = 0;
            foreach (var tagGroup in G.configs.Common_TagEnumConfig.Tags.Values)
            {
                selectedTagIndexs_new[i] = EditorGUILayout.Popup(selectedTagIndexs[i], tagGroup,
                    dropDownStyle, GUILayout.Height(25), GUILayout.MaxWidth(OptionWidth));
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
                tags.Add(j == -1 ? "" : G.configs.Common_TagEnumConfig.Tags.Values.ToArray()[i][j]);
            }
            G.configs.CurrentSavedConfig.Json.Tags = tags.ToArray();
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
            selectedTagIndexs = Enumerable.Repeat(-1, G.configs.Common_TagEnumConfig.Tags.Count).ToArray();
            if (G.configs.Common_TagEnumConfig.Tags.Count >= G.configs.CurrentSavedConfig.Json.Tags.Length)
            {
                for (int i = 0; i < G.configs.CurrentSavedConfig.Json.Tags.Length; i++)
                {
                    selectedTagIndexs[i] = GetIndex(G.configs.Common_TagEnumConfig.Tags.Values.ToArray()[i], G.configs.CurrentSavedConfig.Json.Tags[i], i);
                }
            }
            else if (G.configs.Common_TagEnumConfig.Tags.Count < G.configs.CurrentSavedConfig.Json.Tags.Length)
            {
                EditorUtility.DisplayDialog("Preprocessor", "全局标签枚举类型少于欲加载的标签配置类型", "确定");
                for (int i = 0; i < G.configs.Common_TagEnumConfig.Tags.Count; i++)
                {
                    selectedTagIndexs[i] = GetIndex(G.configs.Common_TagEnumConfig.Tags.Values.ToArray()[i], G.configs.CurrentSavedConfig.Json.Tags[i], i);
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
                  s, count, G.configs.Common_TagEnumConfig.JsonPath, G.configs.CurrentSavedConfig.JsonPath), "确定");
            return -1;
        }
    }
}
