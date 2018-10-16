using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using EazyBuildPipeline.PackageManager.Editor;
using EazyBuildPipeline.PackageManager.Configs;

namespace EazyBuildPipeline.PackageManager
{
    public partial class Runner : EBPRunner<Module,
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        struct BundleVersionStruct { public string BundleName; public string Version; };
        struct PackageManifestStruct 
        { 
            public int flag_; 
            public string name_;
            public int location_; 
            public bool hasDownloaded_; 
        };

        public int ResourceVersion;

        public Runner(Module module) : base(module)
        {
        }

        public override bool Check()
        {
            if (!Module.RootAvailable)
            {
                Module.DisplayDialog(Module.StateConfigLoadFailedMessage);
                return false;
            }
            if (Module.UserConfig.Json.Packages.Count == 0)
            {
                Module.DisplayDialog("该配置内没有Package");
                return false;
            }
            if (Module.ModuleStateConfig.Json.CurrentTag.Length == 0)
            {
                Module.DisplayDialog("错误：Tags为空");
                return false;
            }
            //检查配置
            if (string.IsNullOrEmpty(Module.UserConfig.Json.PackageMode))
            {
                Module.DisplayDialog("请设置打包模式");
                return false;
            }
            if (string.IsNullOrEmpty(Module.UserConfig.Json.LuaSource))
            {
                Module.DisplayDialog("请设置Lua源");
                return false;
            }
            if (Module.UserConfig.Json.CompressionLevel == -1)
            {
                Module.DisplayDialog("请设置压缩等级");
                return false;
            }
            if (G.LuaSourceEnum.IndexOf(Module.UserConfig.Json.LuaSource) == -1)
            {
                Module.DisplayDialog("不能识别Lua源：" + Module.UserConfig.Json.LuaSource);
                return false;
            }

            switch (Module.UserConfig.Json.PackageMode)
            {
                case "Addon":
                    if (string.IsNullOrEmpty(Module.ModuleStateConfig.Json.CurrentAddonVersion))
                    {
                        Module.DisplayDialog("请设置Addon Version");
                        return false;
                    }
                    char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
                    int index = Module.ModuleStateConfig.Json.CurrentAddonVersion.IndexOfAny(invalidFileNameChars);
                    if (index >= 0)
                    {
                        Module.DisplayDialog("Package Version中不能包含非法字符：" + invalidFileNameChars[index]);
                        return false;
                    }
                    foreach (var package in Module.UserConfig.Json.Packages)
                    {
                        if (string.IsNullOrEmpty(package.Necessery))
                        {
                            Module.DisplayDialog("请设置Necessery");
                            return false;
                        }
                        if (string.IsNullOrEmpty(package.DeploymentLocation))
                        {
                            Module.DisplayDialog("请设置Location");
                            return false;
                        }
                        //不能识别Location和Necessery的情况不可能发生，因为该值由枚举中获得
                    }
                    break;
                case "Patch":
                    break;
                default:
                    Module.DisplayDialog("不能识别模式：" + Module.UserConfig.Json.PackageMode);
                    return false;
            }
            return true;
        }

