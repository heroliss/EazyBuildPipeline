using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace EazyBuildPipeline.BundleManager.Editor
{
    public class Runner
    {
        Configs.Configs configs;

        public Runner(Configs.Configs configs)
        {
            this.configs = configs;
        }

        public bool Check()
        {
            if (configs.CurrentConfig.Json.CurrentTags.Length == 0)
            {
                configs.DisplayDialog("错误：Tags为空");
            }
            var target = BuildTarget.NoTarget;
            string targetStr = configs.CurrentConfig.Json.CurrentTags[0];
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetStr, true);
            }
            catch
            {
                configs.DisplayDialog("没有此平台：" + targetStr);
                return false;
            }
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                configs.DisplayDialog(string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target));
                return false;
            }
            return true;
        }

        public void Apply(bool isPartOfPipeline = false)
        {
            //开始
            configs.CurrentConfig.Json.IsPartOfPipeline = isPartOfPipeline;
            configs.CurrentConfig.Json.Applying = true;
            configs.CurrentConfig.Save();

            //准备参数
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), configs.CurrentConfig.Json.CurrentTags[0], true);
            int optionsValue = configs.CurrentConfig.Json.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = configs.CurrentConfig.Json.CurrentResourceVersion;
            int bundleVersion = configs.CurrentConfig.Json.CurrentBundleVersion;
            string tagPath = Path.Combine(configs.LocalConfig.BundlesFolderPath, EBPUtility.GetTagStr(configs.CurrentConfig.Json.CurrentTags));

            //创建目录
            EditorUtility.DisplayProgressBar("Build Bundles", "正在重建目录:" + tagPath, 0.02f);
            if (Directory.Exists(tagPath))
            {
                Directory.Delete(tagPath, true); //清空目录
            }
            string infoPath = Path.Combine(tagPath, "_Info");
            string bundlesPath = Path.Combine(tagPath, "Bundles");
            Directory.CreateDirectory(infoPath);
            Directory.CreateDirectory(bundlesPath);
            //创建Bundles
            EditorUtility.DisplayProgressBar("Build Bundles", "开始创建AssetBundles...", 0.1f);
            var manifest = BuildPipeline.BuildAssetBundles(bundlesPath, configs.BundleBuildMapConfig.Json, (BuildAssetBundleOptions)optionsValue, target);
            if (manifest == null)
            {
                throw new ApplicationException("BuildAssetBundles失败！详情请查看Console面板。");
            }
            //重命名Bundles清单文件
            RenameMainBundleManifest(bundlesPath);
            //创建json文件
            EditorUtility.DisplayProgressBar("Build Bundles", "Creating Info Files...", 0.95f);
            File.WriteAllText(Path.Combine(infoPath, "BuildMap.json"), JsonConvert.SerializeObject(configs.BundleBuildMapConfig.Json, Formatting.Indented));
            File.WriteAllText(Path.Combine(infoPath, "Versions.json"), JsonConvert.SerializeObject(new Dictionary<string, int> {
                    { "ResourceVersion", resourceVersion },
                    { "BundleVersion", bundleVersion } }, Formatting.Indented));
            //创建Map文件
            //此处保留旧map文件的生成方式
            AssetBundleManagement.ABExtractItemBuilder.BuildMapperFile(AssetBundleManagement.ABExtractItemBuilder.BuildAssetMapper(configs.BundleBuildMapConfig.Json), Path.Combine(infoPath, "map"));
            //结束
            configs.CurrentConfig.Json.Applying = false;
            configs.CurrentConfig.Save();

            AssetDatabase.Refresh();
        }

        private void RenameMainBundleManifest(string folderPath)
        {
            string oldName = Path.GetFileName(folderPath);
            string oldPath = Path.Combine(folderPath, oldName);
            string newPath = Path.Combine(folderPath, "assetbundlemanifest");
            File.Move(oldPath, newPath);
            File.Move(oldPath + ".manifest", newPath + ".manifest");
        }
    }
}