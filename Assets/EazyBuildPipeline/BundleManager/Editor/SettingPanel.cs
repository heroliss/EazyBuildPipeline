using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.BundleManager.Editor
{
    [Serializable]
    public class SettingPanel
    {
        [SerializeField] int selectedCompressionIndex;
        [SerializeField] int[] selectedIndexs;
        [SerializeField] GUIStyle dropdownStyle;
        [SerializeField] GUIStyle buttonStyle;
        GUILayoutOption[] defaultOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(90) };
        GUILayoutOption[] dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
        GUILayoutOption[] dropdownOptions2 = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(100) };
        GUILayoutOption[] miniButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(24) };
        GUILayoutOption[] labelOptions = new GUILayoutOption[] { GUILayout.MinWidth(40), GUILayout.MaxWidth(110) };
        GUILayoutOption[] inputOptions = new GUILayoutOption[] { GUILayout.Width(40) };

        private void InitStyles()
        {
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
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
                G.configs.DisplayDialog("加载配置文件时发生错误：" + e.Message);
            }
        }
        public void OnEnable()
        {
        }
        public void OnGUI()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
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
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            if (ShowTagsDropdown()) return;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Resource Version:", labelOptions);
            G.configs.CurrentConfig.Json.CurrentResourceVersion = EditorGUILayout.IntField(G.configs.CurrentConfig.Json.CurrentResourceVersion, inputOptions);
            GUILayout.Space(10);
            //压缩选项
            int selectedCompressionIndex_new = EditorGUILayout.Popup(selectedCompressionIndex, G.configs.CompressionEnum, dropdownStyle, dropdownOptions2);
            if (selectedCompressionIndex_new != selectedCompressionIndex)
            {
                G.configs.CurrentConfig.Json.CurrentBuildAssetBundleOptionsValue -= (int)G.configs.CompressionEnumMap[G.configs.CompressionEnum[selectedCompressionIndex]];
                G.configs.CurrentConfig.Json.CurrentBuildAssetBundleOptionsValue += (int)G.configs.CompressionEnumMap[G.configs.CompressionEnum[selectedCompressionIndex_new]];
                selectedCompressionIndex = selectedCompressionIndex_new;
                return;
            }
            if (GUILayout.Button(new GUIContent("Build Bundles"), buttonStyle, defaultOptions))
            { ClickedApply(); return; }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private bool ShowTagsDropdown()
        {
            int[] selectedIndexs_new = new int[G.configs.Common_TagEnumConfig.Tags.Count];
            int i = 0;
            foreach (var tagType in G.configs.Common_TagEnumConfig.Tags.Values)
            {
                selectedIndexs_new[i] = EditorGUILayout.Popup(selectedIndexs[i], tagType, dropdownStyle, dropdownOptions);
                if (selectedIndexs_new[i] != selectedIndexs[i])
                {
                    selectedIndexs[i] = selectedIndexs_new[i];
                    G.configs.CurrentConfig.Json.CurrentTags[i] = tagType[selectedIndexs[i]];
                    return true;
                }
                i++;
            }
            return false;
        }

        private void ClickedApply()
        {
            G.configs.CurrentConfig.Json.CurrentBundleMap = Path.GetFileName(AssetBundleManagement2.AssetBundleModel.BuildMapPath) + ".json"; //TODO: 覆盖当前map文件名，BundleMaster的特殊处理
            G.configs.BundleBuildMapConfig.Json = G.g.mainTab.GetBuildMap_extension(); //从配置现场覆盖当前map
            if (!G.configs.Runner.Check()) return;
            //确认信息
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), G.configs.CurrentConfig.Json.CurrentTags[0], true);
            int optionsValue = G.configs.CurrentConfig.Json.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = G.configs.CurrentConfig.Json.CurrentResourceVersion;
            string tagPath = Path.Combine(G.configs.LocalConfig.BundlesFolderPath, EBPUtility.GetTagStr(G.configs.CurrentConfig.Json.CurrentTags));

            bool ensure = EditorUtility.DisplayDialog("Build Bundles", string.Format("确定应用当前配置？\n\n" +
                "目标平台: {0}\n 输出路径: {1} \n Resources Version: {2} \n 参数: {3}",
                target, tagPath, resourceVersion, optionsValue), "确定", "取消");
            //开始应用          
            if (ensure)
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Build Bundles", "Getting Bunild Maps...", 0);
                    G.configs.Runner.Run();
                    EditorUtility.DisplayDialog("Build Bundles", "创建AssetBundles成功！", "确定");
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
            if (!newConfigs.LoadAllConfigs(rootPath)) return;
            G.configs = newConfigs;
            InitSelectedIndex();
            ConfigToIndex();
            G.configs.LocalConfig.Save();
            HandleApplyingWarning();
        }

        private void InitSelectedIndex()
        {
            selectedCompressionIndex = -1;
            selectedIndexs = new int[G.configs.Common_TagEnumConfig.Tags.Count];
            for (int i = 0; i < selectedIndexs.Length; i++)
            {
                selectedIndexs[i] = -1;
            }
        }

        private void LoadAllConfigs()
        {
            G.configs.LoadAllConfigs();
            InitSelectedIndex();
            ConfigToIndex();
            HandleApplyingWarning();
        }

        private void HandleApplyingWarning()
        {
            if (G.configs.CurrentConfig.Json.Applying)
            {
                EditorUtility.DisplayDialog("提示", "上次创建Bundles时发生错误或被强制中断，可能导致产生的文件不完全或错误，建议重新创建", "确定");
            }
        }

        private void ConfigToIndex()
        {
            if (G.configs.CurrentConfig.Json.CurrentTags == null)
            {
                return;
            }
            int length = G.configs.CurrentConfig.Json.CurrentTags.Length;
            if (length > G.configs.Common_TagEnumConfig.Tags.Count)
            {
                EditorUtility.DisplayDialog("提示", "欲加载的标签种类比全局标签种类多，请检查全局标签类型是否丢失", "确定");
            }
            else if (length < G.configs.Common_TagEnumConfig.Tags.Count)
            {
                string[] originCurrentTags = G.configs.CurrentConfig.Json.CurrentTags;
                G.configs.CurrentConfig.Json.CurrentTags = new string[G.configs.Common_TagEnumConfig.Tags.Count];
                originCurrentTags.CopyTo(G.configs.CurrentConfig.Json.CurrentTags, 0);
            }
            int i = 0;
            foreach (var item in G.configs.Common_TagEnumConfig.Tags.Values)
            {
                selectedIndexs[i] = GetTagIndex(item, G.configs.CurrentConfig.Json.CurrentTags[i], i);
                i++;
            }

            string compressionName = G.configs.CompressionEnumMap.FirstOrDefault(x=>x.Value == (G.configs.CurrentConfig.Json.CompressionOption)).Key;
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
                  s, count, G.configs.CurrentConfig.JsonPath, G.configs.Common_TagEnumConfig.JsonPath), "确定");
            return -1;
        }
    }
}