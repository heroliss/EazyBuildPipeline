using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public class PackageTree : TreeView
    {
        public List<PackageTreeItem> Packages = new List<PackageTreeItem>();
        readonly ColorPickerHDRConfig colorPickerHDRConfig = new ColorPickerHDRConfig(0, 1, 0, 1);
        const int packageIDStart = -2000000000;

        Texture2D compressionIcon;
        int packageID = packageIDStart;
        int folderID = 0;
        int bundleID = 0;
        int packageCount, folderCount, bundleCount;
        GUIStyle labelErrorStyle;
        GUIStyle inDropDownStyle;
        GUIStyle inToggleStyle;

        #region 列枚举
        enum ColumnEnum
        {
            Name, Connection, Necessery, DeploymentLocation, CopyToStreaming, FileName
        }
        #endregion


        #region TreeView构建
        public PackageTree(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader)
                : base(treeViewState, multiColumnHeader)
		{
			InitStyles();

			SetIcons();

			#region TreeView设置
			baseIndent = 0;
			cellMargin = 5;
			columnIndexForTreeFoldouts = 0;
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

			Reload();
            multiColumnHeader.ResizeToFit();
        }

		private void InitStyles()
		{
			labelErrorStyle = new GUIStyle(G.g.styles.LabelStyle);
			labelErrorStyle.normal.textColor = new Color(1, 0.3f, 0.3f);
			inDropDownStyle = G.g.styles.InDropDownStyle;
            inToggleStyle = G.g.styles.InToggleStyle;
		}

		public void ReConnectWithBundleTree()
        {
            foreach (PackageTreeItem package in Packages)
            {
                if (package.hasChildren)
                {
                    foreach (PackageTreeItem item in package.children)
                    {
                        RecursiveConnectWithBundleItem(item, G.g.bundleTree.bundleDic, G.g.bundleTree.folderDic);
                    }
                }
            }
            UpdateAllComplete();
        }

        private void UpdateAllComplete()
        {
            foreach (BundleTreeItem item in G.g.bundleTree.bundleDic.Values)
            {
                foreach (PackageTreeItem p in item.packageItems)
                {
                    p.complete = true;
                }
            }
            foreach (BundleTreeItem item in G.g.bundleTree.folderDic.Values)
            {
                if (item.hasChildren)
                {
                    foreach (PackageTreeItem p in item.packageItems)
                    {
                        p.complete = false;
                    }
                }
                else
                {
                    foreach (PackageTreeItem p in item.packageItems)
                    {
                        p.complete = true;
                    }
                }
            }
            foreach (BundleTreeItem item in G.g.bundleTree.bundleDic.Values)
            {
                foreach (PackageTreeItem p in item.packageItems)
                {
                    RecursiveUpdateParentComplete((PackageTreeItem)p.parent);
                }
            }
            foreach (BundleTreeItem item in G.g.bundleTree.folderDic.Values)
            {
                if (!item.hasChildren)
                {
                    foreach (PackageTreeItem p in item.packageItems)
                    {
                        RecursiveUpdateParentComplete((PackageTreeItem)p.parent);
                    }
                }
            }
        }

        private void RecursiveConnectWithBundleItem(PackageTreeItem packageItem, Dictionary<string, BundleTreeItem> bundleDic, Dictionary<string, BundleTreeItem> folderDic)
        {
            Dictionary<string, BundleTreeItem> dic = packageItem.bundleItem.isFolder ? folderDic : bundleDic;
            if (dic.ContainsKey(packageItem.bundleItem.relativePath))
            {
                packageItem.lost = false;
                packageItem.bundleItem = dic[packageItem.bundleItem.relativePath];
                packageItem.bundleItem.packageItems.Add(packageItem);
            }
            else
            {
                packageItem.lost = true;
                packageItem.bundleItem = new BundleTreeItem()
                {
                    relativePath = packageItem.bundleItem.relativePath,
                    displayName = packageItem.bundleItem.displayName,
                    isFolder = packageItem.bundleItem.isFolder,
                    icon = packageItem.bundleItem.icon,
                };
            }
            if (packageItem.hasChildren)
            {
                foreach (PackageTreeItem child in packageItem.children)
                {
                    RecursiveConnectWithBundleItem(child, bundleDic, folderDic);
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            Packages.Clear();
            PackageTreeItem root = new PackageTreeItem()
            {
                id = 0,
                depth = -1,
                displayName = "Root",
            };
            BuildTreeFromMap(root);
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        public void UpdateAllFileName()
        {
            foreach (var package in Packages)
            {
                 package.fileName = G.Runner.GetPackageFileName(package.displayName, G.Module.ModuleStateConfig.Json.ResourceVersion);
            }
        }

        private void BuildTreeFromMap(TreeViewItem root)
        {
            foreach (var package in G.Module.UserConfig.Json.Packages)
            {
                Color color = Color.black;
                ColorUtility.TryParseHtmlString("#" + package.Color, out color);
                var p = new PackageTreeItem()
                {
                    id = --packageID,
                    depth = 0,
                    displayName = package.PackageName,
                    packageColor = color,
                    isPackage = true,
                    icon = compressionIcon,
                    necessery = package.Necessery,
                    deploymentLocation = package.DeploymentLocation,
                    copyToStreaming = package.CopyToStreaming
                };
                p.package = p;
                BuildPackageTreeFromBundleTree(package, p);
                BuildPackageTree_lostItems(package, p);
                root.AddChild(p);
                Packages.Add(p);
            }
            UpdateAllFileName();
        }

        private void BuildPackageTreeFromBundleTree(Configs.UserConfig.JsonClass.Package package, PackageTreeItem p)
        {
            foreach (string bundlePath in package.Bundles)
            {
                if (G.g.bundleTree.bundleDic.ContainsKey(bundlePath))
                {
                    RecursiveAddParents(G.g.bundleTree.bundleDic[bundlePath], p);
                }
            }
            foreach (string emptyFolderPath in package.EmptyFolders)
            {
                if (G.g.bundleTree.folderDic.ContainsKey(emptyFolderPath))
                {
                    RecursiveAddParents(G.g.bundleTree.folderDic[emptyFolderPath], p);
                }
            }
        }

        private void BuildPackageTree_lostItems(Configs.UserConfig.JsonClass.Package package, PackageTreeItem p)
        {
            //遍历bundles数组添加所有丢失的bundle
            foreach (string bundlePath in package.Bundles)
            {
                string[] folderNames = bundlePath.Split('/'); //路径字符串中必须全部为/ 不能有\
                PackageTreeItem parent = p;
                string relativePath = "";
                int i;
                for (i = 0; i < folderNames.Length - 1; i++)
                {
                    relativePath += folderNames[i];
                    parent = FindOrCreateFolderItemByFolderName(parent, folderNames[i], relativePath);
                    relativePath += "/";
                }
                string bundleName = Path.GetFileNameWithoutExtension(folderNames[i]);
                relativePath += bundleName;
                FindOrCreateBundleItemByBundleName(parent, bundleName, relativePath);
            }
            //遍历所有空文件夹数组添加丢失的空文件夹
            foreach (string folderPath in package.EmptyFolders)
            {
                string[] folderNames = folderPath.Split('/');
                PackageTreeItem parent = p;
                string relativePath = "";
                int i;
                for (i = 0; i < folderNames.Length; i++)
                {
                    relativePath += folderNames[i];
                    parent = FindOrCreateFolderItemByFolderName(parent, folderNames[i], relativePath);
                    relativePath += "/";
                }
            }
        }

        private void FindOrCreateBundleItemByBundleName(PackageTreeItem parent, string bundleName, string relativePath)
        {
            if (parent.hasChildren)
            {
                foreach (PackageTreeItem item in parent.children)
                {
                    if (item.bundleItem.displayName == bundleName)
                    {
                        return;
                    }
                }
            }
            var newBundle = new PackageTreeItem()
            {
                lost = true,
                id = ++bundleID,
                package = parent.package,
                bundleItem = new BundleTreeItem()
                {
                    icon = G.g.bundleTree.bundleIcon,
                    displayName = bundleName,
                    relativePath = relativePath
                }
            };
            parent.AddChild(newBundle);
        }

        private PackageTreeItem FindOrCreateFolderItemByFolderName(PackageTreeItem parent, string folderName, string relativePath)
        {
            if (parent.hasChildren)
            {
                foreach (PackageTreeItem item in parent.children)
                {
                    if (item.bundleItem.isFolder && item.bundleItem.displayName == folderName)
                    {
                        return item;
                    }
                }
            }
            PackageTreeItem folderItem = new PackageTreeItem()
            {
                lost = true,
                id = --folderID,
                package = parent.package,
                bundleItem = new BundleTreeItem()
                {
                    displayName = folderName,
                    icon = G.g.bundleTree.folderIcon,
                    isFolder = true,
                    relativePath = relativePath
                }
            };
            parent.AddChild(folderItem);
            return folderItem;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            //SortIfNeeded(root, rows);
            return rows;
        }

        void SetIcons()
        {
            try
            {
                compressionIcon = CommonModule.GetIcon("PackageIcon.png");
            }
            catch(Exception e)
            {
                G.Module.DisplayDialog("加载Icon时发生错误：" + e.Message);
            }
        }
        #endregion


        #region GUI
        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (args.acceptedRename)
            {
                args.newName = args.newName.Trim();
                if (args.newName == "") args.newName = args.originalName;
                char[] invalidChars = Path.GetInvalidFileNameChars();
                int invalidCharsIndex = args.newName.IndexOfAny(invalidChars);
                if (invalidCharsIndex >= 0)
                {
                    G.Module.DisplayDialog("新包名：" + args.newName + " 包含非法字符！");
                    return;
                }
                string newName = args.newName;
                int i = 0;
                while (!CheckName(newName, args.itemID)) newName = args.newName + " " + ++i;
                if (i > 0)
                {
                    G.Module.DisplayDialog("包名：" + args.newName + " 已存在! 自动改为：\n" + newName);
                }
                PackageTreeItem item = FindItem(args.itemID, rootItem) as PackageTreeItem;
                item.displayName = newName;
                G.Module.IsDirty = true;
            }
            UpdateAllFileName();
        }

        private bool CheckName(string newName, int id)
        {
            foreach (PackageTreeItem item in rootItem.children)
            {
                if (item.isPackage && item.displayName == newName && id != item.id)
                {
                    return false;
                }
            }
            return true;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return ((PackageTreeItem)item).isPackage;
        }
        protected override bool CanBeParent(TreeViewItem item)
        {
            var packageItem = ((PackageTreeItem)item);
            return packageItem.isPackage || packageItem.bundleItem.isFolder;
        }
        protected override void AfterRowsGUI()
        {
            base.AfterRowsGUI();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (PackageTreeItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (ColumnEnum)args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect rect, PackageTreeItem item, ColumnEnum column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref rect);
            switch (column)
            {
                case ColumnEnum.Name:
                    //args.rowRect = rect;
                    float space = 5 + foldoutWidth + depthIndentWidth * item.depth;
                    GUI.DrawTexture(new Rect(rect.x + space, rect.y, rect.height, rect.height), item.isPackage ? item.icon : item.bundleItem.icon, ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(rect.x + space + rect.height, rect.y, rect.width - space - rect.height, rect.height),
                        item.isPackage ? item.displayName : item.bundleItem.displayName,
                        item.lost ? labelErrorStyle : (item.isPackage ? EditorStyles.label : item.bundleItem.style));
                    break;
                case ColumnEnum.Connection:
                    Color color = EditorGUI.ColorField(new Rect(rect.x/* + Packages.IndexOf(item.package) * (rect.height + 4)*/,
                            rect.y, rect.height, rect.height), GUIContent.none, item.package.packageColor, false, false, false, colorPickerHDRConfig);
                    if (color != item.package.packageColor)
                    {
                        item.package.packageColor = color;
                    }
                    break;
                case ColumnEnum.FileName:
                    if (item.isPackage)
                    {
                        GUI.Label(rect, item.fileName);
                    }
                    break;
                case ColumnEnum.Necessery:
                    if (item.isPackage && G.Module.UserConfig.Json.PackageMode == "Addon")
                    {
                        int index = G.NecesseryEnum.IndexOf(item.necessery);
                        int index_new = EditorGUI.Popup(rect, index, G.NecesseryEnum, inDropDownStyle);
                        if (index_new != index)
                        {
                            item.necessery = G.NecesseryEnum[index_new];
                            G.Module.IsDirty = true;
                        }
                    }
                    break;
                case ColumnEnum.DeploymentLocation:
                    if (item.isPackage && G.Module.UserConfig.Json.PackageMode == "Addon")
                    {
                        int index = G.DeploymentLocationEnum.IndexOf(item.deploymentLocation);
                        int index_new = EditorGUI.Popup(rect, index, G.DeploymentLocationEnum, inDropDownStyle);
                        if (index_new != index)
                        {
                            item.deploymentLocation = G.DeploymentLocationEnum[index_new];
                            G.Module.IsDirty = true;
                        }
                    }
                    break;
                case ColumnEnum.CopyToStreaming:
                    if (item.isPackage && G.Module.UserConfig.Json.PackageMode == "Addon")
                    {
                        Rect rect_new = new Rect(rect.x + rect.width / 2 - 8, rect.y, 16, rect.height);
                        bool selected = EditorGUI.Toggle(rect_new, item.copyToStreaming, inToggleStyle);
                        if (selected != item.copyToStreaming)
                        {
                            item.copyToStreaming = selected;
                            G.Module.IsDirty = true;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion


        #region 表头
        public static MultiColumnHeaderState CreateDefaultHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Name"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 300,
                    minWidth = 100,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByLabel"), "Package的颜色标签"),
                    contextMenuText = "Tag",
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 28,
                    minWidth = 28,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Necessery"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 90,
                    minWidth = 50,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Location"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 80,
                    minWidth = 50,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("CopyToStreaming"),
                    headerTextAlignment = TextAlignment.Center,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 120,
                    minWidth = 30,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("FileName"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Center,
                    width = 300,
                    minWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = true
                },
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(ColumnEnum)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }
        #endregion


        #region 点击
        protected override void DoubleClickedItem(int id)
        {
            PackageTreeItem item = FindItem(id, rootItem) as PackageTreeItem;

            if (item.isPackage || item.hasChildren)
            {
                SetExpanded(id, !IsExpanded(id));
            }
            else
            {
                Locate(item);
            }
        }

        private void Locate(PackageTreeItem item)
        {
            var ids = new int[] { item.bundleItem.id };
            G.g.bundleTree.SetSelection(ids);
            foreach (var id in ids)
            {
                G.g.bundleTree.FrameItem(id);
            }
            G.g.bundleTree.SetFocus();
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
            menu.AddItem(new GUIContent("New Package"), false, CreatePackage);
            menu.AddSeparator(null);
            menu.AddItem(new GUIContent("Expand All"), false, ExpandAll);
            menu.AddItem(new GUIContent("Collapse All"), false, CollapseAll);
        }

        protected override void ContextClickedItem(int id)
        {
            var item = (PackageTreeItem)FindItem(id, rootItem);
            GenericMenu menu = new GenericMenu();
            if (!item.isPackage)
            {
                menu.AddItem(new GUIContent("Locate"), false, () => { Locate(item); });
                menu.AddSeparator(null);
            }
            else
            {
                menu.AddItem(new GUIContent("Rename"), false, () => { BeginRename(item); });
                menu.AddSeparator(null);
            }
            menu.AddItem(new GUIContent("Delete"), false, () => { DeletePackageItem(GetSelection()); });
            menu.AddSeparator(null);
            if (item.hasChildren)
            {
                menu.AddItem(new GUIContent("Recursive Expand"), false, () => { SetExpandedRecursiveForAllSelection(true); });
                menu.AddItem(new GUIContent("Recursive Collapse"), false, () => { SetExpandedRecursiveForAllSelection(false); });
                menu.AddSeparator(null);
            }
            AddGlobalMenuItem(menu);
            menu.ShowAsContext();
            Event.current.Use();
        }

        private void SetExpandedRecursiveForAllSelection(bool b)
        {
            foreach (var i in GetSelection())
            {
                SetExpandedRecursive(i, b);
            };
        }

        private void DeletePackageItem(IList<int> list)
        {
            G.Module.IsDirty = true;
            foreach (var id in list)
            {
                PackageTreeItem item = (PackageTreeItem)FindItem(id, rootItem);
                if (item != null)
                {
                    PackageTreeItem parent = (PackageTreeItem)item.parent;
                    RecursiveDeleteItem(item);
                    if (!item.lost)
                    {
                        RecursiveUpdateParentComplete(parent);
                    }
                }
            }
            BuildRows(rootItem);
        }

        private void RecursiveDeleteItem(PackageTreeItem packageItem)
        {
            while (packageItem.hasChildren)
            {
                RecursiveDeleteItem((PackageTreeItem)packageItem.children[0]);
            }
            if (packageItem.bundleItem != null)
            {
                packageItem.bundleItem.packageItems.Remove(packageItem);
            }
            if (packageItem.isPackage)
            {
                packageCount--;
                Packages.Remove(packageItem); //仅用于处理单独存放包的packages
            }
            else if (packageItem.bundleItem.isFolder)
            {
                folderCount--;
            }
            else
            {
                bundleCount--;
            }
            packageItem.parent.children.Remove(packageItem);
        }

        private void CreatePackage()
        {
            if (string.IsNullOrEmpty(G.Module.ModuleStateConfig.Json.CurrentUserConfigName))
            {
                G.Module.DisplayDialog("请先选择配置或创建一个空配置");
                return;
            }
            G.Module.IsDirty = true;
            PackageTreeItem package = new PackageTreeItem()
            {
                id = --packageID,
                depth = 0,
                icon = compressionIcon,
                displayName = "New Package",
                isPackage = true,
                packageColor = UnityEngine.Random.ColorHSV(0, 1, 0.5f, 1f, 1, 1)
            };
            package.package = package; //这个packageItem所属的package是自己
            rootItem.AddChild(package);
            packageCount++;
            BeginRename(package);
            BuildRows(rootItem);
            FrameItem(package.id);
            Packages.Add(package);
        }
        #endregion


        #region 拖拽
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            //拖到空白处
            if (args.dragAndDropPosition == DragAndDropPosition.OutsideItems)
            {
                return DragAndDropVisualMode.Rejected;
            }
            //拖到package上
            var packageItem = (PackageTreeItem)args.parentItem;
            var bundleItems = (List<TreeViewItem>)DragAndDrop.GetGenericData("BundleTreeItemList");
            //拖到文件夹或文件上
            if (!packageItem.isPackage)
            {
                //拖到RootItem上或文件上
                if (packageItem == rootItem || !packageItem.bundleItem.isFolder)
                {
                    return DragAndDropVisualMode.Rejected;
                }
                foreach (BundleTreeItem item in bundleItems)
                {
                    //拖到其他文件夹上
                    if (item.parent != packageItem.bundleItem)
                    {
                        return DragAndDropVisualMode.Rejected;
                    }
                }
                //拖到同一个父文件夹
                while (!packageItem.isPackage)
                {
                    packageItem = (PackageTreeItem)packageItem.parent;
                }
            }
            //检查完成,释放鼠标开始移动条目
            if (args.performDrop)
            {
                AddBundlesToPackage(packageItem, bundleItems);
            }

            return DragAndDropVisualMode.Copy;
        }

        public void AddBundlesToPackage(PackageTreeItem packageItem, List<TreeViewItem> bundleItems)
        {
            G.Module.IsDirty = true;
            foreach (BundleTreeItem item in bundleItems)
            {
                AddBundlesToPackage(packageItem, item);
            }

            SetupDepthsFromParentsAndChildren(packageItem);
            BuildRows(rootItem);
        }

        private void AddBundlesToPackage(PackageTreeItem packageItem, BundleTreeItem item)
        {
            var p = RecursiveAddParents(item, packageItem);
            if (item.isFolder && item.hasChildren)
            {
                RecursiveAddChildren(item, p, packageItem);
            }
        }

        void RecursiveAddChildren(BundleTreeItem bundleItemParent, PackageTreeItem packageParent, PackageTreeItem packageItem)
        {
            foreach (BundleTreeItem item in bundleItemParent.children)
            {
                var p = FindOrCreatePackageItem(item, packageParent, packageItem);
                if (item.isFolder && item.hasChildren)
                {
                    RecursiveAddChildren(item, p, packageItem);
                }
            }
        }

        /// <summary>
        /// 递归添加父节点
        /// </summary>
        /// <param name="bundleItem">要添加的bundle</param>
        /// <param name="packageItem">在此package里添加</param>
        /// <returns></returns>
        PackageTreeItem RecursiveAddParents(BundleTreeItem bundleItem, PackageTreeItem packageItem)
        {
            if (bundleItem.parent.depth < 0)
            {
                return FindOrCreatePackageItem(bundleItem, packageItem, packageItem);//在packageItem下找到或创建一个以bundleItem为内容的packageItem
            }
            else
            {
                var parent = RecursiveAddParents((BundleTreeItem)bundleItem.parent, packageItem);
                return FindOrCreatePackageItem(bundleItem, parent, packageItem);
            }
        }

        /// <summary>
        /// 从任意一个packageItem节点的第一层children中找到包含bundleItem内容的packageItem，若没有则创建一个
        /// </summary>
        /// <param name="bundleItem">要找的内容</param>
        /// <param name="parent">在该parent节点的第一层子节点中查找</param>
        /// <param name="packageItem">要添加到bundleItem中的包信息</param>
        /// <returns></returns>
        private PackageTreeItem FindOrCreatePackageItem(BundleTreeItem bundleItem, PackageTreeItem parent, PackageTreeItem packageItem)
        {
            PackageTreeItem child = null;
            if (parent.hasChildren)
            {
                foreach (PackageTreeItem c in parent.children)
                {
                    if (c.bundleItem == bundleItem)
                    {
                        child = c;
                        break;
                    }
                }
            }
            if (child == null)
            {
                child = new PackageTreeItem()
                {
                    bundleItem = bundleItem,
                    id = bundleItem.isFolder ? --folderID : ++bundleID,
                    package = packageItem,
                    complete = bundleItem.hasChildren ? false : true
                };
                if (!child.bundleItem.packageItems.Exists(x => x.package == packageItem))
                {
                    child.bundleItem.packageItems.Add(child);
                }
                if (child.bundleItem.isFolder)
                    folderCount++;
                else
                    bundleCount++;

                parent.AddChild(child);

                RecursiveUpdateParentComplete(parent);
            }
            return child;
        }

        private void RecursiveUpdateParentComplete(PackageTreeItem parent)
        {
            if (parent.isPackage || parent == rootItem)
            {
                return;
            }
            if (!parent.hasChildren && !parent.bundleItem.hasChildren)
            {
                parent.complete = true;
                return;
            }
            int count = 0;
            foreach (PackageTreeItem item in parent.children)
            {
                if (!item.lost) count++;
            }
            if (count == parent.bundleItem.children.Count)
            {
                parent.complete = true;
                foreach (PackageTreeItem item in parent.children)
                {
                    if (!item.lost && item.complete == false)
                    {
                        parent.complete = false;
                        break;
                    }
                }
            }
            else
            {
                parent.complete = false;
            }
            RecursiveUpdateParentComplete((PackageTreeItem)parent.parent);
        }
        #endregion

        public void FrameAndSelectItems(List<PackageTreeItem> items)
        {
            int selectItemsCount = Mathf.Min(items.Count, 1000);
            var ids = new int[selectItemsCount];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = items[i].id;
                FrameItem(ids[i]);
            }
            SetSelection(ids);
            SetFocus();
        }
    }
}
