using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public class BundleTree : TreeView
	{
        public struct VersionsStruct { public int ResourceVersion; }
        public VersionsStruct Versions;
        public AssetBundleBuild[] BundleBuildMap;
        public Texture2D folderIcon, bundleIcon, bundleIcon_Scene;
        public class BundleTreeItemDictionary : SerializableDictionary<string, BundleTreeItem> { }
		public BundleTreeItemDictionary bundleDic, folderDic; //key为相对路径，必须首尾无斜杠
        public bool LoadBundlesFromConfig = true;
        public string BundleConfigPath;

        int loadFileProgressCount;
		List<BundleTreeItem> checkFailedItems;
		int directoryLastID;
        int fileLastID;
        GUIStyle labelWarningStyle;
        GUIStyle labelBundleStyle;

        #region 列枚举
        enum ColumnEnum
		{
			Name, Connection
		}
		#endregion


		#region TreeView构建
		void SetIcons()
		{
            try
            {
                folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
                bundleIcon = CommonModule.GetIcon("BundleIcon.png");
                bundleIcon_Scene = CommonModule.GetIcon("BundleIcon_Scene.png");
            }
            catch (Exception e)
            {
                G.Module.DisplayDialog("加载Icon时发生错误：" + e.Message);
            }
        }

		public BundleTree(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader,
            bool loadBundlesFromConfig, string bundleConfigPath)
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

			bundleDic = new BundleTreeItemDictionary();
			folderDic = new BundleTreeItemDictionary();
			checkFailedItems = new List<BundleTreeItem>();

            LoadBundlesFromConfig = loadBundlesFromConfig;
            BundleConfigPath = bundleConfigPath;

			Reload();
            multiColumnHeader.ResizeToFit();
        }

        public void ClearAllConnection()
		{
			foreach (var item in bundleDic.Values)
			{
				item.packageItems.Clear();
			}
			foreach (var item in folderDic.Values)
			{
				item.packageItems.Clear();
			}
		}

		private void InitStyles()
		{
            labelWarningStyle = new GUIStyle(G.g.styles.LabelStyle);
            labelBundleStyle = new GUIStyle(G.g.styles.LabelStyle);
            labelWarningStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0f) : new Color(0.6f, 0.5f, 0f);
			labelBundleStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.8f, 0.7f) : new Color(0.0f, 0.4f, 0.0f);
        }

		protected override TreeViewItem BuildRoot()
		{
			loadFileProgressCount = 0;
			checkFailedItems.Clear();
			bundleDic.Clear();
			folderDic.Clear();

            if (LoadBundlesFromConfig)
            {
                return BuildBundleTreeFromConfig();
            }
            else
            {
                return BuildBundleTreeFromFolder();
            }
		}

        private TreeViewItem BuildBundleTreeFromConfig()
        {
            if (string.IsNullOrEmpty(BundleConfigPath))
            {
                BundleTreeItem root = new BundleTreeItem()
                {
                    id = 0,
                    depth = -1,
                    displayName = "Root",
                    isFolder = false,
                };
                return root;
            }

            BundleTreeItem rootFolderItem = new BundleTreeItem()
            {
                id = 0,
                depth = -1,
                displayName = "Root",
                isFolder = true,
                relativePath = "", //这里是相对路径的根
                icon = folderIcon
            };
            AssetBundleBuild[] bundles = JsonConvert.DeserializeObject<AssetBundleBuild[]>(File.ReadAllText(BundleConfigPath));
            foreach (var bundleItem in bundles)
            {
                BundleTreeItem currentFolderItem = rootFolderItem;
                BundleTreeItem folderItem;
                string[] folders = bundleItem.assetBundleName.Split('/');
                string bundleName = folders[folders.Length - 1];
                for (int i = 0; i < folders.Length - 1; i++)
                {
                    string folderName = folders[i];
                    folderItem = null;
                    if (currentFolderItem.hasChildren)
                    {
                        folderItem = (BundleTreeItem)currentFolderItem.children.Find(x => x.displayName == folderName); //x.id <= 0 说明该项为文件夹
                    }
                    if (folderItem == null)
                    {
                        folderItem = new BundleTreeItem()
                        {
                            id = --directoryLastID,
                            displayName = folderName,
                            isFolder = true,
                            relativePath = currentFolderItem.relativePath == "" ? folderName : currentFolderItem.relativePath + "/" + folderName,
                            icon = folderIcon
                        };
                        currentFolderItem.AddChild(folderItem);
                        folderDic.Add(folderItem.relativePath, folderItem);
                    }
                    currentFolderItem = folderItem;
                }
                var fileItem = new BundleTreeItem()
                {
                    isFolder = false,
                    verify = true,
                    path = null,
                    relativePath = bundleItem.assetBundleName,
                    bundlePath = null,
                    displayName = bundleName,
                    icon = bundleIcon,
                    id = ++fileLastID,
                    size = -1,
                    style = labelBundleStyle
                };
                currentFolderItem.AddChild(fileItem);
                bundleDic.Add(fileItem.relativePath, fileItem);
            }
            //加入虚拟的assetbundlemanifest
            var assetbundlemanifest = new BundleTreeItem()
            {
                isFolder = false,
                verify = true,
                path = null,
                relativePath = "assetbundlemanifest",
                bundlePath = null,
                displayName = "assetbundlemanifest",
                icon = bundleIcon,
                id = ++fileLastID,
                size = -1,
                style = labelBundleStyle
            };
            rootFolderItem.AddChild(assetbundlemanifest);
            bundleDic.Add(assetbundlemanifest.relativePath, assetbundlemanifest);

            SetupDepthsFromParentsAndChildren(rootFolderItem);
            return rootFolderItem;
        }

        private TreeViewItem BuildBundleTreeFromFolder()
        {
            string assetBundlesFolderPath = G.Module.ModuleConfig.BundleWorkFolderPath;
            if (!Directory.Exists(assetBundlesFolderPath))
            {
                G.Module.DisplayDialog("AssetBundles目录不存在：" + assetBundlesFolderPath);
                BundleTreeItem root = new BundleTreeItem()
                {
                    id = 0,
                    depth = -1,
                    displayName = "Root",
                    isFolder = false,
                };
                return root;
            }

            string rootPath = G.Module.GetBundleFolderPath();

            BundleTreeItem rootFolderItem = new BundleTreeItem()
            {
                id = 0,
                depth = -1,
                displayName = Path.GetFileName(rootPath),
                isFolder = true,
                path = rootPath,
                relativePath = "", //这里是相对路径的根
                icon = folderIcon
            };
            if (Directory.Exists(rootFolderItem.path))
            {
                AddDirectories(rootFolderItem);
                AddFiles(rootFolderItem);
                EditorUtility.ClearProgressBar();
            }

            SetupDepthsFromParentsAndChildren(rootFolderItem);

            //检查
            if (checkFailedItems.Count > 0)
            {
                G.Module.DisplayDialog("有 " + checkFailedItems.Count +
                    " 个manifest文件缺少对应的bundle文件！\n（这些项已标记为警告色:黄色）");
            }
            LoadBundleInfo(); //加载信息文件
            return rootFolderItem;
        }

		protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
		{
			var rows = base.BuildRows(root);

			//SortIfNeeded(root, rows);
			return rows;
		}
        #endregion


        #region GUI
        protected override bool CanBeParent(TreeViewItem item)
		{
			return ((BundleTreeItem)item).isFolder;
		}
		protected override void AfterRowsGUI()
		{
			base.AfterRowsGUI();
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			var item = (BundleTreeItem)args.item;

			for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
			{
				CellGUI(args.GetCellRect(i), item, (ColumnEnum)args.GetColumn(i), ref args);
			}
		}

		private void CellGUI(Rect rect, BundleTreeItem item, ColumnEnum column, ref RowGUIArgs args)
		{
			CenterRectUsingSingleLineHeight(ref rect);
			switch (column)
			{
				case ColumnEnum.Name:
					float space = 5 + foldoutWidth + depthIndentWidth * item.depth;
					GUI.DrawTexture(new Rect(rect.x + space, rect.y, rect.height, rect.height), item.icon, ScaleMode.ScaleToFit);
					GUI.Label(new Rect(rect.x + space + rect.height, rect.y, rect.width - space - rect.height, rect.height),
						item.displayName, item.style);
					break;
				case ColumnEnum.Connection:
					foreach (var p in item.packageItems)
					{
						float width = rect.height;
						float x = rect.x;
                        Rect r = new Rect(x + G.g.packageTree.Packages.IndexOf(p.package) * (rect.height + 4), rect.y, width, rect.height);
                        if (GUI.Button(r, GUIContent.none, p.complete ? p.package.colorBlockStyle : p.package.colorBlockStyle_hollow)) LocatePackage(p);
					}
					break;
				default:
					break;
			}
		}

		private void LocatePackage(PackageTreeItem item)
		{
			var ids = new int[] { item.id };
			G.g.packageTree.SetSelection(ids);
			foreach (var id in ids)
			{
				G.g.packageTree.FrameItem(id);
			}
			G.g.packageTree.SetFocus();
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
					contextMenuText = "Tags",
					headerTextAlignment = TextAlignment.Center,
					sortedAscending = true,
					sortingArrowAlignment = TextAlignment.Center,
					width = 100,
					minWidth = 28,
					autoResize = false,
					allowToggleVisibility = true
				},
			};

			Assert.AreEqual(columns.Length, Enum.GetValues(typeof(ColumnEnum)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

			var state = new MultiColumnHeaderState(columns);
			return state;
		}
        #endregion


        #region 数据处理
        private void LoadBundleInfo()
        {
            try
            {
                Versions = new VersionsStruct() { ResourceVersion = -1 };
                //BundleBuildMap = null;
                string versionPath = Path.Combine(G.Module.GetBundleInfoFolderPath(), "Versions.json");
                //string buildMapPath = Path.Combine(G.Module.GetBundleInfoFolderPath(), "BuildMap.json");
                Versions = JsonConvert.DeserializeObject<VersionsStruct>(File.ReadAllText(versionPath));
                //BundleBuildMap = JsonConvert.DeserializeObject<AssetBundleBuild[]>(File.ReadAllText(buildMapPath));
            }
            catch (Exception e)
            {
                G.Module.DisplayDialog("加载BundleInfo时发生错误：" + e.Message);
            }
        }
        void AddDirectories(BundleTreeItem parent)
		{
			string[] directories = Directory.GetDirectories(parent.path);
			foreach (string path in directories)
			{
				var folderItem = new BundleTreeItem()
				{
					id = --directoryLastID,
					displayName = Path.GetFileName(path),
					isFolder = true,
					path = path,
					relativePath = path.Remove(0, G.Module.GetBundleFolderPathStrCount()).Replace('\\', '/'),
					icon = folderIcon
				};
				parent.AddChild(folderItem);
				AddDirectories(folderItem);
				AddFiles(folderItem);
				folderDic.Add(folderItem.relativePath, folderItem);
			}
		}
		double lastTime;
		void AddFiles(BundleTreeItem folderItem)
		{
			string[] files = Directory.GetFiles(folderItem.path);
			foreach (string filePath in files)
			{
                if (EditorApplication.timeSinceStartup - lastTime > 0.06f)
				{
					G.Module.DisplayProgressBar(string.Format("PackageManager(检查：{1}，载入总数：{0})",
					    loadFileProgressCount,G.Module.ModuleConfig.Json.CheckBundle), filePath,
					    (float)loadFileProgressCount % 100000 / 100000);
                    lastTime = EditorApplication.timeSinceStartup;
				}
				loadFileProgressCount++;

				//Texture2D cachedIcon = AssetDatabase.GetCachedIcon(filePath) as Texture2D; //TODO：如何找到类似函数
				if (filePath.EndsWith(".manifest", StringComparison.Ordinal))
				{
					string bundlePath = filePath.Remove(filePath.Length - 9, 9);
					string bundleName = Path.GetFileName(bundlePath);
					var fileItem = new BundleTreeItem()
					{
						isFolder = false,
						verify = true,
						path = filePath, //manifest文件路径
						relativePath = bundlePath.Remove(0, G.Module.GetBundleFolderPathStrCount()).Replace('\\', '/'),
						bundlePath = bundlePath, //去掉manifest后缀的完整路径
						displayName = bundleName,
						icon = bundleIcon,
						id = ++fileLastID,
						size = -1,
						style = labelBundleStyle
					};
					folderItem.AddChild(fileItem);
					bundleDic.Add(fileItem.relativePath, fileItem);
					//检查manifest对应文件是否存在
					if (G.Module.ModuleConfig.Json.CheckBundle)
					{
						fileItem.verify = File.Exists(bundlePath);
						if (fileItem.verify == false)
						{
							checkFailedItems.Add(fileItem);
							fileItem.style = labelWarningStyle;
						}
					}
					//----------------------------
				}
			}
		}

		public static string GetFileSize(long size)
		{
			var num = 1024.00; //byte

			if (size < num)
				return size + "B";
			if (size < Math.Pow(num, 2))
				return (size / num).ToString("f2") + "K"; //kb
			if (size < Math.Pow(num, 3))
				return (size / Math.Pow(num, 2)).ToString("f2") + "M"; //M
			if (size < Math.Pow(num, 4))
				return (size / Math.Pow(num, 3)).ToString("f2") + "G"; //G

			return (size / Math.Pow(num, 4)).ToString("f2") + "T"; //T
		}
		#endregion


		#region 拖放
		protected override bool CanStartDrag(CanStartDragArgs args)
		{
			return true;
		}
		protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
		{
			if (args.draggedItemIDs.Count > 1000)
			{
                G.Module.DisplayDialog("拖动的项太多了！请尽量折叠文件夹后拖拽");
				return;
			}
			DragAndDrop.PrepareStartDrag();
			var items = new List<TreeViewItem>(args.draggedItemIDs.Count);
			foreach (var id in args.draggedItemIDs)
			{
				var item = FindItem(id, rootItem);
				items.Add(item);
			}
			DragAndDrop.SetGenericData("BundleTreeItemList", items);

			#region 添加在Unity编辑器中全局可用的path属性
			//string[] paths = new string[args.draggedItemIDs.Count];
			//for (int i = 0; i < paths.Length; i++)
			//{
			//  paths[i] = item.path;
			//}
			//DragAndDrop.paths = paths;
			#endregion
			DragAndDrop.StartDrag("Bundle Drag");
		}

		#endregion


		#region 点击
		protected override void DoubleClickedItem(int id)
		{
			BundleTreeItem item = FindItem(id, rootItem) as BundleTreeItem;

			if (item.hasChildren)
			{
				SetExpanded(id, !IsExpanded(id));
			}
			else
			{
				Locate(item);
			}
		}

		private void Locate(BundleTreeItem item)
		{
			List<int> ids = new List<int>();
			foreach (var packageItem in item.packageItems)
			{
				ids.Add(packageItem.id);
			}

			G.g.packageTree.SetSelection(ids);
			foreach (var id in ids)
			{
				G.g.packageTree.FrameItem(id);
			}
			G.g.packageTree.SetFocus();
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
			menu.AddItem(new GUIContent("Expand All"), false, () => { ExpandAll(); });
			menu.AddItem(new GUIContent("Collapse All"), false, () => { CollapseAll(); });
		}

		protected override void ContextClickedItem(int id)
		{
			var item = (BundleTreeItem)FindItem(id, rootItem);
			GenericMenu menu = new GenericMenu();
            if (item.packageItems.Count > 0)
			{
				menu.AddItem(new GUIContent("Locate"), false, () => { Locate(item); });
				menu.AddSeparator(null);
			}
			if (G.g.packageTree.Packages.Count > 0)
			{
				foreach (var package in G.g.packageTree.Packages)
				{
					menu.AddItem(new GUIContent("Add To/" + package.displayName), false, () =>
					  {
						  List<TreeViewItem> bundles = new List<TreeViewItem>();
                          foreach (var i in GetSelection())
                          {
                              var bundle = FindItem(i, rootItem);
                              bundles.Add(bundle);
                          }
                          G.g.packageTree.AddBundlesToPackage(package, bundles);
					  });
				}
				menu.AddSeparator(null);
			}
            if (!LoadBundlesFromConfig)
            {
                menu.AddItem(new GUIContent("Reveal In Finder"), false, () => { EditorUtility.RevealInFinder(item.path); });
                menu.AddSeparator(null);
            }
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
			}
		}
        #endregion

        public void FrameAndSelectItems(List<BundleTreeItem> items)
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
