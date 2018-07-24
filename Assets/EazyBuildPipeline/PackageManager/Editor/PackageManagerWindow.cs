using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public class PackageManagerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        #region 成员变量
        private Styles styles;
        private Configs.Configs configs;
        private SettingPanel settingPanel;
        private Rect m_splitterRect;
        private Rect upFixedRect;
        private Rect downFixedRect;
        private Rect leftRect;
        private Rect rightRect;
        private float m_splitterPercent = 0.4f;
        private float k_SplitterWidth = 3;
        private bool m_ResizingSplitter;
        private float k_fixedPanelHight = 130;
        private float k_fixedSpace = 3;
        TreeViewState bundleTreeViewState;
        TreeViewState packageTreeViewState;
        MultiColumnHeaderState bundleTreeHeaderState;
        MultiColumnHeaderState packageTreeHeaderState;
        #endregion

        [MenuItem("Window/EazyBuildPipeline/PackageManager")]
        static void ShowWindow()
        {
            GetWindow<PackageManagerWindow>();
        }
        public PackageManagerWindow()
        {
            titleContent = new GUIContent("Package");
        }

        private void OnDestroy()
        {
            settingPanel.OnDestory();
            G.Clear();
        }
        private void Awake()
        {
            //初始化分割条的Rect
            ComputeRects();
            m_splitterRect = new Rect(
                (int)(downFixedRect.x + downFixedRect.width * m_splitterPercent - k_SplitterWidth / 2), downFixedRect.y,
                k_SplitterWidth, downFixedRect.height);
            //------------------

            G.Init();

            settingPanel = new SettingPanel();
            settingPanel.Awake();

            bundleTreeViewState = new TreeViewState();
            bundleTreeHeaderState = BundleTree.CreateDefaultHeaderState(leftRect.width);

            packageTreeViewState = new TreeViewState();
            packageTreeHeaderState = PackageTree.CreateDefaultHeaderState(rightRect.width);
        }
        private void OnEnable()
        {
            MultiColumnHeaderState bundleMCHS = BundleTree.CreateDefaultHeaderState(leftRect.width);
            MultiColumnHeaderState packageMCHS = PackageTree.CreateDefaultHeaderState(rightRect.width);

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(bundleTreeHeaderState, bundleMCHS))
                MultiColumnHeaderState.OverwriteSerializedFields(bundleTreeHeaderState, bundleMCHS);
            bundleTreeHeaderState = bundleMCHS;

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(packageTreeHeaderState, packageMCHS))
                MultiColumnHeaderState.OverwriteSerializedFields(packageTreeHeaderState, packageMCHS);
            packageTreeHeaderState = packageMCHS;

            G.g.bundleTree = new BundleTree(bundleTreeViewState, new MultiColumnHeader(bundleTreeHeaderState));
            G.g.packageTree = new PackageTree(packageTreeViewState, new MultiColumnHeader(packageTreeHeaderState));
        }

        private void OnGUI()
        {
            #region 处理区域
            HandleHorizontalResize();
            ComputeRects();

            if (m_ResizingSplitter)
                Repaint();
            else //TODO：窗口Resize时触发
            {
                m_splitterRect.x = (int)(downFixedRect.x + downFixedRect.width * m_splitterPercent - k_SplitterWidth / 2);
                m_splitterRect.height = downFixedRect.height;
            }
            #endregion

            settingPanel.OnGUI(upFixedRect);
            G.g.bundleTree.OnGUI(leftRect);
            G.g.packageTree.OnGUI(rightRect);
        }
        private void ComputeRects()
        {
            upFixedRect = new Rect(
                6,
                6,
                position.width - 12,
                k_fixedPanelHight
                );
            downFixedRect = new Rect(
                6,
                upFixedRect.y + upFixedRect.height + k_fixedSpace,
                position.width - 12,
                position.height - 12 - upFixedRect.height - k_fixedSpace
                );
            leftRect = new Rect(
                downFixedRect.x,
                downFixedRect.y,
                downFixedRect.width * m_splitterPercent - k_SplitterWidth / 2,
                downFixedRect.height);
            rightRect = new Rect(
                m_splitterRect.x + m_splitterRect.width,
                downFixedRect.y,
                downFixedRect.width - m_splitterRect.width - leftRect.width,
                downFixedRect.height);
        }

        private void HandleHorizontalResize()
        {
            EditorGUIUtility.AddCursorRect(m_splitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && m_splitterRect.Contains(Event.current.mousePosition))
                m_ResizingSplitter = true;

            if (m_ResizingSplitter)
            {
                m_splitterPercent = Mathf.Clamp((Event.current.mousePosition.x - downFixedRect.x) / downFixedRect.width, 0.15f, 0.85f);
                m_splitterRect.x = (int)(downFixedRect.x + downFixedRect.width * m_splitterPercent - k_SplitterWidth / 2);
            }

            if (Event.current.type == EventType.MouseUp)
            {
                m_ResizingSplitter = false;
            }
        }

        public void OnBeforeSerialize()
        {
            configs = G.configs;
            configs.PackageMapConfig.Json.Packages = settingPanel.GetPackageMap();
            styles = G.g.styles;
        }

        public void OnAfterDeserialize()
        {
            G.configs = configs;
            G.g = new G.GlobalReference();
            G.g.styles = styles;
        }
    }
}
