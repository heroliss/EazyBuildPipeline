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
        int[] selectedIndexs_new;

        [SerializeField] GUIStyle dropdownStyle;
        [SerializeField] GUIStyle buttonStyle;
        GUILayoutOption[] defaultOptions = { GUILayout.MaxHeight(25), GUILayout.MaxWidth(90) };
        GUILayoutOption[] dropdownOptions = { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
        GUILayoutOption[] dropdownOptions2 = { GUILayout.MaxHeight(25), GUILayout.MaxWidth(100) };
        GUILayoutOption[] miniButtonOptions = { GUILayout.MaxWidth(24) };
        GUILayoutOption[] labelOptions = { GUILayout.MinWidth(40), GUILayout.MaxWidth(110) };
        GUILayoutOption[] inputOptions = { GUILayout.Width(40) };

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
                G.Module.DisplayDialog("加载配置文件时发生错误：" + e.Message);
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
            string path = EditorGUILayout.DelayedTextField(CommonModule.CommonConfig.Json.PipelineRootPath);
            if (GUILayout.Button("...", miniButtonOptions))
            {
                path = EditorUtility.OpenFolderPanel("打开根目录", CommonModule.CommonConfig.Json.PipelineRootPath, null);
            }
            if (!string.IsNullOrEmpty(path) && path != CommonModule.CommonConfig.Json.PipelineRootPath)
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
            G.Module.ModuleStateConfig.Json.CurrentResourceVersion = EditorGUILayout.IntField(G.Module.ModuleStateConfig.Json.CurrentResourceVersion, inputOptions);
            GUILayout.Space(10);
            //压缩选项
            int selectedCompressionIndex_new = EditorGUILayout.Popup(selectedCompressionIndex, G.Module.CompressionEnum, dropdownStyle, dropdownOptions2);
            if (selectedCompressionIndex_new != selectedCompressionIndex)
            {
                G.Module.ModuleStateConfig.Json.CurrentBuildAssetBundleOptionsValue -= (int)G.Module.CompressionEnumMap[G.Module.CompressionEnum[selectedCompressionIndex]];
                G.Module.ModuleStateConfig.Json.CurrentBuildAssetBundleOptionsValue += (int)G.Module.CompressionEnumMap[G.Module.CompressionEnum[selectedCompressionIndex_new]];
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
            int i = 0;
            foreach (var tagType in CommonModule.CommonConfig.Json.TagEnum.Values)
            {
                selectedIndexs_new[i] = EditorGUILayout.Popup(selectedIndexs[i], tagType, dropdownStyle, dropdownOptions);
                if (selectedIndexs_new[i] != selectedIndexs[i])
                {
                    selectedIndexs[i] = selectedIndexs_new[i];
                    G.Module.ModuleStateConfig.Json.CurrentTag[i] = tagType[selectedIndexs[i]];
                    return true;
                }
                i++;
            }
            return false;
        }

        private void ClickedApply()
        {
            G.Module.ModuleStateConfig.Json.CurrentUserConfigName = Path.GetFileName(AssetBundleManagement2.AssetBundleModel.BuildMapPath) + ".json"; //TODO: 覆盖当前map文件名，BundleMaster的特殊处理
            G.Module.UserConfig.Json = G.g.mainTab.GetBuildMap_extension().ToList(); //从配置现场覆盖当前map
            if (!G.Runner.Check()) return;
            //确认信息
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), G.Module.ModuleStateConfig.Json.CurrentTag[0], true);
            int optionsValue = G.Module.ModuleStateConfig.Json.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = G.Module.ModuleStateConfig.Json.CurrentResourceVersion;
            string tagPath = Path.Combine(G.Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(G.Module.ModuleStateConfig.Json.CurrentTag));

            bool ensure = EditorUtility.DisplayDialog(G.Module.ModuleName, string.Format("确定应用当前配置？\n\n" +
                "目标平台: {0}\n 输出路径: {1} \n Resources Version: {2} \n 参数: {3}",
                target, tagPath, resourceVersion, optionsValue), "确定", "取消");
            //开始应用          
            if (ensure)
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Build Bundles", "Getting Bunild Maps...", 0);
                    G.Runner.Run();
                    G.Module.DisplayDialog("创建AssetBundles成功！");
                }
                catch (Exception e)
                {
                    G.Module.DisplayDialog("创建AssetBundles时发生错误：" + e.Message);
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
                string originPipelineRootPath = CommonModule.CommonConfig.Json.PipelineRootPath;
                Module newModule = new Module();
                if (!newModule.LoadAllConfigs(path))
                {
                    CommonModule.CommonConfig.Json.PipelineRootPath = originPipelineRootPath;
                    return;
                }
                G.Module = newModule;
                G.Runner.Module = newModule;
                InitSelectedIndex();
                ConfigToIndex();
                CommonModule.CommonConfig.Save();
                EBPUtility.HandleApplyingWarning(G.Module);
            }
        }

        private void InitSelectedIndex()
        {
            selectedCompressionIndex = -1;
            selectedIndexs_new = new int[CommonModule.CommonConfig.Json.TagEnum.Count];
            selectedIndexs = new int[CommonModule.CommonConfig.Json.TagEnum.Count];
            for (int i = 0; i < selectedIndexs.Length; i++)
            {
                selectedIndexs[i] = -1;
            }
        }

        private void LoadAllConfigs()
        {
            G.Module.LoadAllConfigs();
            InitSelectedIndex();
            ConfigToIndex();
            EBPUtility.HandleApplyingWarning(G.Module);
        }

        private void ConfigToIndex()
        {
            if (G.Module.ModuleStateConfig.Json.CurrentTag == null)
            {
                return;
            }
            int length = G.Module.ModuleStateConfig.Json.CurrentTag.Length;
            if (length > CommonModule.CommonConfig.Json.TagEnum.Count)
            {
                G.Module.DisplayDialog("欲加载的标签种类比全局标签种类多，请检查全局标签类型是否丢失");
            }
            else if (length < CommonModule.CommonConfig.Json.TagEnum.Count)
            {
                string[] originCurrentTags = G.Module.ModuleStateConfig.Json.CurrentTag;
                G.Module.ModuleStateConfig.Json.CurrentTag = new string[CommonModule.CommonConfig.Json.TagEnum.Count];
                originCurrentTags.CopyTo(G.Module.ModuleStateConfig.Json.CurrentTag, 0);
            }
            int i = 0;
            foreach (var item in CommonModule.CommonConfig.Json.TagEnum.Values)
            {
                selectedIndexs[i] = GetTagIndex(item, G.Module.ModuleStateConfig.Json.CurrentTag[i], i);
                i++;
            }

            string compressionName = G.Module.CompressionEnumMap.FirstOrDefault(x=>x.Value == (G.Module.ModuleStateConfig.Json.CompressionOption)).Key;
            selectedCompressionIndex = G.Module.CompressionEnum.IndexOf(compressionName);
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
            G.Module.DisplayDialog(string.Format("加载用户配置文件时发生错误：\n欲加载的类型“{0}”"
                  + "不存在于第 {1} 个全局类型枚举中！\n"
                  + "\n请检查配置文件：{2} 和全局类型配置文件：{3}  中的类型名是否匹配",
                  s, count, G.Module.ModuleStateConfig.JsonPath, CommonModule.CommonConfig.JsonPath));
            return -1;
        }
    }
}