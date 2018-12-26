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

        Module module = new Module();
        Runner runner = new Runner();

        readonly string[] toggles = { "Garbage Collection", "Reverse Dependence" };
        int selectedToggle;
        private GUIStyle toggleStyle;

        MultiColumnHeaderState assetTreeHeaderState;
        private TreeViewState assetTreeViewState;
        AssetsTreeView assetTree;

        enum AssetRDState { Unknow, UnReferenced, Referenced, ReferenceRoot }
        class AssetRDItem { public string AssetPath; public string GUID; public AssetRDState State; public UnityEngine.Object MainAssetObj; }
        List<AssetRDItem> selectedAssets = new List<AssetRDItem>();

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

            module.LoadConfigs();
            if (!string.IsNullOrEmpty(module.StateConfig.Json.CurrentMapFilePath) && File.Exists(module.StateConfig.Json.CurrentMapFilePath))
            {
                module.ModuleConfig.AllBundles = JsonConvert.DeserializeObject<BundleRDDictionary>(File.ReadAllText(module.StateConfig.Json.CurrentMapFilePath));
            }

            assetTreeViewState = new TreeViewState();
            assetTreeHeaderState = AssetsTreeView.CreateDefaultHeaderState();

            #region InitRect
            splitterRect = new Rect(module.ModuleConfig.Json.InitialLeftWidth + margin,
                headHeight, splitterWidth, position.height - headHeight - margin);
            leftRect = new Rect(margin, headHeight, splitterRect.x - margin, position.height - headHeight - margin);
            rightRect = new Rect(splitterRect.x + splitterWidth, headHeight,
                position.width - splitterRect.x - splitterWidth - margin, position.height - headHeight - margin);
            #endregion
        }

        private void OnEnable()
        {
            runner.Module = module;
            var assetMCHS = AssetsTreeView.CreateDefaultHeaderState();

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(assetTreeHeaderState, assetMCHS))
                MultiColumnHeaderState.OverwriteSerializedFields(assetTreeHeaderState, assetMCHS);
            assetTreeHeaderState = assetMCHS;

            assetTree = new AssetsTreeView(assetTreeViewState, new MultiColumnHeader(assetTreeHeaderState), module.ModuleConfig, module.StateConfig);
        }

        private void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                int selectedToggle_new = GUILayout.Toolbar(selectedToggle, toggles, toggleStyle);
                if (selectedToggle_new != selectedToggle)
                {
                    selectedToggle = selectedToggle_new;
                    EditorGUIUtility.editingTextField = false;
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
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    selectedAssets.Add(new AssetRDItem()
                    {
                        GUID = guid,
                        AssetPath = assetPath,
                        State = !module.ModuleConfig.AllBundles.ContainsKey(guid) ? AssetRDState.Unknow :
                                module.ModuleConfig.AllBundles[guid].IsRDRoot ? AssetRDState.ReferenceRoot :
                                module.ModuleConfig.AllBundles[guid].RDBundles.Count == 0 ? AssetRDState.UnReferenced : AssetRDState.Referenced,
                        MainAssetObj = AssetDatabase.LoadMainAssetAtPath(assetPath)
                    });
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
                foreach (var assetItem in selectedAssets)
                {
                    using (new GUILayout.VerticalScope("GroupBox"))
                    {
                        switch (assetItem.State)
                        {
                            case AssetRDState.Unknow:
                                AssetItemPanel(assetItem, " [X] ", "ErrorLabel", "LightmapEditorSelectedHighlight");
                                break;
                            case AssetRDState.UnReferenced:
                                AssetItemPanel(assetItem, " [0] ", "HeaderLabel", "LightmapEditorSelectedHighlight");
                                break;
                            case AssetRDState.Referenced:
                                AssetItemPanel(assetItem, " [" + module.ModuleConfig.AllBundles[assetItem.GUID].RDBundles.Count + "] ", "BoldLabel", "LightmapEditorSelectedHighlight");
                                break;
                            case AssetRDState.ReferenceRoot:
                                AssetItemPanel(assetItem, " [R] ", "HeaderLabel", "LightmapEditorSelectedHighlight");
                                break;
                            default:
                                break;
                        }
                        if (assetItem.State != AssetRDState.Unknow)
                        {
                        foreach (var guid in module.ModuleConfig.AllBundles[assetItem.GUID].RDBundles)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            using (new GUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button(new GUIContent(assetPath, AssetDatabase.GetCachedIcon(assetPath)),
                                    freeze && (AssetDatabase.LoadMainAssetAtPath(assetPath)) == Selection.activeObject ? "LightmapEditorSelectedHighlight" : "WhiteLabel", GUILayout.Height(18)))
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
        }

        private void AssetItemPanel(AssetRDItem assetItem, string prefix, GUIStyle normalStyle, GUIStyle selectStyle)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent(prefix + assetItem.AssetPath, AssetDatabase.GetCachedIcon(assetItem.AssetPath)),
                    freeze && (assetItem.MainAssetObj == Selection.activeObject) ? selectStyle : normalStyle, GUILayout.Height(20)))
                {
                    Selection.activeObject = assetItem.MainAssetObj;
                }
                GUILayout.FlexibleSpace();
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
                module.ModuleConfig.Json.AssetsDirectories = EditorGUILayout.TextArea(module.ModuleConfig.Json.AssetsDirectories);
                EditorGUILayout.Separator();

                GUILayout.Label("Exclude Extensions When BuildBundles:");
                module.ModuleConfig.Json.ExcludeExtensionsWhenBuildBundles = EditorGUILayout.TextArea(module.ModuleConfig.Json.ExcludeExtensionsWhenBuildBundles);
                EditorGUILayout.Separator();

                GUILayout.Label("Exclude SubString When Find:");
                module.ModuleConfig.Json.ExcludeSubStringWhenFind = EditorGUILayout.TextArea(module.ModuleConfig.Json.ExcludeSubStringWhenFind);
                EditorGUILayout.Separator();

                GUILayout.Label("Dependence Root Directories:  (Use \"" + module.ModuleConfig.Json.Separator + "\" Add Available Extension)", GUILayout.MinWidth(50));
                module.ModuleConfig.Json.DependenceFilterDirectories = EditorGUILayout.TextArea(module.ModuleConfig.Json.DependenceFilterDirectories);
                EditorGUILayout.Separator();

                GUILayout.Label("Dry Build Bundles OutPutPath:");
                GUILayout.BeginHorizontal();
                module.ModuleConfig.Json.OutputPath = EditorGUILayout.TextField(module.ModuleConfig.Json.OutputPath);
                if (GUILayout.Button("..", GUILayout.MaxWidth(20)))
                {
                    string s = EditorUtility.OpenFolderPanel("Open Folder", module.ModuleConfig.Json.OutputPath, null);
                    if (s != "")
                    {
                        module.ModuleConfig.Json.OutputPath = s;
                    }
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Separator();

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save") && EditorUtility.DisplayDialog("Save", "确定保存配置?", "确定", "取消"))
                    {
                        module.ModuleConfig.Save();
                    }
                    if (GUILayout.Button("Build"))
                    {
                        try { Check(); }
                        catch (Exception e)
                        {
                            EditorUtility.DisplayDialog("Check Failed", e.Message, "确定");
                            return;
                        }
                        if (!Directory.Exists(module.ModuleConfig.Json.OutputPath))
                        {
                            EditorUtility.DisplayDialog("错误", "输出目录不存在：" + module.ModuleConfig.Json.OutputPath, "确定");
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
            string currentMapFilePath = string.IsNullOrEmpty(module.StateConfig.Json.CurrentMapFilePath) ? "ReverseDependenceMap.json" : module.StateConfig.Json.CurrentMapFilePath;
            string ReverseDependenceMapSavePath = EditorUtility.SaveFilePanel("Save Reverse Dependence Map", Path.GetDirectoryName(currentMapFilePath), Path.GetFileName(currentMapFilePath), "json");
            if (string.IsNullOrEmpty(ReverseDependenceMapSavePath))
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("AssetPolice", "Building...", 0);
                runner.Run();

                //保存映射表
                File.WriteAllText(ReverseDependenceMapSavePath, JsonConvert.SerializeObject(module.ModuleConfig.AllBundles, Formatting.Indented));
                module.StateConfig.Json.CurrentMapFilePath = ReverseDependenceMapSavePath;
                module.StateConfig.Save();

                assetTree.Reload();
                EditorUtility.DisplayDialog("AssetPolice", "Build Dependence Map Finish！", "确定");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("AssetPolice", "Build Failed: " + e.Message, "确定");
                throw e;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
