using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace EazyBuildPipeline.BundleManager.Editor
{
    public class Runner
    {
        //Configs.Configs configs; //暂时用不到
        public void Apply(AssetBundleBuild[] buildMap, BuildTarget target, string tagPath, int resourceVersion, int bundleVersion, int optionsValue, bool isPartOfPipeline = false)
        {
            G.configs.CurrentConfig.IsPartOfPipeline = isPartOfPipeline;
            G.configs.CurrentConfig.Applying = true;
            G.configs.CurrentConfig.Save();

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
                throw new ApplicationException("BuildAssetBundles失败！详情请查看Console面板。");
            }

            RenameMainBundleManifest(bundlesPath);
            EditorUtility.DisplayProgressBar("Build Bundles", "Creating Info Files...", 0.95f);
            File.WriteAllText(Path.Combine(infoPath, "BuildMap.json"), JsonConvert.SerializeObject(buildMap, Formatting.Indented));
            File.WriteAllText(Path.Combine(infoPath, "Versions.json"), JsonConvert.SerializeObject(new Dictionary<string, int> {
                    { "ResourceVersion", resourceVersion },
                    { "BundleVersion", bundleVersion } }, Formatting.Indented));
            //此处保留旧map文件的生成
            AssetBundleManagement.ABExtractItemBuilder.BuildMapperFile(AssetBundleManagement.ABExtractItemBuilder.BuildAssetMapper(buildMap), Path.Combine(infoPath, "map"));

            G.configs.CurrentConfig.Applying = false;
            G.configs.CurrentConfig.Save();
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