        protected override void RunProcess()
        {
            //准备参数
            string bundlesFolderPath = Module.GetBundleFolderPath();
            if (!Directory.Exists(bundlesFolderPath))
            {
                throw new ApplicationException("Bundles目录不存：" + bundlesFolderPath);
            }
            string packagesFolderPath = Path.Combine(Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(Module.ModuleStateConfig.Json.CurrentTag));
            int count = 0;
            int total = 0;
            float progress = 0;
            var packages = Module.UserConfig.Json.Packages;
            foreach (var package in packages)
            {
                total += package.Bundles.Count;
            }
            int packagesCount = packages.Count;
            
            //TODO:构建map改进方法
            //if (Configs.g.bundleTree.BundleBuildMap == null)
            //{
            //    throw new ApplicationException("BuildMap is null");
            //}
            //string mapContent = JsonConvert.SerializeObject(BuildAsset2BundleMap(Configs.g.bundleTree.BundleBuildMap), Formatting.Indented);
           
            //开始
            double lastTime = EditorApplication.timeSinceStartup;

            //重建目录
            EditorUtility.DisplayProgressBar("正在重建Package目录", packagesFolderPath, progress); progress += 0.01f;
            if (Directory.Exists(packagesFolderPath))
            {
                Directory.Delete(packagesFolderPath, true);
            }
            Directory.CreateDirectory(packagesFolderPath);
            //设置路径
            string platform = Module.ModuleStateConfig.Json.CurrentTag[0].ToLower();
            string bundlesRootPathInPackage = "AssetBundles/" + platform + "/AssetBundles/";
            string bundleVersionFilePathInPackage = "AssetBundles/" + platform + "/bundle_version";
            string mapFilePathInPackage = "AssetBundles/" + platform + "/maps/map";
            string streamingPath = Path.Combine("Assets/StreamingAssets/AssetBundles", Module.ModuleStateConfig.Json.CurrentTag[0]);
            byte[] buffer = new byte[20971520]; //20M缓存,不够会自动扩大

            //以下为整体上Addon和Patch的不同
            switch (Module.UserConfig.Json.PackageMode)
            {
                case "Patch":
                    mapFilePathInPackage += "_" + ResourceVersion;
                    break;

                case "Addon":
                    //修改当前ResourceVersion
                    CommonModule.CommonConfig.Json.CurrentResourceVersion = ResourceVersion;
                    CommonModule.CommonConfig.Save();

                    EditorUtility.DisplayProgressBar("正在获取需要拷贝到Streaming中的Bundles信息", "", progress); progress += 0.01f;
                    //得到需要拷贝到Streaming中的Bundles
                    List<string> bundlesCopyToStreaming = new List<string>();
                    //List<string> bundlesCopyToStreaming_all = new List<string>();

                    foreach (var package in packages)
                    {
                        if (package.CopyToStreaming)
                        {
                            bundlesCopyToStreaming = bundlesCopyToStreaming.Union(package.Bundles).ToList();
                        }
                        //bundlesCopyToStreaming_all = bundlesCopyToStreaming_all.Union(package.Bundles).ToList();
                    }

                    //重建StreamingAssets/AssetBundles/[Platform]目录
                    EditorUtility.DisplayProgressBar("正在重建StreamingAssets目录:", "Assets/StreamingAssets/AssetBundles", progress); progress += 0.01f;
                    if (Directory.Exists("Assets/StreamingAssets/AssetBundles"))
                    {
                        Directory.Delete("Assets/StreamingAssets/AssetBundles", true);
                    }
                    Directory.CreateDirectory(streamingPath);

                    //构建PackageManifest.json
                    EditorUtility.DisplayProgressBar("正在StreamingAssets中构建文件", "PackageManifest.json", progress); progress += 0.01f;
                    List<PackageManifestStruct> packageManifest = new List<PackageManifestStruct>();
                    //单独加一个核心包的配置信息
                    string corePackageName = string.Join("_", new string[]{
                        platform,
                        Module.UserConfig.Json.PackageMode.ToLower(),
                        Module.ModuleStateConfig.Json.CurrentAddonVersion,
                        "Core"}) + Module.ModuleConfig.Json.PackageExtension;
                    packageManifest.Add(new PackageManifestStruct
                    {
                        name_ = corePackageName,
                        flag_ = G.NecesseryEnum.IndexOf("Immediate"),
                        location_ = G.DeploymentLocationEnum.IndexOf("Built-in"),
                        hasDownloaded_ = false
                    });

                    //所有package的配置信息
                    foreach (var package in packages)
                    {
                        packageManifest.Add(new PackageManifestStruct
                        {
                            name_ = GetPackageFileName(package.PackageName, ResourceVersion),
                            flag_ = G.NecesseryEnum.IndexOf(package.Necessery),
                            location_ = G.DeploymentLocationEnum.IndexOf(package.DeploymentLocation),
                            hasDownloaded_ = package.CopyToStreaming
                        });
                    }
                    string packageManifestContent = JsonConvert.SerializeObject(packageManifest, Formatting.Indented);
                    File.WriteAllText(Path.Combine(streamingPath, "PackageManifest.json"), packageManifestContent);

                    //构建核心包
                    string corePackagePath = Path.Combine(streamingPath, corePackageName);
                    EditorUtility.DisplayProgressBar("正在向StreamingAssets中构建CorePackage", corePackagePath, progress); progress += 0.01f;
                    using (FileStream zipFileStream = new FileStream(corePackagePath, FileMode.Create))
                    {
                        using (ZipOutputStream zipStream = new ZipOutputStream(zipFileStream))
                        {
                            zipStream.SetLevel(Module.UserConfig.Json.CompressionLevel);

                            //构建bundle_version
                            BuildBundleVersionInfoInZipStream(bundleVersionFilePathInPackage, ResourceVersion, bundlesCopyToStreaming, zipStream);

                            //构建map
                            BuildMapInZipStream(mapFilePathInPackage, buffer, zipStream);

                            //添加Lua
                            BuildLuaInZipStream(buffer, zipStream);
                        }
                    }

                    //拷贝Assetbundle
                    string bundlesRootPathInStreaming = Path.Combine(streamingPath, "AssetBundles");
                    int bundlesCopyToStreamingCount = bundlesCopyToStreaming.Count;
                    for (int i = 0; i < bundlesCopyToStreamingCount; i++)
                    {
                        string bundle = bundlesCopyToStreaming[i];
                        if (EditorApplication.timeSinceStartup - lastTime > 0.06f)
                        {
                            EditorUtility.DisplayProgressBar(string.Format("正在向StreamingAssets中拷贝Bundles({0}/{1})",
                                i + 1, bundlesCopyToStreamingCount),
                                bundle, progress + (float)i / bundlesCopyToStreamingCount * 0.1f);//拷贝Assetbundle过程占整个过程的10%
                            lastTime = EditorApplication.timeSinceStartup;
                        }
                        string targetPath = Path.Combine(bundlesRootPathInStreaming, bundle);
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        File.Copy(Path.Combine(bundlesFolderPath, bundle), targetPath, false); //这里不允许覆盖，若已存在则抛出异常
                    }
                    progress += 0.1f;
                    break;
                default:
                    throw new ApplicationException("不能识别模式：" + Module.UserConfig.Json.PackageMode);
            }

            float restProgress = 1 - progress;
            for (int pi = 0; pi < packagesCount; pi++)
            {
                var package = packages[pi];
                using (FileStream zipFileStream = new FileStream(Path.Combine(packagesFolderPath, GetPackageFileName(package.PackageName, ResourceVersion)), FileMode.Create))
                {
                    using (ZipOutputStream zipStream = new ZipOutputStream(zipFileStream))
                    {
                        zipStream.SetLevel(Module.UserConfig.Json.CompressionLevel);

                        //构建Bundles
                        int bundlesCount = package.Bundles.Count;
                        for (int i = 0; i < bundlesCount; i++)
                        {
                            string bundleRelativePath = package.Bundles[i];
                            string bundlePath = Path.Combine(bundlesFolderPath, bundleRelativePath);
                            if (EditorApplication.timeSinceStartup - lastTime > 0.06f)
                            {
                                EditorUtility.DisplayProgressBar(string.Format("正在打包{0}({1}/{2}) : ({3}/{4})  总计:({5}/{6})",
                                    package.PackageName, pi + 1, packagesCount, i + 1, bundlesCount, count + 1, total),
                                    bundleRelativePath, progress + (float)count / total * restProgress);
                                lastTime = EditorApplication.timeSinceStartup;
                            }
                            AddFileToZipStream(zipStream, bundlePath, Path.Combine(bundlesRootPathInPackage, bundleRelativePath), buffer);
                            count++;
                        }

                        //构建空目录
                        int emptyFolderCount = package.EmptyFolders.Count;
                        EditorUtility.DisplayProgressBar(string.Format("正在打包{0}({1}/{2}) : ({3}/{4})  总计:({5}/{6})",
                                package.PackageName, pi + 1, packagesCount, bundlesCount, bundlesCount, count, total),
                                string.Format("Empty Folders (Total:{0})", emptyFolderCount), progress + (float)count / total * restProgress);
                        for (int i = 0; i < emptyFolderCount; i++)
                        {
                            zipStream.PutNextEntry(new ZipEntry(package.EmptyFolders[i] + "/") { });
                        }
                        
                        //构建bundle_version
                        BuildBundleVersionInfoInZipStream(bundleVersionFilePathInPackage, ResourceVersion, package.Bundles, zipStream);

                        //以下为每个包中Patch和Addon独有内容
                        switch (Module.UserConfig.Json.PackageMode)
                        {
                            case "Patch":
                                //构建map
                                EditorUtility.DisplayProgressBar(string.Format("正在打包{0}({1}/{2}) : ({3}/{4})  总计:({5}/{6})",
                                    package.PackageName, pi + 1, packagesCount, bundlesCount, bundlesCount, count, total),
                                    "Building Map...", progress + (float)count / total * restProgress);
                                BuildMapInZipStream(mapFilePathInPackage, buffer, zipStream);
                                //添加Lua
                                EditorUtility.DisplayProgressBar(string.Format("正在打包{0}({1}/{2}) : ({3}/{4})  总计:({5}/{6})",
                                    package.PackageName, pi + 1, packagesCount, bundlesCount, bundlesCount, count, total),
                                    "Adding Lua...", progress + (float)count / total * restProgress);
                                BuildLuaInZipStream(buffer, zipStream);
                                break;

                            case "Addon":
                                break;
                            default:
                                throw new ApplicationException("不能识别模式：" + Module.UserConfig.Json.PackageMode);
                        }
                    }
                }
            }
        }

