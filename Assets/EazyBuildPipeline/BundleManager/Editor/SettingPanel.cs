﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.BundleManager.Editor
{
    public class SettingPanel
    {
        int selectedCompressionIndex;
        int[] selectedIndexs;
        GUIStyle dropdownStyle;
        GUIStyle buttonStyle;
        GUILayoutOption[] defaultOptions;
        private GUILayoutOption[] dropdownOptions;
        private GUILayoutOption[] dropdownOptions2;
        private GUILayoutOption[] miniButtonOptions;
        private GUILayoutOption[] labelOptions;
        private GUILayoutOption[] inputOptions;

        private void InitStyles()
        {
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            defaultOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(90) };
            dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
            dropdownOptions2 = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(100) };
            miniButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(24) };
            labelOptions = new GUILayoutOption[] { GUILayout.MinWidth(40), GUILayout.MaxWidth(110) };
            inputOptions = new GUILayoutOption[] { GUILayout.Width(40) };

        }

        public void Awake()
        {
			InitStyles();
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
            using (new EditorGUILayout.HorizontalScope())
            {
                if (ShowTagsDropdown()) return;
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Resource Version:", labelOptions);
                G.configs.BundleManagerConfig.CurrentResourceVersion = EditorGUILayout.IntField(G.configs.BundleManagerConfig.CurrentResourceVersion, inputOptions);
                EditorGUILayout.LabelField("  Bundle Version:", labelOptions);
                G.configs.BundleManagerConfig.CurrentBundleVersion = EditorGUILayout.IntField(G.configs.BundleManagerConfig.CurrentBundleVersion, inputOptions);
                GUILayout.Space(10);
                //压缩选项
                int selectedCompressionIndex_new = EditorGUILayout.Popup(selectedCompressionIndex, G.configs.CompressionEnum, dropdownStyle, dropdownOptions2);
                if (selectedCompressionIndex_new != selectedCompressionIndex)
                {
                    G.configs.BundleManagerConfig.CurrentBuildAssetBundleOptionsValue -= (int)G.configs.CompressionEnumMap[G.configs.CompressionEnum[selectedCompressionIndex]];
                    G.configs.BundleManagerConfig.CurrentBuildAssetBundleOptionsValue += (int)G.configs.CompressionEnumMap[G.configs.CompressionEnum[selectedCompressionIndex_new]];
                    selectedCompressionIndex = selectedCompressionIndex_new;
                    return;
                }
				if (GUILayout.Button(new GUIContent("Build Bundles"), buttonStyle, defaultOptions))
				{ ClickedApply(); return; }
            }
            GUILayout.FlexibleSpace();
        }

        private bool ShowTagsDropdown()
        {
            int[] selectedIndexs_new = new int[G.configs.TagEnumConfig.Tags.Count];
            int i = 0;
            foreach (var tagType in G.configs.TagEnumConfig.Tags.Values)
            {
                selectedIndexs_new[i] = EditorGUILayout.Popup(selectedIndexs[i], tagType, dropdownStyle, dropdownOptions);
                if (selectedIndexs_new[i] != selectedIndexs[i])
                {
                    selectedIndexs[i] = selectedIndexs_new[i];
                    G.configs.BundleManagerConfig.CurrentTags[i] = tagType[selectedIndexs[i]];
                    G.g.OnChangeTags();
                    return true;
                }
                i++;
            }
            return false;
        }

        private void ClickedApply()
        {
            //准备参数和验证
            BuildTarget target = BuildTarget.NoTarget;
            string targetStr = G.configs.BundleManagerConfig.CurrentTags[0];
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetStr, true);
            }
            catch
            {
                EditorUtility.DisplayDialog("Build Bundles", "没有此平台：" + targetStr, "确定");
                return;
            }
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                EditorUtility.DisplayDialog("Build Bundles", string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target), "确定");
                return;
            }
            int optionsValue = G.configs.BundleManagerConfig.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = G.configs.BundleManagerConfig.CurrentResourceVersion;
            int bundleVersion = G.configs.BundleManagerConfig.CurrentBundleVersion;
            string tagPath = Path.Combine(G.configs.LocalConfig.BundlesFolderPath, G.configs.Tag);

            //开始应用          
            bool ensure = EditorUtility.DisplayDialog("Build Bundles", string.Format("确定应用当前配置？\n\n" +
                "目标平台: {0}\n 输出路径: {1} \n Resources Version: {2} \n Bundle Version: {3}\n 参数: {4}",
                target, tagPath, resourceVersion, bundleVersion, optionsValue), "确定", "取消");
            if (ensure)
            {
                try
                {
                    G.configs.BundleManagerConfig.CurrentBundleMap = Path.GetFileName(AssetBundleManagement2.AssetBundleModel.BuildMapPath); //TODO:BundleMaster的特殊处理
                    G.configs.BundleManagerConfig.Applying = true;
                    G.configs.BundleManagerConfig.Save();
                    EditorUtility.DisplayProgressBar("Build Bundles", "Getting Bunild Maps...", 0);
                    var buildMap = G.g.mainTab.GetBuildMap_extension();
                    G.configs.runner.Apply(buildMap, target, tagPath, resourceVersion, bundleVersion, optionsValue);
                    G.configs.BundleManagerConfig.Applying = false;
                    G.configs.BundleManagerConfig.Save();
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Build Bundles", "创建AssetBundles时发生错误：" + e.Message, "确定");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

		private void ChangeRootPath(string path)
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

        private void ChangeAllConfigsExceptRef(string rootPath)
        {
            //使用newConfigs加载确保发生异常后不修改原configs
            Configs.Configs newConfigs = new Configs.Configs();
            if (!newConfigs.LoadLocalConfig()) return;
            newConfigs.LocalConfig.RootPath = rootPath;
            if (!newConfigs.LoadAllConfigsByLocalConfig()) return;
            G.configs = newConfigs;
            InitSelectedIndex();
            ConfigToIndex();
            G.configs.LocalConfig.Save();
            HandleApplyingWarning();
            G.g.OnChangeRootPath();
        }

        private void InitSelectedIndex()
        {
            selectedCompressionIndex = -1;
            selectedIndexs = new int[G.configs.TagEnumConfig.Tags.Count];
            for (int i = 0; i < selectedIndexs.Length; i++)
            {
                selectedIndexs[i] = -1;
            }
        }

        private void LoadAllConfigs()
        {
            G.configs.LoadLocalConfig();
            G.configs.LoadAllConfigsByLocalConfig();
            InitSelectedIndex();
            ConfigToIndex();
            HandleApplyingWarning();
            G.g.OnChangeRootPath();
        }

        private void HandleApplyingWarning()
        {
            if (G.configs.BundleManagerConfig.Applying)
            {
                EditorUtility.DisplayDialog("提示", "即将打开的这个配置在上次应用时被异常中断（可能是死机，停电等原因）" +
                    "，建议重新应用该配置", "确定");
            }
        }

        private void ConfigToIndex()
        {
            if (G.configs.BundleManagerConfig.CurrentTags == null)
            {
                return;
            }
            int length = G.configs.BundleManagerConfig.CurrentTags.Length;
            if (length > G.configs.TagEnumConfig.Tags.Count)
            {
                EditorUtility.DisplayDialog("提示", "欲加载的标签种类比全局标签种类多，请检查全局标签类型是否丢失", "确定");
            }
            else if (length < G.configs.TagEnumConfig.Tags.Count)
            {
                string[] originCurrentTags = G.configs.BundleManagerConfig.CurrentTags;
                G.configs.BundleManagerConfig.CurrentTags = new string[G.configs.TagEnumConfig.Tags.Count];
                originCurrentTags.CopyTo(G.configs.BundleManagerConfig.CurrentTags, 0);
            }
            int i = 0;
            foreach (var item in G.configs.TagEnumConfig.Tags.Values)
            {
                selectedIndexs[i] = GetTagIndex(item, G.configs.BundleManagerConfig.CurrentTags[i], i);
                i++;
            }

            string compressionName = G.configs.CompressionEnumMap.FirstOrDefault(x=>x.Value == (G.configs.BundleManagerConfig.CompressionOption)).Key;
            selectedCompressionIndex = G.configs.CompressionEnum.IndexOf(compressionName);
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
                  + "不存在于第 {1} 个全局类型枚举中！\n"
                  + "\n请检查配置文件：{2} 和全局类型配置文件：{3}  中的类型名是否匹配",
                  s, count, G.configs.BundleManagerConfig.Path, G.configs.TagEnumConfig.Path), "确定");
            return -1;
        }
    }
}