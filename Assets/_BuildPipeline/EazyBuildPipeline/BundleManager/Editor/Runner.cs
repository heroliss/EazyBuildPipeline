using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using EazyBuildPipeline.BundleManager.Editor;
using EazyBuildPipeline.BundleManager.Configs;

namespace EazyBuildPipeline.BundleManager
{
    public partial class Runner : EBPRunner<Module,
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public Runner(Module module) : base(module)
        {
        }

        public override bool Check(bool onlyCheckConfig = false)
        {
            if (!onlyCheckConfig)
            {
                if (Module.ModuleStateConfig.Json.CurrentTag.Length == 0)
                {
                    Module.DisplayDialog("错误：CurrentTag为空");
                    return false;
                }
                var target = BuildTarget.NoTarget;
                string targetStr = Module.ModuleStateConfig.Json.CurrentTag[0];
                try
                {
                    target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetStr, true);
                }
                catch
                {
                    Module.DisplayDialog("没有此平台：" + targetStr);
                    return false;
                }
                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    Module.DisplayDialog(string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target));
                    return false;
                }
            }
            return base.Check(onlyCheckConfig);
        }

        protected override void RunProcess()
        {
            //准备参数
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), Module.ModuleStateConfig.Json.CurrentTag[0], true);
            int optionsValue = Module.ModuleStateConfig.Json.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = Module.ModuleStateConfig.Json.CurrentResourceVersion;
            string tagPath = Path.Combine(Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(Module.ModuleStateConfig.Json.CurrentTag));

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
            var manifest = BuildPipeline.BuildAssetBundles(bundlesPath, Module.UserConfig.Json.ToArray(), (BuildAssetBundleOptions)optionsValue, target);
            if (manifest == null)
            {
                throw new ApplicationException("BuildAssetBundles失败！详情请查看Console面板。");
            }
            //重命名Bundles清单文件
            RenameMainBundleManifest(bundlesPath);
            //创建json文件
            EditorUtility.DisplayProgressBar("Build Bundles", "Creating Info Files...", 0.95f);
            File.WriteAllText(Path.Combine(infoPath, "BuildMap.json"), JsonConvert.SerializeObject(Module.UserConfig.Json, Formatting.Indented));
            File.WriteAllText(Path.Combine(infoPath, "Versions.json"), JsonConvert.SerializeObject(new Dictionary<string, int> {
                { "ResourceVersion", resourceVersion } }, Formatting.Indented));
            //创建Map文件
            //此处保留旧map文件的生成方式
            AssetBundleManagement.ABExtractItemBuilder.BuildMapperFile(AssetBundleManagement.ABExtractItemBuilder.BuildAssetMapper(Module.UserConfig.Json.ToArray()), Path.Combine(infoPath, "map"));
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