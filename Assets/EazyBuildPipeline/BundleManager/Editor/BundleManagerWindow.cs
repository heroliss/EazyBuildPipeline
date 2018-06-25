﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace LiXuFeng.BundleManager.Editor
{
    public class BundleManagerWindow : EditorWindow
    {
        private AssetBundleManagement2.AssetBundleMainWindow mainTab;
        private SettingPanel settingPanel;
        private float settingPanelHeight = 70;
        
        [MenuItem("Window/BuildPipeline/BundleManager")]
        static void ShowWindow()
        {
            GetWindow<BundleManagerWindow>();
        }
        public BundleManagerWindow()
        {
            titleContent = new GUIContent("Bundle");
        }
        private void OnEnable()
        {
            Configs.Init();

            settingPanel = new SettingPanel();
            settingPanel.OnEnable();

            mainTab = new AssetBundleManagement2.AssetBundleMainWindow();
            Configs.g.mainTab = mainTab;
            mainTab.OnEnable_extension();
        }
        private void OnDisable()
        {
            settingPanel.OnDisable();
            Configs.Clear();
        }
        private void OnDestroy()
        {
            mainTab.OnDestroy_extension();
            mainTab.Close();
        }
        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(6, 6, position.width - 12, settingPanelHeight), new GUIContent(), EditorStyles.helpBox))
            {
                settingPanel.OnGUI();
            }
            Rect mainTabRect = new Rect(6, settingPanelHeight + 6 + 3,
                position.width - 12, position.height - settingPanelHeight - 12 - 3);
            using (new GUILayout.AreaScope(mainTabRect, new GUIContent(), EditorStyles.helpBox))
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
