using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using EazyBuildPipeline.BundleManager.Configs;

namespace EazyBuildPipeline.BundleManager
{
    [Serializable]
    public partial class Runner : EBPRunner<Module,
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public Runner() { }
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
            //清理无用Bundle项
            List<AssetBundleBuild> cleanedBuilds = new List<AssetBundleBuild>();
            if (Module.ModuleStateConfig.Json.CleanUpBundles)
            {
                Module.DisplayProgressBar("开始清理无用Bundle项", 0, true);
                AssetPolice.Editor.Module policeModule = new AssetPolice.Editor.Module();
                AssetPolice.Editor.Runner policeRunner = new AssetPolice.Editor.Runner(policeModule);
                policeModule.LoadConfigs();
                policeModule.ModuleConfig.Json.OutputPath = CommonModule.CommonConfig.CurrentLogFolderPath; //Hack: 由于输出目录必须存在，所以临时这样设置

                Module.DisplayProgressBar("Start Dry Build AssetBundles...", 0.02f, true);
                policeRunner.Run();

                Module.DisplayProgressBar("Create New AssetBundleBuilds...", 0.2f, true);
                var allAssets = policeModule.ModuleConfig.AllBundles;
                List<string> assetNames = new List<string>();
                using (var writer = new StreamWriter(Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, "RemovedAssetsWhenBuildBundles.txt")))
                {
                    int removedAssetsCount = 0;
                    int removedBundlesCount = 0;
                    foreach (AssetBundleBuild bundle in Module.UserConfig.Json)
                    {
                        assetNames.Clear();
                        foreach (string asset in bundle.assetNames)
                        {
                            AssetPolice.Editor.BundleRDItem rddItem;
                            var exist = allAssets.TryGetValue(asset.ToLower(), out rddItem);
                            if (!exist || rddItem.RDBundles.Count > 0 || rddItem.IsRDRoot)
                            {
                                assetNames.Add(asset);
                            }
                            else
                            {
                                removedAssetsCount++;
                                writer.WriteLine("Remove Asset : " + asset);
                            }
                        }
                        if (assetNames.Count != 0)
                        {
                            cleanedBuilds.Add(new AssetBundleBuild() { assetBundleName = bundle.assetBundleName, assetNames = assetNames.ToArray() });
                        }
                        else
                        {
                            removedBundlesCount++;
                            writer.WriteLine("Remove Bundle: " + bundle.assetBundleName);
                        }
                    }
                    writer.WriteLine("\nRemoved Assets Count: " + removedAssetsCount);
                    writer.WriteLine("Removed Bundles Count: " + removedBundlesCount);
                }
                //保存当前Builds作为日志
                File.WriteAllText(Path.Combine(CommonModule.CommonConfig.CurrentLogFolderPath, "CleanedBundles.json"),
                    JsonConvert.SerializeObject(cleanedBuilds, Formatting.Indented));
            }
            else
            {
                cleanedBuilds = Module.UserConfig.Json;
            }

            //准备参数
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), Module.ModuleStateConfig.Json.CurrentTag[0], true);
            int optionsValue = Module.ModuleStateConfig.Json.CurrentBuildAssetBundleOptionsValue;
            int resourceVersion = Module.ModuleStateConfig.Json.ResourceVersion;
            string tagPath = Path.Combine(Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(Module.ModuleStateConfig.Json.CurrentTag));

            //创建目录
            Module.DisplayProgressBar("正在重建目录:" + tagPath, 0.3f, true);
            if (Directory.Exists(tagPath))
            {
                Directory.Delete(tagPath, true); //清空目录
            }
            string infoPath = Path.Combine(tagPath, "_Info");
            string bundlesPath = Path.Combine(tagPath, "Bundles");
            Directory.CreateDirectory(infoPath);
            Directory.CreateDirectory(bundlesPath);

            //创建json文件
            Module.DisplayProgressBar("Creating Info Files...", 0.45f, true);
            File.WriteAllText(Path.Combine(infoPath, "BuildMap.json"), JsonConvert.SerializeObject(cleanedBuilds, Formatting.Indented));
            File.WriteAllText(Path.Combine(infoPath, "Versions.json"), JsonConvert.SerializeObject(new Dictionary<string, int> {
                { "ResourceVersion", resourceVersion } }, Formatting.Indented));
            //创建Map文件
            //此处保留旧map文件的生成方式
            AssetBundleManagement.ABExtractItemBuilder.BuildMapperFile(AssetBundleManagement.ABExtractItemBuilder.BuildAssetMapper(cleanedBuilds.ToArray()), Path.Combine(infoPath, "map"));

            //创建Bundles
            Module.DisplayProgressBar("Start Build AssetBundles...", 0.5f, true);
            var manifest = BuildPipeline.BuildAssetBundles(bundlesPath, cleanedBuilds.ToArray(), (BuildAssetBundleOptions)optionsValue, target);
            if (manifest == null)
            {
                throw new EBPException("BuildAssetBundles失败！详情请查看Console面板。");
            }
            //重命名Bundles清单文件
            Module.DisplayProgressBar("Renaming assetbundlemanifest...", 1f, true);
            RenameMainBundleManifest(bundlesPath);
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