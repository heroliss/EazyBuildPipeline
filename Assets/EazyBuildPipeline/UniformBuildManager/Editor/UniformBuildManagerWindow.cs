using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    public class UniformBuildManagerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        Configs.Configs configs;
        readonly int settingPanelHeight = 150;
 
        private BuildSettingsPanel buildSettingsPanel;
        private SettingPanel settingPanel;


        [MenuItem("Window/EazyBuildPipeline/UniformBuildManager")]
        static void ShowWindow()
        {
            GetWindow<UniformBuildManagerWindow>();
        }
        private void Awake()
        {
            G.Init();

            settingPanel = new SettingPanel();
            buildSettingsPanel = new BuildSettingsPanel();
            settingPanel.Awake();
            buildSettingsPanel.Awake();
        }

        private void OnEnable()
        {
            settingPanel.OnEnable();
            buildSettingsPanel.OnEnable();
        }

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(6, 6, position.width - 12, settingPanelHeight), GUIContent.none, EditorStyles.helpBox))
            {
                settingPanel.OnGUI();
            }
            Rect panelRect = new Rect(6, 6 + settingPanelHeight + 3, position.width - 12, position.height - settingPanelHeight - 3 - 12);
            using (new GUILayout.AreaScope(panelRect, GUIContent.none, EditorStyles.helpBox))
            {
                buildSettingsPanel.OnGUI();
            }
        }
        private void OnFocus()
        {
            settingPanel.OnFocus();
        }
        private void OnDestroy()
        {
            buildSettingsPanel.OnDestory();
            settingPanel.OnDestory();
            G.Clear();
        }

        public void OnBeforeSerialize()
        {
            configs = G.configs;
        }

        public void OnAfterDeserialize()
        {
            G.configs = configs;
            G.g = new G.GlobalReference();
        }
    }
}
