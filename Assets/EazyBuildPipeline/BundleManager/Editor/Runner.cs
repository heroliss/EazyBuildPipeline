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
            var target = BuildTarget.NoTarget;
            string targetStr = configs.CurrentConfig.CurrentTags[0];
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetStr, true);
            }
            catch
            {
                EditorUtility.DisplayDialog("Build Bundles", "没有此平台：" + targetStr, "确定");
                return false;
            }
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                EditorUtility.DisplayDialog("Build Bundles", string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target), "确定");
                return false;
            }
            return true;
        }

        public void Apply(AssetBundleBuild[] buildMap, bool isPartOfPipeline = false)
        {
            //准备参数
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), configs.CurrentConfig.CurrentTags[0], true);
            int optionsValue = configs.CurrentConfig.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = configs.CurrentConfig.CurrentResourceVersion;
            int bundleVersion = configs.CurrentConfig.CurrentBundleVersion;
            string tagPath = Path.Combine(configs.LocalConfig.BundlesFolderPath, EBPUtility.GetTagStr(configs.CurrentConfig.CurrentTags));
            //开始
            configs.CurrentConfig.IsPartOfPipeline = isPartOfPipeline;
            configs.CurrentConfig.Applying = true;
            configs.CurrentConfig.Save();
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
            var manifest = BuildPipeline.BuildAssetBundles(bundlesPath, buildMap, (BuildAssetBundleOptions)optionsValue, target);
            if (manifest == null)
            {
                throw new ApplicationException("BuildAssetBundles失败！详情请查看Console面板。");
            }
            //重命名Bundles清单文件
            RenameMainBundleManifest(bundlesPath);
            //创建json文件
            EditorUtility.DisplayProgressBar("Build Bundles", "Creating Info Files...", 0.95f);
            File.WriteAllText(Path.Combine(infoPath, "BuildMap.json"), JsonConvert.SerializeObject(buildMap, Formatting.Indented));
            File.WriteAllText(Path.Combine(infoPath, "Versions.json"), JsonConvert.SerializeObject(new Dictionary<string, int> {
                    { "ResourceVersion", resourceVersion },
                    { "BundleVersion", bundleVersion } }, Formatting.Indented));
            //创建Map文件
            //此处保留旧map文件的生成方式
            AssetBundleManagement.ABExtractItemBuilder.BuildMapperFile(AssetBundleManagement.ABExtractItemBuilder.BuildAssetMapper(buildMap), Path.Combine(infoPath, "map"));
            //结束
            configs.CurrentConfig.Applying = false;
            configs.CurrentConfig.Save();
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