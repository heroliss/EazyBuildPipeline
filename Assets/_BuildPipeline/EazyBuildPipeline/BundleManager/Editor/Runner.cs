using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
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

        protected override void CheckProcess(bool onlyCheckConfig = false)
        {
            if (!onlyCheckConfig)
            {
                if (Module.ModuleStateConfig.Json.CurrentTag.Length == 0)
                {
                    throw new EBPCheckFailedException("错误：CurrentTag为空");
                }
                var target = BuildTarget.NoTarget;
                string targetStr = Module.ModuleStateConfig.Json.CurrentTag[0];
                try
                {
                    target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetStr, true);
                }
                catch
                {
                    throw new EBPCheckFailedException("没有此平台：" + targetStr);
                }
                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    throw new EBPCheckFailedException(string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target));
                }
            }
            base.CheckProcess(onlyCheckConfig);
        }

        protected override void RunProcess()
        {
            //准备参数
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), Module.ModuleStateConfig.Json.CurrentTag[0], true);
            int optionsValue = Module.ModuleStateConfig.Json.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = Module.ModuleStateConfig.Json.CurrentResourceVersion;
            string tagPath = Path.Combine(Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(Module.ModuleStateConfig.Json.CurrentTag));

            //创建目录
            Module.DisplayProgressBar("正在重建目录:" + tagPath, 0.02f, true);
            if (Directory.Exists(tagPath))
            {
                Directory.Delete(tagPath, true); //清空目录
            }
            string infoPath = Path.Combine(tagPath, "_Info");
            string bundlesPath = Path.Combine(tagPath, "Bundles");
            Directory.CreateDirectory(infoPath);
            Directory.CreateDirectory(bundlesPath);
            //创建Bundles
            Module.DisplayProgressBar("Start Build AssetBundles...", 0.1f, true);
            var manifest = BuildPipeline.BuildAssetBundles(bundlesPath, Module.UserConfig.Json.ToArray(), (BuildAssetBundleOptions)optionsValue, target);
            if (manifest == null)
            {
                throw new EBPException("BuildAssetBundles失败！详情请查看Console面板。");
            }
            //重命名Bundles清单文件
            Module.DisplayProgressBar("Renaming assetbundlemanifest...", 0.92f, true);
            RenameMainBundleManifest(bundlesPath);
            //创建json文件
            Module.DisplayProgressBar("Creating Info Files...", 0.95f, true);
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