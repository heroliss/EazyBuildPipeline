using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace EazyBuildPipeline.AssetPolice.Editor
{
    [Serializable]
    public partial class Runner
    {
        [NonSerialized] public Module Module;

        public Runner()
        { }
        public Runner(Module module)
        {
            Module = module;
        }

        public void Run()
        {
            Module.ModuleConfig.AllBundles.Clear();
            //Create AssetBundleList
            List<AssetBundleBuild> assetBundleList = new List<AssetBundleBuild>();
            foreach (var folder in Module.ModuleConfig.Json.AssetsDirectories.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) //每一个给定的要创建Bundle的目录
            {
                var directory = "Assets/" + folder;
                foreach (var file in FindFiles(directory, Module.ModuleConfig.Json.ExcludeExtensionsWhenBuildBundles)) //每一个资产文件
                {
                    string filePath = Path.Combine(directory, file);
                    var ab = new AssetBundleBuild
                    {
                        assetNames = new[] { filePath },
                        assetBundleName = filePath
                    };
                    assetBundleList.Add(ab);
                }
            }

            //Start DryBuild
            var manifest = BuildPipeline.BuildAssetBundles(Module.ModuleConfig.Json.OutputPath, assetBundleList.ToArray(), BuildAssetBundleOptions.DryRunBuild, EditorUserBuildSettings.activeBuildTarget);
            if (manifest == null)
            {
                throw new EBPException("Dry Build AssetBundles Failed!");
            }

            //创建用来标记是否被引用的字典
            foreach (var bundle in manifest.GetAllAssetBundles()) //获取所有Bundle添加到字典
            {
                Module.ModuleConfig.AllBundles.Add(bundle, new BundleRDItem());
            }

            //标记是否被引用
            foreach (var item in Module.ModuleConfig.Json.DependenceFilterDirectories.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) //每一个给定要检查依赖的目录
            {
                if (string.IsNullOrEmpty(item)) continue;
                string[] s = item.Split(new[] { Module.ModuleConfig.Json.Separator }, StringSplitOptions.RemoveEmptyEntries);
                var directory = "Assets/" + s[0];
                foreach (var file in FindFiles(directory))
                {
                    string extension = Path.GetExtension(file);
                    bool available = false;
                    for (int i = 1; i < s.Length; i++)
                    {
                        if (s[i] == extension)
                        {
                            available = true;
                            break;
                        }
                    }
                    if (s.Length < 2) //该句表示若没有设置Available Extension，则全部允许
                    {
                        available = true;
                    }
                    if (available)
                    {
                        string filePath = Path.Combine(directory, file).Replace('\\', '/').ToLower();
                        Module.ModuleConfig.AllBundles[filePath].IsRDRoot = true;
                        foreach (string dependence in manifest.GetAllDependencies(filePath)) //添加每一个依赖的文件
                        {
                            Module.ModuleConfig.AllBundles[dependence].RDBundles.Add(filePath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 查找给定根目录下的所有文件
        /// </summary>
        /// <param name="rootPath">根目录路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <returns>返回文件相对于rootPath的相对路径</returns>
        public static List<string> FindFiles(string rootPath, string excludeExtensions = "", string searchPattern = "*")
        {
            string[] excludeExtensionList = excludeExtensions.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int rootPathLen = rootPath.Length;
            List<string> files = new List<string>();
            foreach (var file in Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(file);
                if (extension != ".meta" && !excludeExtensionList.Contains(extension)) //需要排除的文件后缀
                {
                    files.Add(file.Substring(rootPathLen + 1));
                }
            }
            return files;
        }
    }
}