        private void BuildLuaInZipStream(byte[] buffer, ZipOutputStream zipStream)
        {
            //AddDirectoryToZipStream(zipStream, "Assets/StreamingAssets/Lua", "Lua/Lua", buffer, "*.lua"); //TODO: 重复的Lua库，可删
            switch (Module.UserConfig.Json.LuaSource)
            {
                case "None":
                    break;
                case "Origin":
                    AddDirectoryToZipStream(zipStream, "Assets/LuaScripts", "Lua/LuaScripts32", buffer, "*.lua");
                    AddDirectoryToZipStream(zipStream, "Assets/LuaScripts", "Lua/LuaScripts64", buffer, "*.lua");
                    break;
                case "ByteCode":
                    AddDirectoryToZipStream(zipStream, "Assets/LuaScriptsByteCode32", "Lua/LuaScripts32", buffer, "*.lua");
                    AddDirectoryToZipStream(zipStream, "Assets/LuaScriptsByteCode64", "Lua/LuaScripts64", buffer, "*.lua");
                    break;
                case "Encrypted":
                    AddDirectoryToZipStream(zipStream, "Assets/LuaScriptsEncrypted32", "Lua/LuaScripts32", buffer, "*.lua");
                    AddDirectoryToZipStream(zipStream, "Assets/LuaScriptsEncrypted64", "Lua/LuaScripts64", buffer, "*.lua");
                    break;
                default:
                    throw new ApplicationException("不能识别Lua源：" + Module.UserConfig.Json.LuaSource);
            }
        }

