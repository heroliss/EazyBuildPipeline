using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public class Runner
    {
        struct BundleVersionStruct { public string BundleName; public string Version; };
        struct DownloadFlagStruct { public int flag_; public string name_; public int location_; public bool hasDownloaded_; };

        Configs.Configs configs;
        public Runner(Configs.Configs configs)
        {
            this.configs = configs;
        }

        public bool Check()
        {
            if (configs.PackageMapConfig.Packages.Count == 0)
            {
                configs.DisplayDialog("该配置内没有Package");
                return false;
            }
            //检查配置
            if (string.IsNullOrEmpty(configs.PackageMapConfig.PackageMode))
            {
                configs.DisplayDialog("请设置打包模式");
                return false;
            }
            if (string.IsNullOrEmpty(configs.PackageMapConfig.LuaSource))
            {
                configs.DisplayDialog("请设置Lua源");
                return false;
            }
            if (configs.PackageMapConfig.CompressionLevel == -1)
            {
                configs.DisplayDialog("请设置压缩等级");
                return false;
            }
            if (G.LuaSourceEnum.IndexOf(configs.PackageMapConfig.LuaSource) == -1)
            {
                configs.DisplayDialog("不能识别Lua源：" + configs.PackageMapConfig.LuaSource);
                return false;
            }

            switch (configs.PackageMapConfig.PackageMode)
            {
                case "Addon":
                    if (string.IsNullOrEmpty(configs.CurrentConfig.CurrentAddonVersion))
                    {
                        configs.DisplayDialog("请设置Addon Version");
                        return false;
                    }
                    char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
                    int index = configs.CurrentConfig.CurrentAddonVersion.IndexOfAny(invalidFileNameChars);
                    if (index >= 0)
                    {
                        configs.DisplayDialog("Package Version中不能包含非法字符：" + invalidFileNameChars[index]);
                        return false;
                    }
                    foreach (var package in configs.PackageMapConfig.Packages)
                    {
                        if (string.IsNullOrEmpty(package.Necessery))
                        {
                            configs.DisplayDialog("请设置Necessery");
                            return false;
                        }
                        if (string.IsNullOrEmpty(package.DeploymentLocation))
                        {
                            configs.DisplayDialog("请设置Location");
                            return false;
                        }
                        //不能识别Location和Necessery的情况不可能发生，因为该值由枚举中获得
                    }
                    break;
                case "Patch":
                    break;
                default:
                    configs.DisplayDialog("不能识别模式：" + configs.PackageMapConfig.PackageMode);
                    return false;
            }
            return true;
        }

        public void ApplyAllPackages(int bundleVersion, int resourceVersion)
        {
            //准备参数
            string bundlesFolderPath = configs.GetBundleFolderPath();
            string packagesFolderPath = Path.Combine(configs.LocalConfig.PackageFolderPath, EBPUtility.GetTagStr(configs.CurrentConfig.CurrentTags));
            int count = 0;
            int total = 0;
            float progress = 0;
            var packageMap = configs.PackageMapConfig.Packages;
            foreach (var package in packageMap)
            {
                total += package.Bundles.Count;
            }
            int packagesCount = packageMap.Count;
            //开始
            configs.CurrentConfig.Applying = true;
            configs.CurrentConfig.Save();
            float lastTime = Time.realtimeSinceStartup;

            //TODO:构建map改进方法
            //if (Configs.g.bundleTree.BundleBuildMap == null)
            //{
            //    throw new ApplicationException("BuildMap is null");
            //}
            //string mapContent = JsonConvert.SerializeObject(BuildAsset2BundleMap(Configs.g.bundleTree.BundleBuildMap), Formatting.Indented);

            //重建目录
            EditorUtility.DisplayProgressBar("正在重建Package目录", packagesFolderPath, progress); progress += 0.01f;
            if (Directory.Exists(packagesFolderPath))
            {
                Directory.Delete(packagesFolderPath, true);
            }
            Directory.CreateDirectory(packagesFolderPath);
            //设置路径
            string bundlesRootPathInPackage = "AssetBundles/" + configs.CurrentConfig.CurrentTags[0].ToLower() + "/AssetBundles/";
            string extraInfoFilePathInPackage = "AssetBundles/" + configs.CurrentConfig.CurrentTags[0].ToLower() + "/extra_info";
            string bundleVersionFilePathInPackage = "AssetBundles/" + configs.CurrentConfig.CurrentTags[0].ToLower() + "/bundle_version";
            string mapFilePathInPackage = "AssetBundles/" + configs.CurrentConfig.CurrentTags[0].ToLower() + "/maps/map";
            string streamingPath = Path.Combine("Assets/StreamingAssets/AssetBundles", configs.CurrentConfig.CurrentTags[0]);
            byte[] buffer = new byte[20971520]; //20M缓存,不够会自动扩大

            //以下为整体上Addon和Patch的不同
            switch (configs.PackageMapConfig.PackageMode)
            {
                case "Patch":
                    mapFilePathInPackage += "_" + bundleVersion;
                    break;

                case "Addon":
                    EditorUtility.DisplayProgressBar("正在获取需要拷贝到Streaming中的Bundles信息", "", progress); progress += 0.01f;
                    //得到需要拷贝到Streaming中的Bundles
                    List<string> bundlesCopyToStreaming = new List<string>();
                    foreach (var package in packageMap)
                    {
                        if (package.CopyToStreaming)
                        {
                            bundlesCopyToStreaming = bundlesCopyToStreaming.Union(package.Bundles).ToList();
                        }
                    }
                    //重建StreamingAssets/AssetBundles/[Platform]目录
                    EditorUtility.DisplayProgressBar("正在重建StreamingAssets目录:", "Assets/StreamingAssets/AssetBundles", progress); progress += 0.01f;
                    if (Directory.Exists("Assets/StreamingAssets/AssetBundles"))
                    {
                        Directory.Delete("Assets/StreamingAssets/AssetBundles", true);
                    }
                    Directory.CreateDirectory(streamingPath);

                    //构建download_flag.json
                    EditorUtility.DisplayProgressBar("正在StreamingAssets中构建文件", "download_flag.json", progress); progress += 0.01f;
                    List<DownloadFlagStruct> flagList = new List<DownloadFlagStruct>();
                    foreach (var package in packageMap)
                    {
                        flagList.Add(new DownloadFlagStruct()
                        {
                            name_ = package.FileName,
                            flag_ = G.NecesseryEnum.IndexOf(package.Necessery),
                            location_ = G.DeploymentLocationEnum.IndexOf(package.DeploymentLocation),
                            hasDownloaded_ = package.CopyToStreaming
                        });
                    }
                    string downloadFlagContent = JsonConvert.SerializeObject(flagList, Formatting.Indented);
                    File.WriteAllText(Path.Combine(streamingPath, "download_flag.json"), downloadFlagContent);

                    //构建小包
                    string miniPackagePath = Path.Combine(streamingPath, string.Join("_", new string[]{
                        configs.CurrentConfig.CurrentTags[0].ToLower(),
                        configs.PackageMapConfig.PackageMode.ToLower(),
                        configs.CurrentConfig.CurrentAddonVersion,
                        "default"})) + configs.LocalConfig.PackageExtension;
                    EditorUtility.DisplayProgressBar("正在向StreamingAssets中构建miniPackage", Path.GetFileName(miniPackagePath), progress); progress += 0.01f;
                    using (FileStream zipFileStream = new FileStream(miniPackagePath, FileMode.Create))
                    {
                        using (ZipOutputStream zipStream = new ZipOutputStream(zipFileStream))
                        {
                            zipStream.SetLevel(configs.PackageMapConfig.CompressionLevel);

                            //构建extra_info
                            BuildExtraInfoInZipStream(extraInfoFilePathInPackage, resourceVersion, Path.GetFileNameWithoutExtension(miniPackagePath), zipStream);

                            //构建bundle_version
                            BuildBundleVersionInfoInZipStream(bundleVersionFilePathInPackage, bundleVersion, bundlesCopyToStreaming, zipStream);

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
                        if (Time.realtimeSinceStartup - lastTime > 0.06f)
                        {
                            EditorUtility.DisplayProgressBar(string.Format("正在向StreamingAssets中拷贝Bundles({0}/{1})",
                                i + 1, bundlesCopyToStreamingCount),
                                bundle, progress + (float)i / bundlesCopyToStreamingCount * 0.1f);//拷贝Assetbundle过程占整个过程的10%
                            lastTime = Time.realtimeSinceStartup;
                        }
                        string targetPath = Path.Combine(bundlesRootPathInStreaming, bundle);
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        File.Copy(Path.Combine(bundlesFolderPath, bundle), targetPath, false); //这里不允许覆盖，若已存在则抛出异常
                    }
                    progress += 0.1f;
                    break;
                default:
                    throw new ApplicationException("不能识别模式：" + configs.PackageMapConfig.PackageMode);
            }

            float restProgress = 1 - progress;
            for (int pi = 0; pi < packagesCount; pi++)
            {
                var package = packageMap[pi];
                using (FileStream zipFileStream = new FileStream(Path.Combine(packagesFolderPath, package.FileName), FileMode.Create))
                {
                    using (ZipOutputStream zipStream = new ZipOutputStream(zipFileStream))
                    {
                        zipStream.SetLevel(configs.PackageMapConfig.CompressionLevel);

                        //构建Bundles
                        int bundlesCount = package.Bundles.Count;
                        for (int i = 0; i < bundlesCount; i++)
                        {
                            string bundleRelativePath = package.Bundles[i];
                            string bundlePath = Path.Combine(bundlesFolderPath, bundleRelativePath);
                            if (Time.realtimeSinceStartup - lastTime > 0.06f)
                            {
                                EditorUtility.DisplayProgressBar(string.Format("正在打包{0}({1}/{2}) : ({3}/{4})  总计:({5}/{6})",
                                    package.PackageName, pi + 1, packagesCount, i + 1, bundlesCount, count + 1, total),
                                    bundleRelativePath, progress + (float)count / total * restProgress);
                                lastTime = Time.realtimeSinceStartup;
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

                        //构建extra_info
                        BuildExtraInfoInZipStream(extraInfoFilePathInPackage, resourceVersion, Path.GetFileNameWithoutExtension(package.FileName), zipStream);

                        //构建bundle_version
                        BuildBundleVersionInfoInZipStream(bundleVersionFilePathInPackage, bundleVersion, package.Bundles, zipStream);

                        //以下为每个包中Patch和Addon独有内容
                        switch (configs.PackageMapConfig.PackageMode)
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
                                throw new ApplicationException("不能识别模式：" + configs.PackageMapConfig.PackageMode);
                        }
                    }
                }
            }
            //结束
            configs.CurrentConfig.Applying = false;
            configs.CurrentConfig.Save();
        }

        private void BuildLuaInZipStream(byte[] buffer, ZipOutputStream zipStream)
        {
            switch (configs.PackageMapConfig.LuaSource)
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
                    throw new ApplicationException("不能识别Lua源：" + configs.PackageMapConfig.LuaSource);
            }
        }

        private void BuildMapInZipStream(string mapFilePath, byte[] buffer, ZipOutputStream zipStream)
        {
            //AddBytesToZipStream(zipStream, mapFilePath, System.Text.Encoding.Default.GetBytes(mapContent));
            AddFileToZipStream(zipStream, Path.Combine(configs.GetBundleInfoFolderPath(), "map"), mapFilePath, buffer);
        }

        private void BuildBundleVersionInfoInZipStream(string bundleVersionFilePath, int bundleVersion, List<string> bundles, ZipOutputStream zipStream)
        {
            string bundleVersionStr = bundleVersion.ToString();
            List<BundleVersionStruct> bundleVersionList = new List<BundleVersionStruct>();
            foreach (var item in bundles)
            {
                bundleVersionList.Add(new BundleVersionStruct() { BundleName = item, Version = bundleVersionStr });
            }
            string bundleVersionContent = JsonConvert.SerializeObject(bundleVersionList, Formatting.Indented);
            AddBytesToZipStream(zipStream, bundleVersionFilePath, System.Text.Encoding.Default.GetBytes(bundleVersionContent));
        }

        private void BuildExtraInfoInZipStream(string extraInfoFilePath, int resourceVersion, string fileNameWithoutExtension, ZipOutputStream zipStream)
        {
            string extraInfoContent = JsonConvert.SerializeObject(new Dictionary<string, string>() {
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
    }
}
