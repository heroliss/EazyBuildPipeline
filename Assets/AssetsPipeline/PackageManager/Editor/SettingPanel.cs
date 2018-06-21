﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LiXuFeng.PackageManager.Editor
{
    public class SettingPanel
    {
        string[] PackageModes = new string[] { "Addon", "Patch" };
        string[] luaSources = new string[] { "None", "Origin", "ByteCode", "Encrypted" };
        int[] compressionLevels = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        string[] compressionLevelsStr = new string[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        int[] selectedTagIndexs;
        int selectedMapIndex;
        private int selectedPackageModeIndex;
        private int selectedLuaSourceIndex;
        List<PackageTreeItem> wrongItems;
        List<PackageTreeItem> emptyItems;

        bool firstShow = true;
        private GUIStyle dropdownStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        private GUILayoutOption[] buttonOptions;
        private GUILayoutOption[] dropdownOptions;
        private GUILayoutOption[] miniDropdownOptions;
        private GUILayoutOption[] miniButtonOptions;
        private GUILayoutOption[] popupOptions;
        private GUILayoutOption[] labelOptions;
        private GUILayoutOption[] shortLabelOptions;
        private List<string> savedConfigNames = new List<string>();
        private bool creatingNewConfig;

        private void InitStyles()
        {
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            labelStyle = new GUIStyle(EditorStyles.label) { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };
            buttonOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
            dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(80) };
            miniDropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(30) };
            miniButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(24) };
            popupOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(200) };
            labelOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(55) };
            shortLabelOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(30) };
        }

        public void OnEnable()
        {
            try
            {
                LoadAllConfigs();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "加载配置文件时发生错误：" + e.Message, "确定");
            }
        }

        public void OnGUI(Rect rect)
        {
            if (firstShow)
            {
                InitStyles();
                firstShow = false;
            }
            if (GUI.GetNameOfFocusedControl() != "InputField1")
            {
                creatingNewConfig = false;
            }
            using (new GUILayout.AreaScope(rect, new GUIContent()))
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("根目录:", GUILayout.Width(45));
                        string path = EditorGUILayout.DelayedTextField(Configs.configs.LocalConfig.RootPath);
                        if (GUILayout.Button("...", miniButtonOptions))
                        {
                            path = EditorUtility.OpenFolderPanel("打开根目录", Configs.configs.LocalConfig.RootPath, null);
                        }
                        ChangeRootPathIfChanged(path);
                    }
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        ShowTagsDropdown();
                        GUILayout.FlexibleSpace();
                        GUILayout.Space(10);
                        if (GUILayout.Button(new GUIContent("New", "新建配置文件"), buttonStyle, buttonOptions)) ClickedNew();
                        if (GUILayout.Button(new GUIContent("Save", "保存配置文件"), buttonStyle, buttonOptions)) ClickedSave();
                        {
                            if (creatingNewConfig)
                            {
                                ShowInputField();
                            }
                            else
                            {
                                ShowMapDropdown();
                            }
                        }

                        if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("ViewToolOrbit"), "查看该文件"), buttonStyle, GUILayout.Height(25)))
                            ClickedShowConfigFile();
                    }
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("模式:", labelStyle, shortLabelOptions);
                        int currentPackageModeIndex_new = EditorGUILayout.Popup(selectedPackageModeIndex, PackageModes, dropdownStyle, dropdownOptions);
                        if (selectedPackageModeIndex != currentPackageModeIndex_new)
                        {
                            selectedPackageModeIndex = currentPackageModeIndex_new;
                            Configs.configs.PackageMapConfig.PackageMode = PackageModes[selectedPackageModeIndex];
                            Configs.g.packageTree.UpdateAllFileName();
                            Configs.g.packageTree.Dirty = true;
                        }
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("Lua:", labelStyle, shortLabelOptions);
                        int currentLuaSourceIndex_new = EditorGUILayout.Popup(selectedLuaSourceIndex, luaSources, dropdownStyle, dropdownOptions);
                        if (selectedLuaSourceIndex != currentLuaSourceIndex_new)
                        {
                            selectedLuaSourceIndex = currentLuaSourceIndex_new;
                            Configs.configs.PackageMapConfig.LuaSource = luaSources[selectedLuaSourceIndex];
                            Configs.g.packageTree.Dirty = true;
                        }
                        GUILayout.Space(10);
                        EditorGUILayout.LabelField("压缩等级:", labelStyle, labelOptions);
                        int compressionLevel_new = EditorGUILayout.IntPopup(Configs.configs.PackageMapConfig.CompressionLevel, compressionLevelsStr,
                            compressionLevels, dropdownStyle, miniDropdownOptions);
                        if (compressionLevel_new != Configs.configs.PackageMapConfig.CompressionLevel)
                        {
                            Configs.configs.PackageMapConfig.CompressionLevel = compressionLevel_new;
                            Configs.g.packageTree.Dirty = true;
                        }
                        GUILayout.Space(20);
                        if (GUILayout.Button(new GUIContent("Revert"), buttonStyle, buttonOptions)) ClickedRevert();
                        if (GUILayout.Button(new GUIContent("Apply"), buttonStyle, buttonOptions)) ClickedApply();
                    }
                    GUILayout.FlexibleSpace();
                }
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Resource Version: " + Configs.g.bundleTree.BundleVersions.ResourceVersion, labelStyle, GUILayout.MaxWidth(150));
                    EditorGUILayout.LabelField("  Bundle Version: " + Configs.g.bundleTree.BundleVersions.BundleVersion, labelStyle, GUILayout.MaxWidth(150));
                    GUILayout.FlexibleSpace();
                    if (Configs.configs.PackageMapConfig.PackageMode == "Addon")
                    {
                        EditorGUILayout.LabelField("Package Version:", labelStyle, GUILayout.MaxWidth(110));
                        string packageVersion_new = EditorGUILayout.TextField(Configs.configs.PackageMapConfig.PackageVersion);
                        {
                            if (packageVersion_new != Configs.configs.PackageMapConfig.PackageVersion)
                            {
                                Configs.configs.PackageMapConfig.PackageVersion = packageVersion_new;
                                Configs.g.packageTree.UpdateAllFileName();
                                Configs.g.packageTree.Dirty = true;
                            }
                        }
                    }
                }
            }
        }

        private void ShowTagsDropdown()
        {
            int[] selectedIndexs_new = new int[Configs.configs.TagEnumConfig.Tags.Count];
            int i = 0;
            foreach (var tagType in Configs.configs.TagEnumConfig.Tags.Values)
            {
                selectedIndexs_new[i] = EditorGUILayout.Popup(selectedTagIndexs[i], tagType, dropdownStyle, dropdownOptions);
                if (selectedIndexs_new[i] != selectedTagIndexs[i])
                {
                    selectedTagIndexs[i] = selectedIndexs_new[i];
                    Configs.configs.PackageConfig.CurrentTags[i] = tagType[selectedTagIndexs[i]];
                    OnChangeTags();
                }
                i++;
            }
        }

        private void OnChangeTags()
        {
            Configs.g.bundleTree.Reload();
            Configs.g.packageTree.ReConnectWithBundleTree();
            Configs.g.packageTree.UpdateAllFileName();
        }

        private void ClickedRevert()
        {
            ChangeMap(selectedMapIndex);
        }

        private void ClickedShowConfigFile()
        {
            string path = "";
            try
            {
                path = Path.Combine(Configs.configs.LocalConfig.PackageMapsFolderPath, savedConfigNames[selectedMapIndex] + ".json");
            }
            catch { }
            if (!File.Exists(path))
            {
                path = Configs.configs.LocalConfig.PackageMapsFolderPath;
            }
            EditorUtility.RevealInFinder(path);
        }

        private void ClickedApply()
        {
            if (CheckAllPackageItem())
            {
                bool ensure = EditorUtility.DisplayDialog("Package", string.Format("确定应用当前配置？\n\n压缩程度：{0}",
                    Configs.configs.PackageMapConfig.CompressionLevel),
                    "确定", "取消");
                if (ensure)
                {
                    try
                    {
                        EditorUtility.DisplayProgressBar("Build Packages", "Starting...", 0);
                        float startTime = Time.realtimeSinceStartup;

                        Configs.configs.PackageConfig.Applying = true;
                        Configs.configs.PackageConfig.Save();
                        ApplyAllPackages();
                        Configs.configs.PackageConfig.Applying = false;
                        Configs.configs.PackageConfig.Save();

                        EditorUtility.ClearProgressBar();
                        TimeSpan time = TimeSpan.FromSeconds(Time.realtimeSinceStartup - startTime);
                        if (EditorUtility.DisplayDialog("Build Packages", "打包完成！用时：" + string.Format("{0}时 {1}分 {2}秒", time.Hours, time.Minutes, time.Seconds),
                            "显示文件", "关闭"))
                        {
                            string firstPackagePath = Path.Combine(Configs.configs.LocalConfig.PackagePath, Configs.configs.Tag +
                                "/" + Configs.g.packageTree.Packages[0].displayName + Configs.configs.LocalConfig.PackageExtension);
                            EditorUtility.RevealInFinder(firstPackagePath);
                        }
                    }
                    catch (Exception e)
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Build Packages", "打包时发生错误：" + e.Message, "确定");
                    }
                }
            }
        }

        private void ApplyAllPackages()
        {
            float lastTime = Time.realtimeSinceStartup;
            string bundlesFolderPath = Path.Combine(Configs.configs.LocalConfig.BundlePath, Configs.configs.Tag);
            string packagesFolderPath = Path.Combine(Configs.configs.LocalConfig.PackagePath, Configs.configs.Tag);
            var packageMap = GetPackageMap();
            int count = 0;
            int total = 0;
            foreach (var package in packageMap)
            {
                total += package.Bundles.Count;
            }
            int packagesCount = packageMap.Count;

            EditorUtility.DisplayProgressBar("Build Packages", "正在重建目录:" + packagesFolderPath, 0);
            if (Directory.Exists(packagesFolderPath))
            {
                Directory.Delete(packagesFolderPath, true);
            }
            Directory.CreateDirectory(packagesFolderPath);

            byte[] buffer = new byte[20971520]; //20M缓存,不够会自动扩大
            for (int pi = 0; pi < packagesCount; pi++)
            {
                var package = packageMap[pi];
                using (FileStream zipFileStream = new FileStream(Path.Combine(packagesFolderPath, package.PackageName + Configs.configs.LocalConfig.PackageExtension), FileMode.Create))
                {
                    using (ZipOutputStream zipStream = new ZipOutputStream(zipFileStream))
                    {
                        zipStream.SetLevel(Configs.configs.PackageMapConfig.CompressionLevel);
                        int bundlesCount = package.Bundles.Count;
                        for (int i = 0; i < bundlesCount; i++)
                        {
                            var bundleManifestRelativePath = package.Bundles[i];
                            string bundleRelativePath = bundleManifestRelativePath.Remove(bundleManifestRelativePath.Length - 9, 9);
                            //string bundleManifestPath = Path.Combine(bundlesFolderPath, bundleManifestRelativePath);
                            string bundlePath = Path.Combine(bundlesFolderPath, bundleRelativePath);
                            if (Time.realtimeSinceStartup - lastTime > 0.06f)
                            {
                                EditorUtility.DisplayProgressBar(string.Format("正在打包{0}({1}/{2}) : ({3}/{4})  总计:({5}/{6})",
                                    package.PackageName, pi + 1, packagesCount, i + 1, bundlesCount, count + 1, total),
                                    bundleRelativePath, (float)count / total);
                                lastTime = Time.realtimeSinceStartup;
                            }
                            //AddFileToZipStream(zipStream, bundleManifestPath, bundleManifestRelativePath);
                            AddFileToZipStream(zipStream, bundlePath, bundleRelativePath, buffer);
                            count++;
                        }

                        int emptyFolderCount = package.EmptyFolders.Count;
                        EditorUtility.DisplayProgressBar(string.Format("正在打包{0}({1}/{2}) : (-/{5})  总计:({3}/{4})",
                                    package.PackageName, pi + 1, packagesCount, count + 1, total, emptyFolderCount),
                                    "Empty Folders", (float)count / total);

                        for (int i = 0; i < emptyFolderCount; i++)
                        {
                            zipStream.PutNextEntry(new ZipEntry(package.EmptyFolders[i] + "/") { });
                        }
                    }
                }
            }
        }

        private static void AddFileToZipStream(ZipOutputStream zipStream, string sourceFilePath, string targetPathInZip, byte[] buffer)
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

        private bool CheckAllPackageItem()
        {
            if (Configs.g.packageTree.Packages.Count == 0)
            {
                return false;
            }
            wrongItems = new List<PackageTreeItem>();
            emptyItems = new List<PackageTreeItem>();
            foreach (PackageTreeItem item in Configs.g.packageTree.Packages)
            {
                RecursiveCheckItem(item);
            }
            if (wrongItems.Count != 0)
            {
                EditorUtility.DisplayDialog("提示", "发现" + wrongItems.Count + "个有问题的项，请修复后再应用", "确定");
                FrameAndSelectPackageTreeItems(wrongItems);
                return false;
            }
            if (emptyItems.Count != 0)
            {
                if (!EditorUtility.DisplayDialog("提示", "发现" + emptyItems.Count + "个空文件夹或包，是否继续？",
                    "继续打包", "返回"))
                {
                    FrameAndSelectPackageTreeItems(emptyItems);
                    return false;
                }
            }
            return true;
        }

        private void FrameAndSelectPackageTreeItems(List<PackageTreeItem> items)
        {
            int selectItemsCount = Mathf.Min(items.Count, 1000);
            var ids = new int[selectItemsCount];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = items[i].id;
                Configs.g.packageTree.FrameItem(ids[i]);
            }
            Configs.g.packageTree.SetSelection(ids);
            Configs.g.packageTree.SetFocus();
        }

        private void RecursiveCheckItem(PackageTreeItem packageItem)
        {
            if (packageItem.hasChildren)
            {
                foreach (PackageTreeItem item in packageItem.children)
                {
                    RecursiveCheckItem(item);
                }
            }
            else
            {
                if (packageItem.isPackage)
                {
                    emptyItems.Add(packageItem);
                }
                else if (packageItem.lost)
                {
                    wrongItems.Add(packageItem);
                }
                else if (packageItem.bundleItem.isFolder)
                {
                    emptyItems.Add(packageItem);
                }
                else if (!packageItem.bundleItem.verify)
                {
                    wrongItems.Add(packageItem);
                }
            }
        }

        private void ChangeRootPathIfChanged(string path)
        {
            if (!string.IsNullOrEmpty(path) && path != Configs.configs.LocalConfig.RootPath)
            {
                bool ensure = true;
                if (Configs.g.packageTree.Dirty)
                {
                    ensure = !EditorUtility.DisplayDialog("改变根目录", "更改未保存，是否要放弃更改？", "返回", "放弃保存");
                }
                if (ensure)
                {
                    try
                    {
                        ChangeAllConfigsExceptRef(path);
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("错误", "更换根目录时发生错误：" + e.ToString(), "确定");
                    }
                }
            }
        }

        private void ChangeAllConfigsExceptRef(string rootPath)
        {
            //使用newConfigs加载确保发生异常后不修改原configs
            Config.Configs newConfigs = new Config.Configs();
            newConfigs.LoadLocalConfig();
            newConfigs.LocalConfig.RootPath = rootPath;
            if (!newConfigs.LoadAllConfigsByLocalConfig()) return;
            Configs.configs = newConfigs;
            InitSelectedIndex();
            LoadMaps();
            ConfigToIndex();
            Configs.configs.LocalConfig.Save();
            HandleApplyingWarning();
            OnChangeRootPath();
        }

        private void OnChangeRootPath()
        {
            //必须bundletree先reload
            Configs.g.bundleTree.Reload();
            Configs.g.packageTree.Reload();
        }

        private void InitSelectedIndex()
        {
            selectedMapIndex = -1;
            selectedLuaSourceIndex = -1;
            selectedPackageModeIndex = -1;
            selectedTagIndexs = new int[Configs.configs.TagEnumConfig.Tags.Count];
            for (int i = 0; i < selectedTagIndexs.Length; i++)
            {
                selectedTagIndexs[i] = -1;
            }
        }

        private void LoadAllConfigs()
        {
            Configs.configs.LoadLocalConfig();
            Configs.configs.LoadAllConfigsByLocalConfig();
            InitSelectedIndex();
            LoadMaps();
            ConfigToIndex();
            HandleApplyingWarning();
        }

        private void LoadMaps()
        {
            try
            {
                if (!string.IsNullOrEmpty(Configs.configs.PackageConfig.CurrentPackageMap))
                {
                    string mapsFolderPath = Configs.configs.LocalConfig.PackageMapsFolderPath;
                    string currentMapPath = Path.Combine(mapsFolderPath, Configs.configs.PackageConfig.CurrentPackageMap);
                    Configs.configs.PackageMapConfig.Path = currentMapPath;
                    Configs.configs.PackageMapConfig.Load();
                }
                else
                {
                    Configs.configs.PackageConfig.CurrentPackageMap = null;
                    Configs.configs.PackageMapConfig.Path = null;
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "载入映射文件：" + Configs.configs.PackageConfig.CurrentPackageMap + " 时发生错误：" + e.Message, "确定");
                Configs.configs.PackageConfig.CurrentPackageMap = null;
                Configs.configs.PackageMapConfig.Path = null;
            }
            savedConfigNames = new List<string>();
            FindSavedConfigs(Configs.configs.LocalConfig.PackageMapsFolderPath);
        }

        private void FindSavedConfigs(string path)
        {
            try
            {
                List<string> savedConfigPaths = new List<string>();
                savedConfigNames = new List<string>();
                RecursiveFindJson(path, savedConfigPaths);
                foreach (var configPath in savedConfigPaths)
                {
                    string extension = Path.GetExtension(configPath);
                    savedConfigNames.Add(configPath.Remove(configPath.Length - extension.Length, extension.Length).Remove(0, path.Length + 1).Replace('\\', '/'));
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", "查找映射文件列表时发生错误：" + e.Message, "确定");
            }
        }

        private static void RecursiveFindJson(string path, List<string> jsonPaths)
        {
            jsonPaths.AddRange(Directory.GetFiles(path, "*.json"));
            foreach (var folder in Directory.GetDirectories(path))
            {
                RecursiveFindJson(folder, jsonPaths);
            }
        }

        private void HandleApplyingWarning()
        {
            if (Configs.configs.PackageConfig.Applying)
            {
                EditorUtility.DisplayDialog("提示", "即将打开的这个配置在上次应用时被异常中断（可能是死机，停电等原因）" +
                    "，建议重新应用该配置", "确定");
            }
        }

        private void ConfigToIndex()
        {
            if (Configs.configs.PackageConfig.CurrentTags == null)
            {
                return;
            }
            int length = Configs.configs.PackageConfig.CurrentTags.Length;
            if (Configs.configs.PackageConfig.CurrentTags.Length > Configs.configs.TagEnumConfig.Tags.Count)
            {
                length = Configs.configs.TagEnumConfig.Tags.Count;
                EditorUtility.DisplayDialog("提示", "欲加载的标签种类比全局标签种类多，请检查全局标签类型是否丢失", "确定");
            }
            else
            {
                string[] originCurrentTags = Configs.configs.PackageConfig.CurrentTags;
                Configs.configs.PackageConfig.CurrentTags = new string[Configs.configs.TagEnumConfig.Tags.Count];
                originCurrentTags.CopyTo(Configs.configs.PackageConfig.CurrentTags, 0);
            }
            int i = 0;
            foreach (var item in Configs.configs.TagEnumConfig.Tags.Values)
            {
                selectedTagIndexs[i] = GetIndex(item, Configs.configs.PackageConfig.CurrentTags[i], i);
                i++;
            }
            if (Configs.configs.PackageConfig.CurrentPackageMap == null)
            {
                return;
            }
            string extension = Path.GetExtension(Configs.configs.PackageConfig.CurrentPackageMap);
            selectedMapIndex = savedConfigNames.IndexOf(Configs.configs.PackageConfig.CurrentPackageMap.Remove(
                Configs.configs.PackageConfig.CurrentPackageMap.Length - extension.Length, extension.Length));
            selectedLuaSourceIndex = luaSources.IndexOf(Configs.configs.PackageMapConfig.LuaSource);
            selectedPackageModeIndex = PackageModes.IndexOf(Configs.configs.PackageMapConfig.PackageMode);
        }
        
        private int GetIndex(string[] sList, string s, int count)
        {
            if (string.IsNullOrEmpty(s)) return -1;
            for (int i = 0; i < sList.Length; i++)
            {
                if (s == sList[i])
                {
                    return i;
                }
            }
            EditorUtility.DisplayDialog("错误", string.Format("加载配置文件时发生错误：\n欲加载的类型“{0}”"
                  + "不存在于第 {1} 个全局类型枚举中！\n"
                  + "\n请检查配置文件：{2} 和全局类型配置文件：{3}  中的类型名是否匹配",
                  s, count, Configs.configs.PackageConfig.Path, Configs.configs.TagEnumConfig.Path), "确定");
            return -1;
        }

        private void ClickedSave()
        {
            bool ensure = true;
            if (Configs.g.packageTree.Dirty)
            {
                ensure = EditorUtility.DisplayDialog("保存", "是否保存并覆盖原配置：" + savedConfigNames[selectedMapIndex], "覆盖保存", "取消");
            }
            if (!ensure) return;

            SaveCurrentMap();
        }

        private void SaveCurrentMap()
        {
            try
            {
                List<Config.PackageMapConfig.Package> packages = GetPackageMap();
                Configs.configs.PackageMapConfig.Packages = packages;
                Configs.configs.PackageMapConfig.Save();

                EditorUtility.DisplayDialog("保存", "保存Package树成功！", "确定");
                Configs.g.packageTree.Dirty = false;
            }

            catch (Exception e)
            {
                EditorUtility.DisplayDialog("保存", "保存Package树时发生错误：\n" + e.Message, "确定");
            }
        }

        private List<Config.PackageMapConfig.Package> GetPackageMap()
        {
            var packages = new List<Config.PackageMapConfig.Package>();
            foreach (var package in Configs.g.packageTree.Packages)
            {
                var p = new Config.PackageMapConfig.Package()
                {
                    Bundles = new List<string>(),
                    EmptyFolders = new List<string>(),
                    PackageName = package.displayName,
                    Color = ColorUtility.ToHtmlStringRGB(package.packageColor),
                    CopyToStreaming = package.copyToStreaming,
                    DeploymentLocation = package.deploymentLocation,
                    Necessery = package.necessery
                };
                if (package.hasChildren)
                {
                    foreach (PackageTreeItem packageItem in package.children)
                    {
                        RecursiveAddItem(packageItem, p);
                    }
                }
                packages.Add(p);
            }

            return packages;
        }

        private void RecursiveAddItem(PackageTreeItem packageItem, Config.PackageMapConfig.Package package)
        {
            if (packageItem.hasChildren)
            {
                foreach (PackageTreeItem item in packageItem.children)
                {
                    RecursiveAddItem(item, package);
                }
            }
            else
            {
                if (!packageItem.bundleItem.isFolder)
                {
                    package.Bundles.Add(packageItem.bundleItem.relativePath);
                }
                else
                {
                    package.EmptyFolders.Add(packageItem.bundleItem.relativePath);
                }
            }
        }
        private void ShowInputField()
        {
            GUI.SetNextControlName("InputField1");
            string tip = "<输入名称>(回车确定，空串取消)";
            string s = EditorGUILayout.DelayedTextField(tip, dropdownStyle, popupOptions);
            GUI.FocusControl("InputField1");
            s = s.Trim().Replace('\\', '/');
            if (s != tip)
            {
                if (s != "")
                {
                    try
                    {
                        string path = Path.Combine(Configs.configs.LocalConfig.PackageMapsFolderPath, s + ".json");
                        if (File.Exists(path))
                            EditorUtility.DisplayDialog("创建失败", "创建新文件失败，该名称已存在！", "确定");
                        else
                        {
                            CreateNewMap(s, path);
                        }
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("创建失败", "创建时发生错误：" + e.Message, "确定");
                    }
                }
                creatingNewConfig = false;
            }
        }

        private void CreateNewMap(string name, string path)
        {
            //新建
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Create(path).Close();
            EditorUtility.DisplayDialog("创建成功", "创建成功!", "确定");
            //更新列表
            FindSavedConfigs(Configs.configs.LocalConfig.PackageMapsFolderPath);
            //保存
            Configs.configs.PackageMapConfig.Path = path;
            SaveCurrentMap();
            //切换
            Configs.g.packageTree.Dirty = false;
            ChangeMap(savedConfigNames.ToArray().IndexOf(name));
        }

        private void ClickedNew()
        {
            creatingNewConfig = true;
        }

        private void ShowMapDropdown()
        {
            if (Configs.g.packageTree.Dirty)
            {
                try
                {
                    savedConfigNames[selectedMapIndex] += "*";
                }
                catch { }
            }
            int selectedMapIndex_new = EditorGUILayout.Popup(selectedMapIndex, savedConfigNames.ToArray(), dropdownStyle, popupOptions);
            if (Configs.g.packageTree.Dirty)
            {
                try
                {
                    savedConfigNames[selectedMapIndex] = savedConfigNames[selectedMapIndex].Remove(savedConfigNames[selectedMapIndex].Length - 1);
                }
                catch { }
            }
            if (selectedMapIndex_new != selectedMapIndex)
            {
                ChangeMap(selectedMapIndex_new);
            }
        }

        public void OnDisable()
        {
            if (Configs.g.packageTree.Dirty)
            {
                bool result = false;
                try
                {
                    result = EditorUtility.DisplayDialog("PackageManager",
                        "当前文件映射未保存，是否保存并覆盖 \" " + savedConfigNames[selectedMapIndex] + " \" ?", "保存并退出", "直接退出");
                }
                catch { }
                if (result == true)
                {
                    SaveCurrentMap();
                }
            }
        }

        private void ChangeMap(int selectedMapIndex_new)
        {
            bool ensureLoad = true;
            if (Configs.g.packageTree.Dirty)
            {
                ensureLoad = !EditorUtility.DisplayDialog("切换配置", "更改未保存，是否要放弃更改？", "返回", "放弃保存");
            }
            if (ensureLoad)
            {
                try
                {
                    var newPackageMapConfig = new Config.PackageMapConfig();
                    string newPackageMap = savedConfigNames[selectedMapIndex_new] + ".json";
                    newPackageMapConfig.Path = Path.Combine(Configs.configs.LocalConfig.PackageMapsFolderPath, newPackageMap);
                    newPackageMapConfig.Load();
                    //至此加载成功
                    Configs.configs.PackageConfig.CurrentPackageMap = newPackageMap;
                    Configs.configs.PackageMapConfig = newPackageMapConfig;
                    selectedMapIndex = selectedMapIndex_new;
                    ConfigToIndex();
                    OnChangeMap();
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("切换map", "切换Map配置时发生错误：" + e.Message, "确定");
                }
            }
        }

        private void OnChangeMap()
        {
            Configs.g.bundleTree.ClearAllConnection();
            Configs.g.packageTree.Reload();
        }
    }
}
