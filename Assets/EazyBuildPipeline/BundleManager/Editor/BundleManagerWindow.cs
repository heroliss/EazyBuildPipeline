﻿using UnityEngine;
using UnityEditor;

namespace EazyBuildPipeline.BundleManager.Editor
{
    public class BundleManagerWindow : EditorWindow
    {
        private AssetBundleManagement2.AssetBundleMainWindow mainTab;
        private SettingPanel settingPanel;
        private float settingPanelHeight = 70;
        
        [MenuItem("Window/EazyBuildPipeline/BundleManager")]
        static void ShowWindow()
        {
            GetWindow<BundleManagerWindow>();
        }
        public BundleManagerWindow()
        {
            titleContent = new GUIContent("Bundle");
        }
        private void Awake()
        {
            G.Init();

            settingPanel = new SettingPanel();
            settingPanel.Awake();

            mainTab = new AssetBundleManagement2.AssetBundleMainWindow();
            G.g.mainTab = mainTab;
            mainTab.OnEnable_extension();
        }
            
        private void OnDestroy()
        {
            mainTab.OnDestroy_extension();
            G.Clear();
        }
        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(6, 6, position.width - 12, settingPanelHeight), GUIContent.none, EditorStyles.helpBox))
			{
				settingPanel.OnGUI();
			}
			Rect mainTabRect = new Rect(6, settingPanelHeight + 6 + 3,
                position.width - 12, position.height - settingPanelHeight - 12 - 3);
            using (new GUILayout.AreaScope(mainTabRect, GUIContent.none, EditorStyles.helpBox))
			{
				mainTab.OnGUI_extension(mainTabRect);
			}
        }
        private void OnFocus()
        {
            mainTab.OnFocus_extension();
        }
        private void Update()
        {
            mainTab.Update_extension();
        }
    }
}
