using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LiXuFeng.BundleManager.Editor
{
    public class SettingPanel
    {
        bool firstShow = true;
        int[] selectedIndexs;
        GUIStyle dropdownStyle;
        GUIStyle buttonStyle;
        GUILayoutOption[] defaultOptions;
        private GUILayoutOption[] dropdownOptions;
        private GUILayoutOption[] miniButtonOptions;

        private void InitStyles()
        {
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            defaultOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(90) };
            dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
            miniButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(24) };
        }

        public void OnEnable()
        {
            try
            {
                LoadAllConfigs();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载配置文件时发生错误：" + e.Message, "确定");
            }
        }

        public void OnGUI()
        {
            if (firstShow)
            {
                InitStyles();
                firstShow = false;
            }
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("根目录:", GUILayout.Width(45));
                string path = EditorGUILayout.DelayedTextField(Configs.configs.LocalConfig.RootPath);
                if (GUILayout.Button("...", miniButtonOptions))
                {
                    path = EditorUtility.OpenFolderPanel("打开根目录", Configs.configs.LocalConfig.RootPath, null);
                }
                ChangeRootPathIfChanged(path);
            }
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                ShowTagsDropdown();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Build Bundles"), buttonStyle, defaultOptions)) ClickedApply();
            }
            GUILayout.FlexibleSpace();
        }

        private void ShowTagsDropdown()
        {
            int[] selectedIndexs_new = new int[Configs.configs.TagEnumConfig.Tags.Count];
            int i = 0;
            foreach (var tagType in Configs.configs.TagEnumConfig.Tags.Values)
            {
                selectedIndexs_new[i] = EditorGUILayout.Popup(selectedIndexs[i], tagType, dropdownStyle, dropdownOptions);
                if (selectedIndexs_new[i] != selectedIndexs[i])
                {
                    selectedIndexs[i] = selectedIndexs_new[i];
                    Configs.configs.BundleManagerConfig.CurrentTags[i] = tagType[selectedIndexs[i]];
                    Configs.g.OnChangeTags();
                }
                i++;
            }
        }

        private void ClickedApply()
        {
            bool ensure = EditorUtility.DisplayDialog("BundleManager", "确定应用当前配置？", "确定", "取消");
            if (ensure)
            {
                Configs.configs.BundleManagerConfig.Applying = true;
                Configs.configs.BundleManagerConfig.Save();
                Configs.g.Apply();
                Configs.configs.BundleManagerConfig.Applying = false;
                Configs.configs.BundleManagerConfig.Save();
            }
        }

        private void ChangeRootPathIfChanged(string path)
        {
            if (!string.IsNullOrEmpty(path) && path != Configs.configs.LocalConfig.RootPath)
            {
                bool ensure = true;
                //if (Configs.g.bun.Dirty)
                //{
                //    ensure = !EditorUtility.DisplayDialog("改变根目录", "更改未保存，是否要放弃更改？", "返回", "放弃保存");
                //}
                if (ensure)
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
            }
        }

        private void ChangeAllConfigsExceptRef(string rootPath)
        {
            //使用newConfigs加载确保发生异常后不修改原configs
            Config.Configs newConfigs = new Config.Configs();
            newConfigs.LoadLocalConfig();
            newConfigs.LocalConfig.RootPath = rootPath;
            if (!newConfigs.LoadAllConfigsByLocalConfig()) return;
            Configs.configs = newConfigs;
            InitSelectedIndex();
            ConfigToIndex();
            Configs.configs.LocalConfig.Save();
            HandleApplyingWarning();
            Configs.g.OnChangeRootPath();
        }

        private void InitSelectedIndex()
        {
            selectedIndexs = new int[Configs.configs.TagEnumConfig.Tags.Count];
            for (int i = 0; i < selectedIndexs.Length; i++)
            {
                selectedIndexs[i] = -1;
            }
        }

        private void LoadAllConfigs()
        {
            Configs.configs.LoadLocalConfig();
            Configs.configs.LoadAllConfigsByLocalConfig();
            InitSelectedIndex();
            ConfigToIndex();
            HandleApplyingWarning();
            Configs.g.OnChangeRootPath();
        }

        private void HandleApplyingWarning()
        {
            if (Configs.configs.BundleManagerConfig.Applying)
            {
                EditorUtility.DisplayDialog("提示", "即将打开的这个配置在上次应用时被异常中断（可能是死机，停电等原因）" +
                    "，建议重新应用该配置", "确定");
            }
        }

        private void ConfigToIndex()
        {
            if (Configs.configs.BundleManagerConfig.CurrentTags == null)
            {
                return;
            }
            int length = Configs.configs.BundleManagerConfig.CurrentTags.Length;
            if (Configs.configs.BundleManagerConfig.CurrentTags.Length > Configs.configs.TagEnumConfig.Tags.Count)
            {
                length = Configs.configs.TagEnumConfig.Tags.Count;
                EditorUtility.DisplayDialog("提示", "欲加载的标签种类比全局标签种类多，请检查全局标签类型是否丢失", "确定");
            }
            else
            {
                string[] originCurrentTags = Configs.configs.BundleManagerConfig.CurrentTags;
                Configs.configs.BundleManagerConfig.CurrentTags = new string[Configs.configs.TagEnumConfig.Tags.Count];
                originCurrentTags.CopyTo(Configs.configs.BundleManagerConfig.CurrentTags, 0);
            }
            int i = 0;
            foreach (var item in Configs.configs.TagEnumConfig.Tags.Values)
            {
                selectedIndexs[i] = GetIndex(item, Configs.configs.BundleManagerConfig.CurrentTags[i], i);
                i++;
            }
        }

        private int GetIndex(string[] sList, string s, int count)
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
                  + "不存在于第 {1} 个全局类型枚举中！\n"
                  + "\n请检查配置文件：{2} 和全局类型配置文件：{3}  中的类型名是否匹配",
                  s, count, Configs.configs.BundleManagerConfig.Path, Configs.configs.TagEnumConfig.Path), "确定");
            return -1;
        }

        public void OnDisable()
        {

        }
    }
}
