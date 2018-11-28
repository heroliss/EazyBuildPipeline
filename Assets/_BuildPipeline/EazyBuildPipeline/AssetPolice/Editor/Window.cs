using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System;
using System.Diagnostics;
using UnityEditor.IMGUI.Controls;
using Newtonsoft.Json;

namespace EazyBuildPipeline.AssetPolice.Editor
{
    public class Window : EditorWindow
    {
        [MenuItem("Window/EazyBuildPipeline/AssetPolice")]
        static void ShowWindow()
        {
            GetWindow<Window>("AssetPolice");
        }

        string configSearchText = "EazyBuildPipeline AssetPoliceConfig";
        ModuleConfig moduleConfig = new ModuleConfig();
        StateConfig stateConfig = new StateConfig();

        readonly string[] toggles = { "Garbage Collection", "Reverse Dependence" };
        int selectedToggle;
        private GUIStyle toggleStyle;

        MultiColumnHeaderState assetTreeHeaderState;
        private TreeViewState assetTreeViewState;
        AssetsTreeView assetTree;
        List<string> selectedAssets = new List<string>();

        bool freeze;
        Vector2 scrollPosition_ReverseDependencePanel;

        #region 分割条
        Rect splitterRect;
        Rect leftRect;
        Rect rightRect;
        bool resizingSplitter;
        const float headHeight = 20;
        const float fixedSpace = 3;
        const float splitterWidth = 3;
        const float margin = 3;

        private void HandleHorizontalResize()
        {
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
                resizingSplitter = true;

            if (resizingSplitter)
            {
                splitterRect.x = Event.current.mousePosition.x - splitterWidth / 2;
                leftRect.width = splitterRect.x - margin;
                rightRect.width = position.width - splitterRect.x - splitterWidth - margin;
                rightRect.x = splitterRect.x + splitterWidth;
            }

            if (Event.current.type == EventType.MouseUp)
            {
                resizingSplitter = false;
            }
        }
        #endregion
        private void InitStyles()
        {
            toggleStyle = new GUIStyle("toolbarbutton") { fixedHeight = 22, wordWrap = true };
        }
        private void Awake()
        {
            InitStyles();
            string[] guids = AssetDatabase.FindAssets(configSearchText);
            if (guids.Length == 0)
            {
                throw new EBPException("未能找到配置文件! 搜索文本：" + configSearchText);
            }
            string moduleConfigPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string configsFolderPath = Path.GetDirectoryName(moduleConfigPath);
            moduleConfig.Load(moduleConfigPath);
            stateConfig.Load(Path.Combine(configsFolderPath, moduleConfig.Json.StateConfigName));

            if (!string.IsNullOrEmpty(stateConfig.Json.CurrentMapFilePath) && File.Exists(stateConfig.Json.CurrentMapFilePath))
            {
                moduleConfig.AllBundles = JsonConvert.DeserializeObject<BundleRDDictionary>(File.ReadAllText(stateConfig.Json.CurrentMapFilePath));
            }

            assetTreeViewState = new TreeViewState();
            assetTreeHeaderState = AssetsTreeView.CreateDefaultHeaderState();

            #region InitRect
            splitterRect = new Rect(moduleConfig.Json.InitialLeftWidth + margin,
                headHeight, splitterWidth, position.height - headHeight - margin);
            leftRect = new Rect(margin, headHeight, splitterRect.x - margin, position.height - headHeight - margin);
            rightRect = new Rect(splitterRect.x + splitterWidth, headHeight,
                position.width - splitterRect.x - splitterWidth - margin, position.height - headHeight - margin);
            #endregion
        }

        private void OnEnable()
        {
            var assetMCHS = AssetsTreeView.CreateDefaultHeaderState();

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(assetTreeHeaderState, assetMCHS))
                MultiColumnHeaderState.OverwriteSerializedFields(assetTreeHeaderState, assetMCHS);
            assetTreeHeaderState = assetMCHS;

            assetTree = new AssetsTreeView(assetTreeViewState, new MultiColumnHeader(assetTreeHeaderState), moduleConfig, stateConfig);
        }

        private void OnGUI()
        {
            //这里当切换Panel时改变焦点，是用来解决当焦点在某个TextField上时输入框遗留显示的问题
            //GUI.SetNextControlName("Toggle1"); //如果有这句话则会影响到SettingsPanel中New时的输入框的焦点，使输入框不能显示
            using (new GUILayout.HorizontalScope())
            {
                int selectedToggle_new = GUILayout.Toolbar(selectedToggle, toggles, toggleStyle);
                if (selectedToggle_new != selectedToggle)
                {
                    selectedToggle = selectedToggle_new;
                    GUI.FocusControl("Toggle1"); //这里可能什么都没focus到，但是可以取消当前的focus
                }
                GUILayout.FlexibleSpace();
            }
            switch (toggles[selectedToggle])
            {
                case "Garbage Collection":
                    GarbageCollectionPanel();
                    break;
                case "Reverse Dependence":
                    ReverseDependencePanel();
                    break;
                default:
                    break;
            }
        }

