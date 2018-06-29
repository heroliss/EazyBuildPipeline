using Newtonsoft.Json;
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
        Dictionary<string, BuildAssetBundleOptions> compressionDic = new Dictionary<string, BuildAssetBundleOptions>
        {
            { "Uncompress",BuildAssetBundleOptions.UncompressedAssetBundle },
            { "LZMA",BuildAssetBundleOptions.None },
            { "LZ4" ,BuildAssetBundleOptions.ChunkBasedCompression}
        };
        string[] compressionEnum;
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

        public void OnEnable()
        {
			InitStyles();
            compressionEnum = compressionDic.Keys.ToArray();
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
                string path = EditorGUILayout.DelayedTextField(Configs.configs.LocalConfig.RootPath);
                if (GUILayout.Button("...", miniButtonOptions))
                {
                    path = EditorUtility.OpenFolderPanel("打开根目录", Configs.configs.LocalConfig.RootPath, null);
                }
				if (!string.IsNullOrEmpty(path) && path != Configs.configs.LocalConfig.RootPath)
				{
					ChangeRootPath(path);
					return;
				}
            }
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                ShowTagsDropdown();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Resource Version:", labelOptions);
                Configs.configs.BundleManagerConfig.CurrentResourceVersion = EditorGUILayout.IntField(Configs.configs.BundleManagerConfig.CurrentResourceVersion, inputOptions);
                EditorGUILayout.LabelField("  Bundle Version:", labelOptions);
                Configs.configs.BundleManagerConfig.CurrentBundleVersion = EditorGUILayout.IntField(Configs.configs.BundleManagerConfig.CurrentBundleVersion, inputOptions);
                GUILayout.Space(10);
                //压缩选项
                int selectedCompressionIndex_new = EditorGUILayout.Popup(selectedCompressionIndex, compressionEnum, dropdownStyle, dropdownOptions2);
                if (selectedCompressionIndex_new != selectedCompressionIndex)
                {
                    Configs.configs.BundleManagerConfig.CurrentBuildAssetBundleOptionsValue -= (int)compressionDic[compressionEnum[selectedCompressionIndex]];
                    Configs.configs.BundleManagerConfig.CurrentBuildAssetBundleOptionsValue += (int)compressionDic[compressionEnum[selectedCompressionIndex_new]];
                    selectedCompressionIndex = selectedCompressionIndex_new;
					return;
                }
				if (GUILayout.Button(new GUIContent("Build Bundles"), buttonStyle, defaultOptions))
				{ ClickedApply(); return; }
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
            //准备参数和验证
            BuildTarget target = BuildTarget.NoTarget;
            string targetStr = Configs.configs.BundleManagerConfig.CurrentTags[0];
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
            int optionsValue = Configs.configs.BundleManagerConfig.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = Configs.configs.BundleManagerConfig.CurrentResourceVersion;
            int bundleVersion = Configs.configs.BundleManagerConfig.CurrentBundleVersion;
            string tagPath = Path.Combine(Configs.configs.LocalConfig.BundlesFolderPath, Configs.configs.Tag);

            //开始应用          
            bool ensure = EditorUtility.DisplayDialog("Build Bundles", string.Format("确定应用当前配置？\n\n" +
                "目标平台: {0}\n 输出路径: {1} \n Resources Version: {2} \n Bundle Version: {3}\n 参数: {4}",
                target, tagPath, resourceVersion, bundleVersion, optionsValue), "确定", "取消");
            if (ensure)
            {
                try
                {
                    Configs.configs.BundleManagerConfig.Applying = true;
                    Configs.configs.BundleManagerConfig.Save();
                    Apply(target, tagPath, resourceVersion, bundleVersion, optionsValue);
                    Configs.configs.BundleManagerConfig.Applying = false;
                    Configs.configs.BundleManagerConfig.Save();
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

        private void Apply(BuildTarget target, string tagPath, int resourceVersion, int bundleVersion, int optionsValue)
        {
            EditorUtility.DisplayProgressBar("Build Bundles", "Getting Bunild Maps...", 0);
            AssetBundleBuild[] buildMap = Configs.g.mainTab.GetBuildMap_extension();

            EditorUtility.DisplayProgressBar("Build Bundles", "正在重建目录:" + tagPath, 0.02f);
            if (Directory.Exists(tagPath))
            {
                Directory.Delete(tagPath, true); //清空目录
            }
            string infoPath = Path.Combine(tagPath, "_Info");
            string bundlesPath = Path.Combine(tagPath, "Bundles");
            Directory.CreateDirectory(infoPath);
            Directory.CreateDirectory(bundlesPath);

            EditorUtility.DisplayProgressBar("Build Bundles", "开始创建AssetBundles...", 0.1f);
            var manifest = BuildPipeline.BuildAssetBundles(bundlesPath, buildMap, (BuildAssetBundleOptions)optionsValue, target);
            if (manifest == null)
            {
                EditorUtility.DisplayDialog("Build Bundles", "创建AssetBundles失败！详情请查看Console面板。", "确定");
                return;
            }

            RenameMainBundleManifest(bundlesPath);
            EditorUtility.DisplayProgressBar("Build Bundles", "Creating Info Files...", 0.95f);
            File.WriteAllText(Path.Combine(infoPath, "BuildMap.json"), JsonConvert.SerializeObject(buildMap, Formatting.Indented));
            File.WriteAllText(Path.Combine(infoPath, "Versions.json"), JsonConvert.SerializeObject(new Dictionary<string, int> {
                    { "ResourceVersion", resourceVersion },
                    { "BundleVersion", bundleVersion } }, Formatting.Indented));
            //此处保留旧map文件的生成
            AssetBundleManagement.ABExtractItemBuilder.BuildMapperFile(AssetBundleManagement.ABExtractItemBuilder.BuildAssetMapper(buildMap), Path.Combine(infoPath, "map"));

            EditorUtility.DisplayDialog("Build Bundles", "创建AssetBundles成功！", "确定");
        }

        private void RenameMainBundleManifest(string folderPath)
        {
            string oldName = Path.GetFileName(folderPath);
            string oldPath = Path.Combine(folderPath, oldName);
            string newPath = Path.Combine(folderPath, "assetbundlemanifest");
            File.Move(oldPath, newPath);
            File.Move(oldPath + ".manifest", newPath + ".manifest");
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
            selectedCompressionIndex = -1;
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

            string compressionName = compressionDic.FirstOrDefault(x=>x.Value == (Configs.configs.BundleManagerConfig.CompressionOption)).Key;
            selectedCompressionIndex = compressionEnum.IndexOf(compressionName);
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