        private void BuildMapInZipStream(string mapFilePath, byte[] buffer, ZipOutputStream zipStream)
        {
            //AddBytesToZipStream(zipStream, mapFilePath, System.Text.Encoding.Default.GetBytes(mapContent));
            AddFileToZipStream(zipStream, Path.Combine(Module.GetBundleInfoFolderPath(), "map"), mapFilePath, buffer);
        }

        private void BuildBundleVersionInfoInZipStream(string bundleVersionFilePath, int bundleVersion, List<string> bundles, ZipOutputStream zipStream)
        {
            string bundleVersionStr = bundleVersion.ToString();
            List<BundleVersionStruct> bundleVersionList = new List<BundleVersionStruct>();
            foreach (var item in bundles)
            {
                bundleVersionList.Add(new BundleVersionStruct { BundleName = item, Version = bundleVersionStr });
            }
            string bundleVersionContent = JsonConvert.SerializeObject(bundleVersionList, Formatting.Indented);
            AddBytesToZipStream(zipStream, bundleVersionFilePath, System.Text.Encoding.Default.GetBytes(bundleVersionContent));
        }

        private void BuildExtraInfoInZipStream(string extraInfoFilePath, int resourceVersion, string fileNameWithoutExtension, ZipOutputStream zipStream)
        {
            string extraInfoContent = JsonConvert.SerializeObject(new Dictionary<string, string> {
                            { "brief_desc", fileNameWithoutExtension },
                            { "res_version", resourceVersion.ToString() }
                        }, Formatting.Indented);
            AddBytesToZipStream(zipStream, extraInfoFilePath, System.Text.Encoding.Default.GetBytes(extraInfoContent));
        }