        private void OnSelectionChange()
        {
            if (!freeze)
            {
                selectedAssets.Clear();
                foreach (string guid in Selection.assetGUIDs)
                {
                    selectedAssets.Add(AssetDatabase.GUIDToAssetPath(guid).ToLower());
                }
            }
            Repaint();
        }

        private void ReverseDependencePanel()
        {
            freeze = GUILayout.Toggle(freeze, "Freeze", freeze ? "sv_label_1" : "sv_label_0");
            using (var scrollViewScope = new GUILayout.ScrollViewScope(scrollPosition_ReverseDependencePanel))
            {
                scrollPosition_ReverseDependencePanel = scrollViewScope.scrollPosition;
                for (int i = 0; i < selectedAssets.Count; i++)
                {
                    using (new GUILayout.VerticalScope("GroupBox"))
                    {
                        string assetPath = selectedAssets[i];
                        if (moduleConfig.AllBundles.ContainsKey(assetPath))
                        {
                            int theReferencedAssetCount = moduleConfig.AllBundles[assetPath].RDBundles.Count;
                            using (new GUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button(new GUIContent((moduleConfig.AllBundles[assetPath].IsRDRoot ? " [R] " : " [" + theReferencedAssetCount + "] ") + assetPath,
                                    AssetDatabase.GetCachedIcon(assetPath)),
                                    freeze && (AssetDatabase.LoadMainAssetAtPath(assetPath) == Selection.activeObject) ? "LightmapEditorSelectedHighlight" : theReferencedAssetCount == 0 ? "HeaderLabel" : "BoldLabel", GUILayout.Height(20)))
                                {
                                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
                                }
                                GUILayout.FlexibleSpace();
                            }

                            foreach (var theReferencedAsset in moduleConfig.AllBundles[assetPath].RDBundles)
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    if (GUILayout.Button(new GUIContent(theReferencedAsset, AssetDatabase.GetCachedIcon(theReferencedAsset)),
                                        freeze && (AssetDatabase.LoadMainAssetAtPath(theReferencedAsset)) == Selection.activeObject ? "LightmapEditorSelectedHighlight" : "WhiteLabel", GUILayout.Height(18)))
                                    {
                                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(theReferencedAsset);
                                    }
                                    GUILayout.FlexibleSpace();
                                }
                            }
                        }
                        else
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button(new GUIContent(" [X] " + assetPath, AssetDatabase.GetCachedIcon(assetPath)),
                                    freeze && (AssetDatabase.LoadMainAssetAtPath(assetPath) == Selection.activeObject) ? "LightmapEditorSelectedHighlight" : "ErrorLabel", GUILayout.Height(20)))
                                {
                                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                }
            }
        }

        private void GarbageCollectionPanel()
        {
            #region 处理区域
            HandleHorizontalResize(); //拖动分割条时的处理
            if (resizingSplitter) Repaint();
            else //改变窗体尺寸时的处理
            {
                rightRect.width = position.width - splitterRect.x - splitterWidth - margin;
                leftRect.height = rightRect.height = splitterRect.height = position.height - headHeight - margin;
            }
            #endregion

            using (new GUILayout.AreaScope(leftRect))
            {
                EditorGUILayout.Separator();

                GUILayout.Label("Asset Directories:");
                moduleConfig.Json.AssetsDirectories = EditorGUILayout.TextArea(moduleConfig.Json.AssetsDirectories);
                EditorGUILayout.Separator();

                GUILayout.Label("Exclude Extensions When BuildBundles:");
                moduleConfig.Json.ExcludeExtensionsWhenBuildBundles = EditorGUILayout.TextArea(moduleConfig.Json.ExcludeExtensionsWhenBuildBundles);
                EditorGUILayout.Separator();

                GUILayout.Label("Exclude SubString When Find:");
                moduleConfig.Json.ExcludeSubStringWhenFind = EditorGUILayout.TextArea(moduleConfig.Json.ExcludeSubStringWhenFind);
                EditorGUILayout.Separator();

                GUILayout.Label("Dependence Root Directories:  (Use \"" + moduleConfig.Json.Separator + "\" Add Available Extension)", GUILayout.MinWidth(50));
                moduleConfig.Json.DependenceFilterDirectories = EditorGUILayout.TextArea(moduleConfig.Json.DependenceFilterDirectories);
                EditorGUILayout.Separator();

                GUILayout.Label("Dry Build Bundles OutPutPath:");
                GUILayout.BeginHorizontal();
                moduleConfig.Json.OutputPath = EditorGUILayout.TextField(moduleConfig.Json.OutputPath);
                if (GUILayout.Button("..", GUILayout.MaxWidth(20)))
                {
                    string s = EditorUtility.OpenFolderPanel("Open Folder", moduleConfig.Json.OutputPath, null);
                    if (s != "")
                    {
                        moduleConfig.Json.OutputPath = s;
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Separator();

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save") && EditorUtility.DisplayDialog("Save", "确定保存配置?", "确定", "取消"))
                    {
                        moduleConfig.Save();
                    }
                    if (GUILayout.Button("Build"))
                    {
                        try { Check(); }
                        catch (Exception e)
                        {
                            EditorUtility.DisplayDialog("Check Failed", e.Message, "确定");
                            return;
                        }
                        if (!Directory.Exists(moduleConfig.Json.OutputPath))
                        {
                            EditorUtility.DisplayDialog("错误", "输出目录不存在：" + moduleConfig.Json.OutputPath, "确定");
                            return;
                        }
                        //if (EditorUtility.DisplayDialog("Find No Reference Assets", "确定开始?", "确定", "取消"))
                        {
                            Run();
                        }
                    }
                }
            }
            assetTree.OnGUI(rightRect);
        }

        void Check()
        {
            //if (string.IsNullOrEmpty(configs.Json.OutputPath.Trim()))
            //{
            //    throw new EBPCheckFailedException("路径不能为空");
            //}

            //if (!Directory.Exists(configs.Json.OutputPath))
            //{
            //    throw new EBPCheckFailedException("找不到输出路径：" + configs.Json.OutputPath);
            //}
        }

        void Run()
        {
            string currentMapFilePath = string.IsNullOrEmpty(stateConfig.Json.CurrentMapFilePath) ? "ReverseDependenceMap.json" : stateConfig.Json.CurrentMapFilePath;
            string ReverseDependenceMapSavePath = EditorUtility.SaveFilePanel("Save Reverse Dependence Map", Path.GetDirectoryName(currentMapFilePath), Path.GetFileName(currentMapFilePath), "json");
            if (string.IsNullOrEmpty(ReverseDependenceMapSavePath))
            {
                return;
            }
            moduleConfig.AllBundles.Clear();
            //Create AssetBundleList
            List<AssetBundleBuild> assetBundleList = new List<AssetBundleBuild>();
            foreach (var folder in moduleConfig.Json.AssetsDirectories.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) //每一个给定的要创建Bundle的目录
            {
                var directory = "Assets/" + folder;
                foreach (var file in FindFiles(directory, moduleConfig.Json.ExcludeExtensionsWhenBuildBundles)) //每一个资产文件
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
            EditorUtility.DisplayProgressBar("", "Building...", 0);
            var manifest = BuildPipeline.BuildAssetBundles(moduleConfig.Json.OutputPath, assetBundleList.ToArray(), BuildAssetBundleOptions.DryRunBuild, EditorUserBuildSettings.activeBuildTarget);
            EditorUtility.ClearProgressBar();
            if (manifest == null)
            {
                EditorUtility.DisplayDialog("AssetPolice", "Dry Build AssetBundles Failed!", "确定");
                return;
            }

            //创建用来标记是否被引用的字典
            foreach (var bundle in manifest.GetAllAssetBundles()) //获取所有Bundle添加到字典
            {
                moduleConfig.AllBundles.Add(bundle, new BundleRDItem());
            }

            //标记是否被引用
            foreach (var item in moduleConfig.Json.DependenceFilterDirectories.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) //每一个给定要检查依赖的目录
            {
                if (string.IsNullOrEmpty(item)) continue;
                string[] s = item.Split(new[] { moduleConfig.Json.Separator }, StringSplitOptions.RemoveEmptyEntries);
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
                        moduleConfig.AllBundles[filePath].IsRDRoot = true;
                        foreach (string dependence in manifest.GetAllDependencies(filePath)) //添加每一个依赖的文件
                        {
                            moduleConfig.AllBundles[dependence].RDBundles.Add(filePath);
                        }
                    }
                }
            }

            //保存映射表
            File.WriteAllText(ReverseDependenceMapSavePath, JsonConvert.SerializeObject(moduleConfig.AllBundles, Formatting.Indented));
            stateConfig.Json.CurrentMapFilePath = ReverseDependenceMapSavePath;
            stateConfig.Save();

            assetTree.Reload();
            EditorUtility.DisplayDialog("AssetPolice", "Build Dependence Map Finish！", "确定");
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
