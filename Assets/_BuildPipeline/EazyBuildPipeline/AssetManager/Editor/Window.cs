using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System;
using System.Diagnostics;
using UnityEditor.IMGUI.Controls;

namespace EazyBuildPipeline.AssetManager.Editor
{
    public class Window : EditorWindow
    {
        [MenuItem("Window/EazyBuildPipeline/AssetManager")]
        static void ShowWindow()
        {
            GetWindow<Window>("Finder");
        }

        string configSearchText = "EazyBuildPipeline AssetManagerConfig";
        Configs configs = new Configs();
        MultiColumnHeaderState assetTreeHeaderState;
        private TreeViewState assetTreeViewState;
        AssetsTreeView assetTree;

        private void Awake()
        {
            string[] guids = AssetDatabase.FindAssets(configSearchText);
            if (guids.Length == 0)
            {
                throw new EBPException("未能找到配置文件! 搜索文本：" + configSearchText);
            }
            configs.Load(AssetDatabase.GUIDToAssetPath(guids[0]));

            assetTreeViewState = new TreeViewState();
            assetTreeHeaderState = AssetsTreeView.CreateDefaultHeaderState();
        }

        private void OnEnable()
        {
            var assetMCHS = AssetsTreeView.CreateDefaultHeaderState();

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(assetTreeHeaderState, assetMCHS))
                MultiColumnHeaderState.OverwriteSerializedFields(assetTreeHeaderState, assetMCHS);
            assetTreeHeaderState = assetMCHS;

            assetTree = new AssetsTreeView(assetTreeViewState, new MultiColumnHeader(assetTreeHeaderState), configs);
        }

        private void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Asset Directories:");
                    configs.Json.AssetsDirectories = EditorGUILayout.TextArea(configs.Json.AssetsDirectories);
                    GUILayout.Space(10);
                    GUILayout.Label("Exclude Extensions When BuildBundles:");
                    configs.Json.ExcludeExtensionsWhenBuildBundles = EditorGUILayout.TextArea(configs.Json.ExcludeExtensionsWhenBuildBundles);
                    GUILayout.Label("Exclude SubString When Find:");
                    configs.Json.ExcludeSubStringWhenFind = EditorGUILayout.TextArea(configs.Json.ExcludeSubStringWhenFind);
                }
                GUILayout.Space(10);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Dependence Root Directories:  (Use \"" + configs.Json.Separator + "\" Add Available Extension)", GUILayout.MinWidth(50));
                    configs.Json.DependenceFilterDirectories = EditorGUILayout.TextArea(configs.Json.DependenceFilterDirectories);
                    GUILayout.Space(10);
                    GUILayout.Label("OutPutPath");
                    GUILayout.BeginHorizontal();
                    configs.Json.OutputPath = EditorGUILayout.TextField(configs.Json.OutputPath);
                    if (GUILayout.Button("..", GUILayout.MaxWidth(20)))
                    {
                        string s = EditorUtility.OpenFolderPanel("Open Folder", configs.Json.OutputPath, null);
                        if (s != "") configs.Json.OutputPath = s;
                    }
                    GUILayout.EndHorizontal();
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Save") && EditorUtility.DisplayDialog("Save", "确定保存配置?", "确定", "取消"))
                        {
                            configs.Save();
                        }
                        if (GUILayout.Button("Build"))
                        {
                            if (!Directory.Exists(configs.Json.OutputPath))
                            {
                                EditorUtility.DisplayDialog("错误", "输出目录不存在：" + configs.Json.OutputPath, "确定");
                                return;
                            }
                            if (EditorUtility.DisplayDialog("Find No Reference Assets", "确定开始?", "确定", "取消"))
                            {
                                Run();
                            }
                        }
                    }
                }
            }

            //获取底边的垂直位置
            Rect lastRect = GUILayoutUtility.GetLastRect();
            float bottomY = lastRect.y + lastRect.height + 3;

            assetTree.OnGUI(new Rect(3, bottomY, position.width - 6, position.height - bottomY - 3));
        }

        void Run()
        {
            //Create AssetBundleList
            List<AssetBundleBuild> assetBundleList = new List<AssetBundleBuild>();
            foreach (var item in configs.Json.AssetsDirectories.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) //每一个给定的要创建Bundle的目录
            {
                var directory = "Assets/" + item;
                foreach (var file in FindFiles(directory, configs.Json.ExcludeExtensionsWhenBuildBundles)) //每一个资产文件
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
            var manifest = BuildPipeline.BuildAssetBundles(configs.Json.OutputPath, assetBundleList.ToArray(), BuildAssetBundleOptions.DryRunBuild, EditorUserBuildSettings.activeBuildTarget);
            EditorUtility.ClearProgressBar();
            if (manifest == null)
            {
                UnityEngine.Debug.LogError("Dry Build AssetBundles Failed!");
                return;
            }

            //创建用来标记是否被引用的字典
            string[] excludeSubStrList = configs.Json.ExcludeSubStringWhenFind.Replace('\\', '/').ToLower().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, bool> allBundles = new Dictionary<string, bool>();
            foreach (var item in manifest.GetAllAssetBundles()) //获取所有Bundle添加到字典
            {
                bool available = true;
                foreach (var except in excludeSubStrList)
                {
                    if (item.Contains(except))
                    {
                        available = false;
                    }
                }
                if (available)
                {
                    allBundles.Add(item, false);
                }
            }

            //标记是否被引用
            foreach (var item in configs.Json.DependenceFilterDirectories.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) //每一个给定要检查依赖的目录
            {
                if (string.IsNullOrEmpty(item)) continue;
                string[] s = item.Split(new[] { configs.Json.Separator }, StringSplitOptions.RemoveEmptyEntries);
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
                    if (s.Length < 2)
                    {
                        available = true;
                    }
                    if (available)
                    {
                        string filePath = Path.Combine(directory, file).Replace('\\', '/').ToLower();
                        allBundles[filePath] = true; //自身也标记
                        foreach (var dependence in manifest.GetAllDependencies(filePath)) //标记每一个依赖的文件
                        {
                            allBundles[dependence] = true;
                        }
                    }
                }
            }

            //显示无引用项
            using (var writer = new StreamWriter(configs.ResultFilePath))
            {
                foreach (var item in allBundles.Keys)
                {
                    if (allBundles[item] == false)
                    {
                        configs.NoReferenceAssetList.Add(item);
                        writer.WriteLine(item);
                    }
                }
            }

            assetTree.Reload();
            assetTree.Repaint();

            if(EditorUtility.DisplayDialog("Find No Reference Assets Finish", "完成！", "打开结果文件", "关闭"))
            {
                Process.Start(configs.ResultFilePath);
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
