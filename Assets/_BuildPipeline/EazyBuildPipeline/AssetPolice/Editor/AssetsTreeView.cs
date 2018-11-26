using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace EazyBuildPipeline.AssetPolice.Editor
{
    class AssetsTreeView : TreeView
    {
        public ModuleConfig moduleConfig;
        public StateConfig stateConfig;
        int id = 0;
        Texture2D emptyTexture;
        #region 列枚举
        enum ColumnEnum
        {
            Check, Path
        }
        #endregion

        public AssetsTreeView(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader, ModuleConfig moduleConfig, StateConfig stateConfig) : base(treeViewState, multiColumnHeader)
        {
            this.moduleConfig = moduleConfig;
            this.stateConfig = stateConfig;

            #region TreeView设置
            baseIndent = 0;
            cellMargin = 2;
            columnIndexForTreeFoldouts = 1;
            //depthIndentWidth (get only)
            extraSpaceBeforeIconAndLabel = 0;
            //foldoutWidth (get only)
            //hasSearch (get only)
            //isDragging (get only)
            //isInitialized (get only)
            //base.multiColumnHeader
            //rootItem (get only)
            rowHeight = 20;
            customFoldoutYOffset = (rowHeight - EditorGUIUtility.singleLineHeight) * 0.5f;
            //searchString
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            //showingHorizontalScrollBar (get only)
            //showingVerticalScrollBar (get only)
            //state (get only)
            //totalHeight (get only)
            //treeViewControlID
            //treeViewRect
            #endregion

            #region MultiColumnHeader设置
            base.multiColumnHeader.canSort = false;
            //base.multiColumnHeader.height = 25;
            base.multiColumnHeader.sortedColumnIndex = -1;
            //base.multiColumnHeader.sortingChanged += OnSortingChanged;
            #endregion

            emptyTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                anisoLevel = 1,
                mipMapBias = 0,
            };
            emptyTexture.SetPixel(0, 0, new Color(0, 0, 0, 0));
            emptyTexture.Apply();

            multiColumnHeader.ResizeToFit();
            Reload();
        }
        protected override bool CanRename(TreeViewItem item)
        {
            return true;
        }
        protected override void RenameEnded(RenameEndedArgs args)
        {
        }
        protected override TreeViewItem BuildRoot()
        {
            id = 0;
            var root = new TreeViewItem()
            {
                id = -1,
                depth = -1,
                displayName = "Root",
            };
            root.children = new List<TreeViewItem>();

            string[] excludeSubStrList = moduleConfig.Json.ExcludeSubStringWhenFind.Replace('\\', '/').ToLower().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var bundle in moduleConfig.AllBundles)
            {
                if (!bundle.Value.IsRDRoot && bundle.Value.RDBundles.Count == 0)
                {
                    bool available = true;
                    foreach (var except in excludeSubStrList)
                    {
                        if (bundle.Key.Contains(except))
                        {
                            available = false;
                            break;
                        }
                    }
                    if (available)
                    {
                        root.AddChild(new AssetTreeItem(id++, 0, bundle.Key));
                    }
                }
            }
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (AssetTreeItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (ColumnEnum)args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect rect, AssetTreeItem item, ColumnEnum column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref rect);
            switch (column)
            {
                case ColumnEnum.Check:
                    bool b = GUI.Toggle(rect, item.Check, GUIContent.none);
                    if (item.Check != b)
                    {
                        if (IsSelected(item.id))
                        {
                            foreach (AssetTreeItem selectedItem in FindRows(GetSelection()))
                            {
                                selectedItem.Check = b;
                            }
                        }
                        else
                        {
                            item.Check = b;
                        }
                    }
                    break;
                case ColumnEnum.Path:
                    float space = 0;
                    GUI.DrawTexture(new Rect(rect.x + space, rect.y, rect.height, rect.height),
                        AssetDatabase.GetCachedIcon(item.displayName) as Texture2D ?? emptyTexture, ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(rect.x + space + rect.height, rect.y, rect.width - space - rect.height, rect.height), item.displayName);
                    break;
                default:
                    break;
            }
        }

        #region 点击
        protected override void DoubleClickedItem(int id)
        {
            AssetTreeItem item = FindItem(id, rootItem) as AssetTreeItem;
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item.displayName);
        }

        protected override void ContextClicked()
        {
            GenericMenu menu = new GenericMenu();

            AddGlobalMenuItem(menu);

            menu.ShowAsContext();
            Event.current.Use();
        }

        private void AddGlobalMenuItem(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Export Checked Items"), false, () =>
            {
                string path = EditorUtility.SaveFilePanel("Export", moduleConfig.Json.OutputPath, null, "txt");
                if (!string.IsNullOrEmpty(path))
                {
                    int exportCount = 0;
                    using (var writer = new StreamWriter(path))
                    {
                        for (int i = 0; i < rootItem.children.Count; i++)
                        {
                            var item = rootItem.children[i];
                            if (((AssetTreeItem)item).Check == true)
                            {
                                writer.WriteLine(item.displayName);
                                exportCount++;
                            }
                        }
                    }
                    EditorUtility.DisplayDialog("导出成功", "已将" + exportCount + "项导出到文件" + path, "确定");
                }
            });

            menu.AddSeparator(null);
            menu.AddItem(new GUIContent("Delete Checked Files"), false, () =>
            {
                int checkedCount = 0;
                foreach (AssetTreeItem item in rootItem.children)
                {
                    if (item.Check == true)
                    {
                        checkedCount++;
                    }
                }
                if (EditorUtility.DisplayDialog("删除文件", "你确定要删除所有勾选✔的文件？ (共" + checkedCount + "项)", "删除所有勾选的文件", "取消"))
                {
                    int deleteCount = 0;
                    for (int i = 0; i < rootItem.children.Count; i++)
                    {
                        var item = rootItem.children[i];
                        if (((AssetTreeItem)item).Check == true)
                        {
                            moduleConfig.AllBundles.Remove(item.displayName);
                            AssetDatabase.DeleteAsset(item.displayName);
                            deleteCount++;
                            if (EditorUtility.DisplayCancelableProgressBar("Delete Assets", item.displayName, (float)deleteCount / checkedCount))
                            {
                                EditorUtility.ClearProgressBar();
                                EditorUtility.DisplayDialog("删除被中止", "已删除" + deleteCount + "个文件", "确定");
                                Reload();
                                return;
                            }
                        }
                    }
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("删除成功", "已删除" + deleteCount + "个文件", "确定");
                    Reload();
                }
            });
            menu.AddSeparator(null);
            menu.AddItem(new GUIContent("Open Map File"), false, () =>
            {
                string path = EditorUtility.OpenFilePanel("Open", moduleConfig.Json.OutputPath, "json");
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return;
                }
                moduleConfig.AllBundles = JsonConvert.DeserializeObject<BundleRDDictionary>(File.ReadAllText(path));
                stateConfig.Json.CurrentMapFilePath = path;
                stateConfig.Save();
                Reload();
            });
        }
        #endregion

        #region 表头
        public static MultiColumnHeaderState CreateDefaultHeaderState()
        {
            var columns = new[]
            {
               new MultiColumnHeaderState.Column
                {
                    //headerContent = new GUIContent("Check"),
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                    width = 30,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    headerTextAlignment = TextAlignment.Left,
                    //sortedAscending = true,
                    //sortingArrowAlignment = TextAlignment.Center,
                    width = 500,
                    minWidth = 50,
                    autoResize = true,
                    allowToggleVisibility = false
                },
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(ColumnEnum)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }
        #endregion
    }

    public class AssetTreeItem : TreeViewItem
    {
        public bool Check;

        public AssetTreeItem() : base()
        {
        }

        public AssetTreeItem(int id, int depth, string displayName) : base(id, depth, displayName)
        {
        }
    }
}