        /// <summary>
        /// 将目录下所有文件按照原目录结构加入压缩流（不包含空文件夹）
        /// </summary>
        /// <param name="zipStream"></param>
        /// <param name="sourceFolderPath">源目录路径，必须不是/开头或结尾</param>
        /// <param name="targetFolderPath"></param>
        /// <param name="buffer"></param>
        /// <param name="searchPattern"></param>
        private void AddDirectoryToZipStream(ZipOutputStream zipStream, string sourceFolderPath, string targetFolderPath, byte[] buffer, string searchPattern = "*")
        {
            int length = sourceFolderPath.Length + 1;
            foreach (var filePath in Directory.GetFiles(sourceFolderPath, searchPattern, SearchOption.AllDirectories))
            {
                AddFileToZipStream(zipStream, filePath, Path.Combine(targetFolderPath, filePath.Remove(0, length)), buffer);
            }
        }

        private Dictionary<string, string> BuildAsset2BundleMap(AssetBundleBuild[] buildMap)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (var item in buildMap)
            {
                foreach (string assetName in item.assetNames)
                {
                    map.Add(assetName, item.assetBundleName);
                }
            }
            return map;
        }

        private void AddFileToZipStream(ZipOutputStream zipStream, string sourceFilePath, string targetPathInZip, byte[] buffer)
        {
            using (FileStream fileStream = new FileStream(sourceFilePath, FileMode.Open))
            {
                ZipEntry zipEntry = new ZipEntry(targetPathInZip);
                zipStream.PutNextEntry(zipEntry);
                int fileLength = (int)fileStream.Length;
                if (buffer.Length < fileLength)
                {
                    buffer = new byte[fileLength];
                }
                fileStream.Read(buffer, 0, fileLength);
                zipStream.Write(buffer, 0, fileLength);
            }
        }

        private void AddBytesToZipStream(ZipOutputStream zipStream, string targetPathInZip, byte[] bytes)
        {
            ZipEntry zipEntry = new ZipEntry(targetPathInZip);
            zipStream.PutNextEntry(zipEntry);
            zipStream.Write(bytes, 0, bytes.Length);
        }

        public string GetPackageFileName(string displayName,int resourceVersion)
        {
            string fileName;
            var currentTag = Module.ModuleStateConfig.Json.CurrentTag;
            string platform = currentTag[0] == null ? "Unknow" : currentTag[0].ToLower();
            switch (Module.UserConfig.Json.PackageMode)
            {
                case "Addon":
                    fileName = string.Format("{0}_addon_{1}_{2}{3}",
                        platform,
                        Module.ModuleStateConfig.Json.CurrentAddonVersion,
                        displayName,
                        Module.ModuleConfig.Json.PackageExtension);
                    break;
                case "Patch":
                    fileName = string.Format("{0}_patch_{1}_{2}{3}",
                      platform,
                      resourceVersion,
                      displayName,
                      Module.ModuleConfig.Json.PackageExtension);
                    break;
                default:
                    fileName = displayName + Module.ModuleConfig.Json.PackageExtension;
                    break;
            }
            return fileName;
        }
    }
}
