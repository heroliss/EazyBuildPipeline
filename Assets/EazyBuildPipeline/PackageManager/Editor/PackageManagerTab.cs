using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public class PackageManagerTab
    {
        #region 成员变量
        private SettingPanel settingPanel = new SettingPanel();
        private BundleTree bundleTree;
        private PackageTree packageTree;
        private EditorWindow m_editorWindow;
        private Rect m_TabRect;
        private Rect m_splitterRect;
        private Rect upFixedRect;
        private Rect downFixedRect;
        private Rect leftRect;
        private Rect rightRect;
        [SerializeField]
        private float m_splitterPercent = 0.4f;
        private float k_SplitterWidth = 3;
        private bool m_ResizingSplitter;
        private float k_fixedPanelHight = 130;
        private float k_fixedSpace = 3;
        private MultiColumnHeaderState bundleTreeHeaderState;
        private TreeViewState bundleTreeState;
        private MultiColumnHeader bundleTreeHeader;
        private MultiColumnHeaderState packageTreeHeaderState;
        private MultiColumnHeader packageTreeHeader;
        private TreeViewState packageTreeState;
        #endregion

        #region 初始化TreeView
        void InitTreeViewIfNeeded()
        {
            if (bundleTree == null)
            {
                if (bundleTreeHeaderState == null)
                    bundleTreeHeaderState = BundleTree.CreateDefaultHeaderState(leftRect.width);
                InitStateAndHeader(leftRect.width, bundleTreeHeaderState, ref bundleTreeHeader, ref bundleTreeState);
                bundleTree = new BundleTree(bundleTreeState, bundleTreeHeader);
                Configs.g.bundleTree = bundleTree;
            }
            if (packageTree == null)
            {
                if (packageTreeHeaderState == null)
                    packageTreeHeaderState = PackageTree.CreateDefaultHeaderState(rightRect.width);
                InitStateAndHeader(rightRect.width, packageTreeHeaderState, ref packageTreeHeader, ref packageTreeState);
                packageTree = new PackageTree(packageTreeState, packageTreeHeader);
                Configs.g.packageTree = packageTree;
            }
        }

        private void InitStateAndHeader(float treeViewWidth, MultiColumnHeaderState headerState, ref MultiColumnHeader header, ref TreeViewState treeViewState)
        {
            if (header == null)
                header = new MultiColumnHeader(headerState);
            if (treeViewState == null)
                treeViewState = new TreeViewState();
        }
        #endregion

        public PackageManagerTab()
        {
        }
        internal void OnDisable()
        {
            settingPanel.OnDisable();
            Configs.Clear();
        }
        internal void OnEnable(Rect rect, EditorWindow editorWindow)
        {
            Configs.Init();
            m_editorWindow = editorWindow;
            m_TabRect = rect;
            ComputeRects();
            m_splitterRect = new Rect(
                (int)(downFixedRect.x + downFixedRect.width * m_splitterPercent - k_SplitterWidth / 2), downFixedRect.y,
                k_SplitterWidth, downFixedRect.height);

            settingPanel.OnEnable();
            InitTreeViewIfNeeded();
        }

        internal void OnGUI(Rect tabRect)
        {
            #region 处理区域
            m_TabRect = tabRect;
            HandleHorizontalResize();
            ComputeRects();

            if (m_ResizingSplitter)
                m_editorWindow.Repaint();
            else //TODO：窗口Resize时触发
            {
                m_splitterRect.x = (int)(downFixedRect.x + downFixedRect.width * m_splitterPercent - k_SplitterWidth / 2);
                m_splitterRect.height = downFixedRect.height;
            }
            #endregion
            
			settingPanel.OnGUI(upFixedRect);         
            bundleTree.OnGUI(leftRect);
            packageTree.OnGUI(rightRect);
        }
        private void ComputeRects()
        {
            upFixedRect = new Rect(
                m_TabRect.x,
                m_TabRect.y,
                m_TabRect.width,
                k_fixedPanelHight
                );
            downFixedRect = new Rect(
                m_TabRect.x,
                upFixedRect.y + upFixedRect.height + k_fixedSpace,
                m_TabRect.width,
                m_TabRect.height - upFixedRect.height - k_fixedSpace
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
    }
}
