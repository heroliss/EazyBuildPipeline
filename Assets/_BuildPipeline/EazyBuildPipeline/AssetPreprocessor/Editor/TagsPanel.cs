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
            G.g.OnChangeCurrentUserConfig += Reset;
        }
        public void OnDisable()
        {

        }

        public void Reset()
        {
            PullCurrentTags();
            selectedTagIndexs_origin = selectedTagIndexs.ToArray();
        }

        public void OnGUI(float panelWidth)
        {
            EditorGUILayout.BeginHorizontal();
            float sumWidth = headSpace;
            float width = OptionWidth;

            int[] selectedTagIndexs_new = new int[CommonModule.CommonConfig.Json.TagEnum.Count];

            int i = 0;
            foreach (var tagGroup in CommonModule.CommonConfig.Json.TagEnum.Values)
            {
                selectedTagIndexs_new[i] = EditorGUILayout.Popup(selectedTagIndexs[i], tagGroup,
                    dropDownStyle, GUILayout.MaxWidth(OptionWidth));
                if (selectedTagIndexs_new[i] != selectedTagIndexs[i])
                {
                    selectedTagIndexs[i] = selectedTagIndexs_new[i];
                    UpdateCurrentConfig();
                    G.Module.IsDirty = true;
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
                tags.Add(j == -1 ? "" : CommonModule.CommonConfig.Json.TagEnum.Values.ToArray()[i][j]);
            }
            G.Module.UserConfig.Json.Tags = tags.ToArray();
        }

        private void PullCurrentTags()
        {
            selectedTagIndexs = Enumerable.Repeat(-1, CommonModule.CommonConfig.Json.TagEnum.Count).ToArray();
            if (CommonModule.CommonConfig.Json.TagEnum.Count >= G.Module.UserConfig.Json.Tags.Length)
            {
                for (int i = 0; i < G.Module.UserConfig.Json.Tags.Length; i++)
                {
                    selectedTagIndexs[i] = GetIndex(CommonModule.CommonConfig.Json.TagEnum.Values.ToArray()[i], G.Module.UserConfig.Json.Tags[i], i);
                }
            }
            else if (CommonModule.CommonConfig.Json.TagEnum.Count < G.Module.UserConfig.Json.Tags.Length)
            {
                G.Module.DisplayDialog("全局标签枚举类型少于欲加载的标签配置类型");
                for (int i = 0; i < CommonModule.CommonConfig.Json.TagEnum.Count; i++)
                {
                    selectedTagIndexs[i] = GetIndex(CommonModule.CommonConfig.Json.TagEnum.Values.ToArray()[i], G.Module.UserConfig.Json.Tags[i], i);
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
            G.Module.DisplayDialog(string.Format("加载用户配置文件时发生错误：\n欲加载的类型“{0}”"
                  + "不存在于第 {1} 个全局类型枚举中！\n"
                  + "\n请检查配置文件：{2} 和全局类型配置文件：{3}  中的类型名是否匹配",
                  s, count, G.Module.UserConfig.JsonPath, CommonModule.CommonConfig.JsonPath));
            return -1;
        }
    }
}
