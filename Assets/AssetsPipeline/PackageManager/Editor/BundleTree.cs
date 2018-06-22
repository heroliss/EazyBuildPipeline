using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace LiXuFeng.PackageManager.Editor
{
	public class BundleTree : TreeView
	{
		public Texture2D folderIcon, bundleIcon, bundleIcon_Scene;
		int loadFileProgressCount;
		List<BundleTreeItem> checkFailedItems;
		public Dictionary<string, BundleTreeItem> bundleDic, folderDic; //key为相对路径，必须首尾无斜杠
		private int directoryLastID;
		private int fileLastID;
		GUIStyle labelWarningStyle;
		GUIStyle labelBundleStyle;
		GUIStyle colorBlockStyle;

        public struct BundleVersionsStruct { public int ResourceVersion; public int BundleVersion; }
        public BundleVersionsStruct BundleVersions;
        public AssetBundleBuild[] BundleBuildMap;

        #region 列枚举
        enum ColumnEnum
		{
			Name, Connection
		}
		#endregion


		#region TreeView构建
		void SetIcons()
		{
			folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
			string[] icons = AssetDatabase.FindAssets("BundleIcon");
			foreach (string i in icons)
			{
				string name = AssetDatabase.GUIDToAssetPath(i);
				if (name.Contains("BundleIcon.png"))
					bundleIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(name, typeof(Texture2D));
				if (name.Contains("BundleIcon_Scene.png"))
					bundleIcon_Scene = (Texture2D)AssetDatabase.LoadAssetAtPath(name, typeof(Texture2D));
			}
		}

		public BundleTree(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader)
				: base(treeViewState, multiColumnHeader)
		{
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

			InitStyles();

			bundleDic = new Dictionary<string, BundleTreeItem>();
			folderDic = new Dictionary<string, BundleTreeItem>();
			checkFailedItems = new List<BundleTreeItem>();
			Reload();
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
			labelWarningStyle = new GUIStyle(EditorStyles.label);
			labelWarningStyle.normal.textColor = new Color(0.8f, 0.8f, 0f);
			labelBundleStyle = new GUIStyle(EditorStyles.label);
			labelBundleStyle.normal.textColor = new Color(0.3f, 0.8f, 0.7f);
			colorBlockStyle = new GUIStyle("Button") { normal = new GUIStyleState() { background = new Texture2D(1, 1, TextureFormat.RGB24, false, false) }, alignment = TextAnchor.MiddleCenter };
		}

		protected override TreeViewItem BuildRoot()
		{
			loadFileProgressCount = 0;
			checkFailedItems.Clear();
			bundleDic.Clear();
			folderDic.Clear();

			string assetBundlesFolderPath = Configs.configs.LocalConfig.BundleRootPath;
			if (!Directory.Exists(assetBundlesFolderPath))
			{
				EditorUtility.DisplayDialog("错误", "AssetBundles目录不存在：" + assetBundlesFolderPath, "确定");
                BundleTreeItem root = new BundleTreeItem()
                {
                    id = 0,
                    depth = -1,
                    displayName = "Root",
                    isFolder = false,
                };
                return root;
			}

            string rootPath = Configs.configs.BundlePath;

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
				EditorUtility.DisplayDialog("提示", "有 " + checkFailedItems.Count +
					" 个manifest文件缺少对应的bundle文件！\n（这些项已标记为警告色:黄色）", "确定");
			}
            LoadBundleInfo();
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
					Color defaultColor = GUI.color;
					foreach (var p in item.packageItems)
					{
						GUI.color = p.package.packageColor;
						//colorBlockStyle.normal.background.SetPixel(1, 1, p.package.packageColor);
						float width = rect.height;
						float x = rect.x;
						if (!p.complete) { width /= 2; x += width / 2; }
						if (GUI.Button(new Rect(x + Configs.g.packageTree.Packages.IndexOf(p.package) * (rect.height + 4),
							rect.y, width, rect.height), new GUIContent(), colorBlockStyle)) LocatePackage(p);
					}
					GUI.color = defaultColor;
					break;
				default:
					break;
			}
		}

		private void LocatePackage(PackageTreeItem item)
		{
			var ids = new int[] { item.id };
			Configs.g.packageTree.SetSelection(ids);
			foreach (var id in ids)
			{
				Configs.g.packageTree.FrameItem(id);
			}
			Configs.g.packageTree.SetFocus();
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
                BundleVersions = new BundleVersionsStruct() { BundleVersion = -1, ResourceVersion = -1 };
                BundleBuildMap = null;
                string versionPath = Path.Combine(Configs.configs.BundleInfoPath, "Versions.json");
                string buildMapPath = Path.Combine(Configs.configs.BundleInfoPath, "BuildMap.json");
                BundleVersions = JsonConvert.DeserializeObject<BundleVersionsStruct>(File.ReadAllText(versionPath));
                BundleBuildMap = JsonConvert.DeserializeObject<AssetBundleBuild[]>(File.ReadAllText(buildMapPath));
            }
            catch //(Exception e)
            {
                //EditorUtility.DisplayDialog("错误", "在" + Configs.configs.BundleInfoPath + "中加载信息时发生错误：" + e.Message, "确定");
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
					relativePath = path.Remove(0, Configs.configs.PathHandCount).Replace('\\', '/'),
					icon = folderIcon
				};
				parent.AddChild(folderItem);
				AddDirectories(folderItem);
				AddFiles(folderItem);
				folderDic.Add(folderItem.relativePath, folderItem);
			}
		}
		float lastTime;
		void AddFiles(BundleTreeItem folderItem)
		{
			string[] files = Directory.GetFiles(folderItem.path);
			foreach (string filePath in files)
			{
				if (Time.realtimeSinceStartup - lastTime > 0.06f)
				{
					EditorUtility.DisplayProgressBar(string.Format("PackageManager(检查：{1}，载入总数：{0})",
					    loadFileProgressCount,Configs.configs.LocalConfig.CheckBundle), filePath,
					    (float)loadFileProgressCount % 100000 / 100000);
					lastTime = Time.realtimeSinceStartup;
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
						path = bundlePath,
						relativePath = bundlePath.Remove(0, Configs.configs.PathHandCount).Replace('\\', '/'),
						bundlePath = bundlePath,
						displayName = bundleName,
						icon = bundleIcon,
						id = ++fileLastID,
						size = -1,
						style = labelBundleStyle
					};
					folderItem.AddChild(fileItem);
					bundleDic.Add(fileItem.relativePath, fileItem);
					//检查manifest对应文件是否存在
					if (Configs.configs.LocalConfig.CheckBundle)
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
				EditorUtility.DisplayDialog("提示", "拖动的项太多了！请尽量折叠文件夹后拖拽", "确定");
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

			Configs.g.packageTree.SetSelection(ids);
			foreach (var id in ids)
			{
				Configs.g.packageTree.FrameItem(id);
			}
			Configs.g.packageTree.SetFocus();
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
			menu.AddItem(new GUIContent("展开所有项"), false, () => { ExpandAll(); });
			menu.AddItem(new GUIContent("折叠所有项"), false, () => { CollapseAll(); });
		}

		protected override void ContextClickedItem(int id)
		{
			var item = (BundleTreeItem)FindItem(id, rootItem);
			GenericMenu menu = new GenericMenu();
			if (Configs.g.packageTree.Packages.Count > 0)
			{
				foreach (var package in Configs.g.packageTree.Packages)
				{
					menu.AddItem(new GUIContent("添加到/" + package.displayName), false, () =>
					  {
						  List<TreeViewItem> bundles = new List<TreeViewItem>();
                          foreach (var i in GetSelection())
                          {
                              var bundle = FindItem(i, rootItem);
                              bundles.Add(bundle);
                          }
                          Configs.g.packageTree.AddBundlesToPackage(package, bundles);
					  });
				}
				menu.AddSeparator(null);
			}
			if (item.packageItems.Count > 0)
			{
				menu.AddItem(new GUIContent("定位"), false, () => { Locate(item); });
				menu.AddSeparator(null);
			}
            menu.AddItem(new GUIContent("Reveal In Finder"), false, () => { EditorUtility.RevealInFinder(item.path + ".manifest"); });
            menu.AddSeparator(null);
            if (item.hasChildren)
			{
				menu.AddItem(new GUIContent("全部展开"), false, () => { SetExpandedRecursiveForAllSelection(true); });
				menu.AddItem(new GUIContent("全部折叠"), false, () => { SetExpandedRecursiveForAllSelection(false); });
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
	}
}
