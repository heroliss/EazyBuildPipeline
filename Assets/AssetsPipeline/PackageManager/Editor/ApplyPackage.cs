using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiXuFeng.PackageManager.Editor
{
    class ApplyPackage
    {
        struct BundleVersionStruct { public string BundleName; public string Version; };
        struct DownloadFlagStruct { public int flag_; public string name_; public int location_; public bool hasDownloaded_; };

        public void ApplyAllPackages(List<Config.PackageMapConfig.Package> packageMap)
        {
            float lastTime = Time.realtimeSinceStartup;
            string bundlesFolderPath = Configs.configs.BundlePath;
            string packagesFolderPath = Path.Combine(Configs.configs.LocalConfig.PackageRootPath, Configs.configs.Tag);
            int count = 0;
            int total = 0;
            foreach (var package in packageMap)
            {
                total += package.Bundles.Count;
            }
            int packagesCount = packageMap.Count;

            //TODO:构建map改进方法
            //if (Configs.g.bundleTree.BundleBuildMap == null)
            //{
            //    throw new ApplicationException("BuildMap is null");
            //}
            //string mapContent = JsonConvert.SerializeObject(BuildAsset2BundleMap(Configs.g.bundleTree.BundleBuildMap), Formatting.Indented);

            EditorUtility.DisplayProgressBar("Build Packages", "正在重建目录:" + packagesFolderPath, 0);
            if (Directory.Exists(packagesFolderPath))
            {
                Directory.Delete(packagesFolderPath, true);
            }
            Directory.CreateDirectory(packagesFolderPath);

            string bundlesRootPathInPackage = "AssetBundles/" + Configs.configs.PackageConfig.CurrentTags[0].ToLower() + "/AssetBundles/";
            string extraInfoFilePathInPackage = "AssetBundles/" + Configs.configs.PackageConfig.CurrentTags[0].ToLower() + "/extra_info";
            string bundleVersionFilePathInPackage = "AssetBundles/" + Configs.configs.PackageConfig.CurrentTags[0].ToLower() + "/bundle_version";
            string mapFilePathInPackage = "AssetBundles/" + Configs.configs.PackageConfig.CurrentTags[0].ToLower() + "/maps/map";
            string streamingPath = Path.Combine("Assets/StreamingAssets/AssetBundles", Configs.configs.PackageConfig.CurrentTags[0]);
            byte[] buffer = new byte[20971520]; //20M缓存,不够会自动扩大

            //以下为整体上Addon和Patch的不同
            switch (Configs.configs.PackageMapConfig.PackageMode)
            {
                case "Patch":
                    mapFilePathInPackage += "_" + Configs.g.bundleTree.BundleVersions.BundleVersion;
                    break;

                case "Addon":
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
                    if (Directory.Exists(streamingPath))
                    {
                        Directory.Delete(streamingPath, true);
                    }
                    Directory.CreateDirectory(streamingPath);

                    //构建download_flag.json
                    List<DownloadFlagStruct> flagList = new List<DownloadFlagStruct>();
                    foreach (var package in packageMap)
                    {
                        flagList.Add(new DownloadFlagStruct()
                        {
                            name_ = package.FileName,
                            flag_ = Configs.NecesseryEnum.IndexOf(package.Necessery),
                            location_ = Configs.DeploymentLocationEnum.IndexOf(package.DeploymentLocation),
                            hasDownloaded_ = package.CopyToStreaming
                        });
                    }
                    string downloadFlagContent = JsonConvert.SerializeObject(flagList, Formatting.Indented);
                    File.WriteAllText(Path.Combine(streamingPath, "download_flag.json"), downloadFlagContent);

                    //构建小包
                    string miniPackagePath = Path.Combine(streamingPath, string.Join("_", new string[]{
                        Configs.configs.PackageConfig.CurrentTags[0].ToLower(),
                        Configs.configs.PackageMapConfig.PackageMode.ToLower(),
                        Configs.configs.PackageMapConfig.PackageVersion,
                        "default"})) + Configs.configs.LocalConfig.PackageExtension;
                    using (FileStream zipFileStream = new FileStream(miniPackagePath, FileMode.Create))
                    {
                        using (ZipOutputStream zipStream = new ZipOutputStream(zipFileStream))
                        {
                            zipStream.SetLevel(Configs.configs.PackageMapConfig.CompressionLevel);

                            //构建extra_info
                            BuildExtraInfoInZipStream(extraInfoFilePathInPackage, Path.GetFileNameWithoutExtension(miniPackagePath), zipStream);

                            //构建bundle_version
                            BuildBundleVersionInfoInZipStream(bundleVersionFilePathInPackage, bundlesCopyToStreaming, zipStream);

                            //构建map
                            BuildMapInZipStream(mapFilePathInPackage, buffer, zipStream);

                            //添加Lua
                            BuildLuaInZipStream(buffer, zipStream);
                        }
                    }

                    //拷贝Assetbundle
                    string bundlesRootPathInStreaming = Path.Combine(streamingPath, "AssetBundles");
                    foreach (var bundle in bundlesCopyToStreaming)
                    {
                        string targetPath = Path.Combine(bundlesRootPathInStreaming, bundle);
                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                        File.Copy(Path.Combine(bundlesFolderPath, bundle), targetPath, false); //这里不允许覆盖，若已存在则抛出异常
                    }
                    break;
                default:
                    throw new ApplicationException("不能识别模式：" + Configs.configs.PackageMapConfig.PackageMode);
            }

            for (int pi = 0; pi < packagesCount; pi++)
            {
                var package = packageMap[pi];
                using (FileStream zipFileStream = new FileStream(Path.Combine(packagesFolderPath, package.FileName), FileMode.Create))
                {
                    using (ZipOutputStream zipStream = new ZipOutputStream(zipFileStream))
                    {
                        zipStream.SetLevel(Configs.configs.PackageMapConfig.CompressionLevel);

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
                                    bundleRelativePath, (float)count / total);
                                lastTime = Time.realtimeSinceStartup;
                            }
                            AddFileToZipStream(zipStream, bundlePath, Path.Combine(bundlesRootPathInPackage, bundleRelativePath), buffer);
                            count++;
                        }

                        //构建空目录
                        int emptyFolderCount = package.EmptyFolders.Count;
                        EditorUtility.DisplayProgressBar(string.Format("正在打包{0}({1}/{2}) : (-/{5})  总计:({3}/{4})",
                                    package.PackageName, pi + 1, packagesCount, count + 1, total, emptyFolderCount),
                                    "Empty Folders", (float)count / total);
                        for (int i = 0; i < emptyFolderCount; i++)
                        {
                            zipStream.PutNextEntry(new ZipEntry(package.EmptyFolders[i] + "/") { });
                        }

                        //构建extra_info
                        BuildExtraInfoInZipStream(extraInfoFilePathInPackage, Path.GetFileNameWithoutExtension(package.FileName), zipStream);

                        //构建bundle_version
                        BuildBundleVersionInfoInZipStream(bundleVersionFilePathInPackage, package.Bundles, zipStream);

                        //以下为每个包中Patch和Addon独有内容
                        switch (Configs.configs.PackageMapConfig.PackageMode)
                        {
                            case "Patch":
                                //构建map
                                BuildMapInZipStream(mapFilePathInPackage, buffer, zipStream);
                                //添加Lua
                                BuildLuaInZipStream(buffer, zipStream);
                                break;

                            case "Addon":
                                break;
                            default:
                                throw new ApplicationException("不能识别模式：" + Configs.configs.PackageMapConfig.PackageMode);
                        }
                    }
                }
            }
        }

        private void BuildLuaInZipStream(byte[] buffer, ZipOutputStream zipStream)
        {
            switch (Configs.configs.PackageMapConfig.LuaSource)
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
                    throw new ApplicationException("不能识别Lua源：" + Configs.configs.PackageMapConfig.LuaSource);
            }
        }

        private void BuildMapInZipStream(string mapFilePath, byte[] buffer, ZipOutputStream zipStream)
        {
            //AddBytesToZipStream(zipStream, mapFilePath, System.Text.Encoding.Default.GetBytes(mapContent));
            AddFileToZipStream(zipStream, Path.Combine(Configs.configs.BundleInfoPath, "map"), mapFilePath, buffer);
        }

        private void BuildBundleVersionInfoInZipStream(string bundleVersionFilePath, List<string> bundles, ZipOutputStream zipStream)
        {
            string bundleVersion = Configs.g.bundleTree.BundleVersions.BundleVersion.ToString();
            List<BundleVersionStruct> bundleVersionList = new List<BundleVersionStruct>();
            foreach (var item in bundles)
            {
                bundleVersionList.Add(new BundleVersionStruct() { BundleName = item, Version = bundleVersion });
            }
            string bundleVersionContent = JsonConvert.SerializeObject(bundleVersionList, Formatting.Indented);
            AddBytesToZipStream(zipStream, bundleVersionFilePath, System.Text.Encoding.Default.GetBytes(bundleVersionContent));
        }

        private void BuildExtraInfoInZipStream(string extraInfoFilePath, string fileNameWithoutExtension, ZipOutputStream zipStream)
        {
            string extraInfoContent = JsonConvert.SerializeObject(new Dictionary<string, string>() {
                            { "brief_desc", fileNameWithoutExtension },
                            { "res_version", Configs.g.bundleTree.BundleVersions.ResourceVersion.ToString() }
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