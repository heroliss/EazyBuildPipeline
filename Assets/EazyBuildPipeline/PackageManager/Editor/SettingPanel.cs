using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.PackageManager.Editor
{
    [Serializable]
    public class SettingPanel
    {
        readonly int[] compressionLevelEnum = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        readonly string[] compressionLevelsEnumStr = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        [SerializeField] int[] selectedTagIndexs;
        [SerializeField] int selectedUserConfigIndex;
        [SerializeField] int selectedPackageModeIndex;
        [SerializeField] int selectedLuaSourceIndex;
        List<PackageTreeItem> wrongItems;
        List<PackageTreeItem> emptyItems;

        [SerializeField] GUIStyle dropdownStyle;
        [SerializeField] GUIStyle buttonStyle;
        [SerializeField] GUIStyle labelStyle;

        GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
        GUILayoutOption[] dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(80) };
        GUILayoutOption[] miniDropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(30) };
        GUILayoutOption[] miniButtonOptions = new GUILayoutOption[] { GUILayout.MaxWidth(24) };
        GUILayoutOption[] popupOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(200) };
        GUILayoutOption[] labelOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(100) };
        GUILayoutOption[] shortLabelOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(30) };
        GUILayoutOption[] shortLabelOptions2 = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(40) };

        [SerializeField] string[] userConfigNames = { };
        [SerializeField] bool creatingNewConfig;

        private void InitStyles()
        {
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            labelStyle = new GUIStyle(EditorStyles.label) { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };
        }

        public void Awake()
        {
            InitStyles();
            try
            {
                LoadAllConfigs();
            }
            catch (Exception e)
            {
                G.Module.DisplayDialog("加载配置文件时发生错误：" + e.Message);
            }
        }

        public void OnGUI(Rect rect)
        {
            if (creatingNewConfig == true && GUI.GetNameOfFocusedControl() != "InputField1")
            {
                creatingNewConfig = false;
            }
            GUILayout.BeginArea(rect, GUIContent.none);

            //SettingPanel
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Root:", GUILayout.Width(45));
            string path = EditorGUILayout.DelayedTextField(CommonModule.CommonConfig.Json.PipelineRootPath);
            if (GUILayout.Button("...", miniButtonOptions))
            {
                path = EditorUtility.OpenFolderPanel("打开根目录", CommonModule.CommonConfig.Json.PipelineRootPath, null);
            }
            if (!string.IsNullOrEmpty(path) && path != CommonModule.CommonConfig.Json.PipelineRootPath)
            {
                ChangeRootPath(path);
                return;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            if(ShowTagsDropdown()) return;
            GUILayout.FlexibleSpace();
            GUILayout.Space(10);
            if (GUILayout.Button(new GUIContent("New", "新建配置文件"), buttonStyle, buttonOptions))
            { ClickedNew(); return; }
            if (GUILayout.Button(new GUIContent("Save", "保存配置文件"), buttonStyle, buttonOptions))
            { ClickedSave(); return; }

            if (creatingNewConfig)
            {
                ShowInputField();
            }
            else
            {
                if (ShowMapDropdown()) return;
            }

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("ViewToolOrbit"), "查看该文件"), buttonStyle, GUILayout.Height(25)))
            { ClickedShowConfigFile(); return; }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Mode:", labelStyle, shortLabelOptions2);
            int currentPackageModeIndex_new = EditorGUILayout.Popup(selectedPackageModeIndex, G.PackageModeEnum, dropdownStyle, dropdownOptions);
            if (selectedPackageModeIndex != currentPackageModeIndex_new)
            {
                selectedPackageModeIndex = currentPackageModeIndex_new;
                G.Module.UserConfig.Json.PackageMode = G.PackageModeEnum[selectedPackageModeIndex];
                G.g.packageTree.UpdateAllFileName();
                G.Module.IsDirty = true;
                return;
            }
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Lua:", labelStyle, shortLabelOptions);
            int currentLuaSourceIndex_new = EditorGUILayout.Popup(selectedLuaSourceIndex, G.LuaSourceEnum, dropdownStyle, dropdownOptions);
            if (selectedLuaSourceIndex != currentLuaSourceIndex_new)
            {
                selectedLuaSourceIndex = currentLuaSourceIndex_new;
                G.Module.UserConfig.Json.LuaSource = G.LuaSourceEnum[selectedLuaSourceIndex];
                G.Module.IsDirty = true;
                return;
            }
            GUILayout.Space(10);
            EditorGUILayout.LabelField("CompressLevel:", labelStyle, labelOptions);
            int compressionLevel_new = EditorGUILayout.IntPopup(G.Module.UserConfig.Json.CompressionLevel, compressionLevelsEnumStr,
                compressionLevelEnum, dropdownStyle, miniDropdownOptions);
            if (compressionLevel_new != G.Module.UserConfig.Json.CompressionLevel)
            {
                G.Module.UserConfig.Json.CompressionLevel = compressionLevel_new;
                G.Module.IsDirty = true;
                return;
            }
            GUILayout.Space(20);
            if (GUILayout.Button(new GUIContent("Revert"), buttonStyle, buttonOptions))
            { ClickedRevert(); return; }
            if (GUILayout.Button(new GUIContent("Build"), buttonStyle, buttonOptions))
            { ClickedApply(); return; }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            //VersionPanel
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Resource Version: " + G.g.bundleTree.Versions.ResourceVersion, labelStyle, GUILayout.MaxWidth(150));
            GUILayout.FlexibleSpace();
            if (G.Module.UserConfig.Json.PackageMode == "Addon")
            {
                EditorGUILayout.LabelField("Addon Version:", labelStyle, GUILayout.MaxWidth(110));
                string addonVersion_new = EditorGUILayout.TextField(G.Module.ModuleStateConfig.Json.CurrentAddonVersion);
                {
                    if (!string.IsNullOrEmpty(addonVersion_new)) addonVersion_new = addonVersion_new.Trim();
                    if (addonVersion_new != G.Module.ModuleStateConfig.Json.CurrentAddonVersion)
                    {
                        G.Module.ModuleStateConfig.Json.CurrentAddonVersion = addonVersion_new;
                        G.g.packageTree.UpdateAllFileName();
                        return;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private bool ShowTagsDropdown()
        {
            int[] selectedIndexs_new = new int[CommonModule.CommonConfig.Json.TagEnum.Count];
            int i = 0;
            foreach (var tagType in CommonModule.CommonConfig.Json.TagEnum.Values)
            {
                selectedIndexs_new[i] = EditorGUILayout.Popup(selectedTagIndexs[i], tagType, dropdownStyle, dropdownOptions);
                if (selectedIndexs_new[i] != selectedTagIndexs[i])
                {
                    selectedTagIndexs[i] = selectedIndexs_new[i];
                    G.Module.ModuleStateConfig.Json.CurrentTag[i] = tagType[selectedTagIndexs[i]];
                    OnChangeTags();
                    return true;
                }
                i++;
            }
            return false;
        }

        private void OnChangeTags()
        {
            G.g.bundleTree.Reload();
            G.g.packageTree.ReConnectWithBundleTree();
            G.g.packageTree.UpdateAllFileName();
        }

        private void ClickedRevert()
        {
            ChangeUserConfig(selectedUserConfigIndex);
        }

        private void ClickedShowConfigFile()
        {
            string path = "";
            try
            {
                path = Path.Combine(G.Module.ModuleConfig.UserConfigsFolderPath, userConfigNames[selectedUserConfigIndex] + ".json");
            }
            catch { }
            if (!File.Exists(path))
            {
                path = G.Module.ModuleConfig.UserConfigsFolderPath;
            }
            EditorUtility.RevealInFinder(path);
        }

        private void ClickedApply()
        {
            G.Module.UserConfig.Json.Packages = GetPackageMap(); //从配置现场覆盖当前map
            if (!G.Runner.Check()) return;
            if (!CheckAllPackageItem()) return;

            bool ensure = EditorUtility.DisplayDialog("Build Packages", string.Format("确定应用当前配置？"),
                "确定", "取消");
            if (ensure)
            {
                try
                {
                    EditorUtility.DisplayProgressBar("Build Packages", "Starting...", 0);
                    double startTime = EditorApplication.timeSinceStartup;
                    G.Runner.ResourceVersion = G.g.bundleTree.Versions.ResourceVersion;
                    G.Runner.Run();

                    TimeSpan time = TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - startTime);
                    if (EditorUtility.DisplayDialog("Build Packages", "打包完成！用时：" + string.Format("{0}时 {1}分 {2}秒", time.Hours, time.Minutes, time.Seconds),
                        "显示文件", "关闭"))
                    {
                        string firstPackagePath = Path.Combine(G.Module.ModuleConfig.WorkPath, EBPUtility.GetTagStr(G.Module.ModuleStateConfig.Json.CurrentTag) +
                            "/" + G.g.packageTree.Packages[0].fileName);
                        EditorUtility.RevealInFinder(firstPackagePath);
                    }
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Build Packages", "打包时发生错误：" + e.Message, "确定");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }


        private bool CheckAllPackageItem()
        {
            //检查缺失项和空项
            wrongItems = new List<PackageTreeItem>();
            emptyItems = new List<PackageTreeItem>();
            foreach (PackageTreeItem item in G.g.packageTree.Packages)
            {
                RecursiveCheckItem(item);
            }
            //缺失提示
            if (wrongItems.Count != 0)
            {
                EditorUtility.DisplayDialog("提示", "发现" + wrongItems.Count + "个有问题的项，请修复后再应用", "确定");
                G.g.packageTree.FrameAndSelectItems(wrongItems);
                return false;
            }

            //检查Bundle是否全部加入Package或重复加入Package
            List<BundleTreeItem> omittedBundleList = new List<BundleTreeItem>();
            List<BundleTreeItem> repeatedBundleList = new List<BundleTreeItem>();
            foreach (var bundle in G.g.bundleTree.bundleDic.Values)
            {
                if (bundle.packageItems.Count == 0)
                {
                    omittedBundleList.Add(bundle);
                }
                else if (bundle.packageItems.Count > 1)
                {
                    repeatedBundleList.Add(bundle);
                }
            }
            if (G.Module.UserConfig.Json.PackageMode == "Addon") //仅在addon模式下提示遗漏bundle
            {
                if (omittedBundleList.Count != 0)
                {
                    if (!EditorUtility.DisplayDialog("提示", "发现" + omittedBundleList.Count + "个遗漏的Bundle，是否继续？", "继续", "返回"))
                    { G.g.bundleTree.FrameAndSelectItems(omittedBundleList); return false; }

                }
            }
            if (repeatedBundleList.Count != 0)
            {
                if (!EditorUtility.DisplayDialog("提示", "发现" + repeatedBundleList.Count + "个重复打包的Bundle，是否继续？", "继续", "返回"))
                { G.g.bundleTree.FrameAndSelectItems(repeatedBundleList); return false; }
            }

            //空项提示
            if (emptyItems.Count != 0)
            {
                if (!EditorUtility.DisplayDialog("提示", "发现" + emptyItems.Count + "个空文件夹或包，是否继续？",
                    "继续", "返回"))
                {
                    G.g.packageTree.FrameAndSelectItems(emptyItems);
                    return false;
                }
            }
            return true;
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

        private void ChangeRootPath(string path)
        {
            bool ensure = true;
            if (G.Module.IsDirty)
            {
                ensure = EditorUtility.DisplayDialog("改变根目录", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
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

        private void ChangeAllConfigsExceptRef(string pipelineRootPath)
        {
            Module newModule = new Module();
            if (!newModule.LoadAllConfigs(pipelineRootPath)) return;
            G.Module = newModule;
            G.Runner.Module = newModule;
            InitSelectedIndex();
            LoadUserConfigList();
            ConfigToIndex();
            CommonModule.CommonConfig.Save();
            HandleApplyingWarning();
            OnChangeRootPath();
        }

        private void OnChangeRootPath()
        {
            //必须bundletree先reload
            G.g.bundleTree.Reload();
            G.g.packageTree.Reload();
        }

        private void InitSelectedIndex()
        {
            selectedUserConfigIndex = -1;
            selectedLuaSourceIndex = -1;
            selectedPackageModeIndex = -1;
            selectedTagIndexs = new int[CommonModule.CommonConfig.Json.TagEnum.Count];
            for (int i = 0; i < selectedTagIndexs.Length; i++)
            {
                selectedTagIndexs[i] = -1;
            }
        }

        private void LoadAllConfigs()
        {
            G.Module.LoadAllConfigs();
            InitSelectedIndex();
            LoadUserConfigList();

            ConfigToIndex();
            HandleApplyingWarning();
        }
        private void LoadUserConfigList()
        {
            userConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.ModuleConfig.UserConfigsFolderPath);
        }
        private void HandleApplyingWarning()
        {
            if (G.Module.ModuleStateConfig.Json.Applying)
            {
                EditorUtility.DisplayDialog("提示", "上次执行打包时发生错误或被强制中断，可能导致产生不完整或错误的压缩包、在StreamingAssets下产生不完整或错误的文件，建议重新打包。", "确定");
            }
        }

        private void ConfigToIndex()
        {
            if (G.Module.ModuleStateConfig.Json.CurrentTag == null)
            {
                return;
            }
            int length = G.Module.ModuleStateConfig.Json.CurrentTag.Length;
            if (length > CommonModule.CommonConfig.Json.TagEnum.Count)
            {
                EditorUtility.DisplayDialog("提示", "欲加载的标签种类比全局标签种类多，请检查全局标签类型是否丢失", "确定");
            }
            else if (length < CommonModule.CommonConfig.Json.TagEnum.Count)
            {
                string[] originCurrentTags = G.Module.ModuleStateConfig.Json.CurrentTag;
                G.Module.ModuleStateConfig.Json.CurrentTag = new string[CommonModule.CommonConfig.Json.TagEnum.Count];
                originCurrentTags.CopyTo(G.Module.ModuleStateConfig.Json.CurrentTag, 0);
            }
            int i = 0;
            foreach (var item in CommonModule.CommonConfig.Json.TagEnum.Values)
            {
                selectedTagIndexs[i] = GetIndex(item, G.Module.ModuleStateConfig.Json.CurrentTag[i], i);
                i++;
            }
            if (G.Module.ModuleStateConfig.Json.CurrentUserConfigName == null)
            {
                return;
            }
            selectedUserConfigIndex = userConfigNames.IndexOf(G.Module.ModuleStateConfig.Json.CurrentUserConfigName.RemoveExtension());
            selectedLuaSourceIndex = G.LuaSourceEnum.IndexOf(G.Module.UserConfig.Json.LuaSource);
            selectedPackageModeIndex = G.PackageModeEnum.IndexOf(G.Module.UserConfig.Json.PackageMode);
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
            G.Module.DisplayDialog(string.Format("加载配置文件时发生错误：\n欲加载的类型“{0}”"
                  + "不存在于第 {1} 个全局类型枚举中！\n"
                  + "\n请检查配置文件：{2} 和全局类型配置文件：{3}  中的类型名是否匹配",
                  s, count, G.Module.ModuleStateConfig.JsonPath, CommonModule.CommonConfig.JsonPath));
            return -1;
        }

        private void ClickedSave()
        {
            bool ensure = true;
            if (G.Module.IsDirty)
            {
                ensure = EditorUtility.DisplayDialog("保存", "是否保存并覆盖原配置：" + userConfigNames[selectedUserConfigIndex], "覆盖保存", "取消");
            }
            if (!ensure) return;

            SaveCurrentMap();
        }

        private void SaveCurrentMap()
        {
            try
            {
                G.Module.UserConfig.Json.Packages = GetPackageMap();
                G.Module.UserConfig.Save();

                EditorUtility.DisplayDialog("保存", "保存配置成功！", "确定");
                G.Module.IsDirty = false; 
                
                G.g.OnChangeCurrentConfig(); //总控暂时用不上 设置Dirty也用不上
            }

            catch (Exception e)
            {
                EditorUtility.DisplayDialog("保存", "保存配置时发生错误：\n" + e.Message, "确定");
            }
        }

        //由于PackageMapConfig不会随Package的修改而更新，所以必须由此函数获取package信息
        public List<Configs.UserConfig.JsonClass.Package> GetPackageMap()
        {
            var packages = new List<Configs.UserConfig.JsonClass.Package>();
            foreach (var package in G.g.packageTree.Packages)
            {
                var p = new Configs.UserConfig.JsonClass.Package()
                {
                    Bundles = new List<string>(),
                    EmptyFolders = new List<string>(),
                    PackageName = package.displayName,
                    Color = ColorUtility.ToHtmlStringRGB(package.packageColor),
                    CopyToStreaming = package.copyToStreaming,
                    DeploymentLocation = package.deploymentLocation,
                    Necessery = package.necessery,
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

        private void RecursiveAddItem(PackageTreeItem packageItem, Configs.UserConfig.JsonClass.Package package)
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
                        string path = Path.Combine(G.Module.ModuleStateConfig.UserConfigsFolderPath, s + ".json");
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
            userConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.ModuleConfig.UserConfigsFolderPath);
            //保存
            G.Module.UserConfig.JsonPath = path;
            SaveCurrentMap();
            //切换
            G.Module.IsDirty = false;
            ChangeUserConfig(userConfigNames.IndexOf(name));
            //用于总控
            G.g.OnChangeConfigList();
        }

        private void ClickedNew()
        {
            creatingNewConfig = true;
            ShowInputField();
        }

        private bool ShowMapDropdown()
        {
            if (G.Module.IsDirty)
            {
                try
                {
                    userConfigNames[selectedUserConfigIndex] += "*";
                }
                catch { }
            }
            int selectedMapIndex_new = EditorGUILayout.Popup(selectedUserConfigIndex, userConfigNames, dropdownStyle, popupOptions);
            if (G.Module.IsDirty)
            {
                try
                {
                    userConfigNames[selectedUserConfigIndex] = userConfigNames[selectedUserConfigIndex].Remove(userConfigNames[selectedUserConfigIndex].Length - 1);
                }
                catch { }
            }
            if (selectedMapIndex_new != selectedUserConfigIndex)
            {
                ChangeUserConfig(selectedMapIndex_new);
                return true;
            }
            return false;
        }

        public void OnDestory()
        {
            if (G.Module.IsDirty)
            {
                bool result = false;
                try
                {
                    result = EditorUtility.DisplayDialog("PackageManager",
                        "当前配置未保存，是否保存并覆盖 \" " + userConfigNames[selectedUserConfigIndex] + " \" ?", "保存并退出", "直接退出");
                }
                catch { }
                if (result == true)
                {
                    SaveCurrentMap();
                }
            }
        }

        private void ChangeUserConfig(int selectedUserConfigIndex_new)
        {
            bool ensureLoad = true;
            if (G.Module.IsDirty)
            {
                ensureLoad = EditorUtility.DisplayDialog("切换配置", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
            }
            if (ensureLoad)
            {
                //try
                {
                    var newUserConfig = new Configs.UserConfig();
                    string newUserConfigFileName = userConfigNames[selectedUserConfigIndex_new] + ".json";
                    newUserConfig.JsonPath = Path.Combine(G.Module.ModuleConfig.UserConfigsFolderPath, newUserConfigFileName);
                    newUserConfig.Load();
                    //至此加载成功
                    G.Module.IsDirty = false;
                    G.Module.ModuleStateConfig.Json.CurrentUserConfigName = newUserConfigFileName;
                    G.Module.UserConfig = newUserConfig;
                    selectedUserConfigIndex = selectedUserConfigIndex_new;
                    ConfigToIndex();
                    OnChangeUserConfig();
                }
                //catch (Exception e)
                //{
                //    EditorUtility.DisplayDialog("切换map", "切换用户配置时发生错误：" + e.Message, "确定");
                //}
            }
        }

        private void OnChangeUserConfig()
        {
            G.g.bundleTree.ClearAllConnection();
            G.g.packageTree.Reload();
        }
    }
}