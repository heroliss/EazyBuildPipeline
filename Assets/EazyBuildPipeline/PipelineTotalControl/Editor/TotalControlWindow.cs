using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using EazyBuildPipeline.PlayerBuilder.Editor;

namespace EazyBuildPipeline.PipelineTotalControl.Editor
{
    public class TotalControlWindow : EditorWindow, ISerializationCallbackReceiver
    {
        Module module;
        readonly int settingPanelHeight = 160;
 
        private PlayerSettingsPanel playerSettingsPanel;
        private SettingPanel settingPanel;


        [MenuItem("Window/EazyBuildPipeline/TotalControl")]
        static void ShowWindow()
        {
            GetWindow<TotalControlWindow>();
        }
        private void Awake()
        {
            G.Init();
            PlayerBuilder.G.Init();

            settingPanel = new SettingPanel();
            playerSettingsPanel = new PlayerSettingsPanel();
            settingPanel.Awake();
            playerSettingsPanel.Awake();
        }

        private void OnEnable()
        {
            G.g.MainWindow = this;
            settingPanel.OnEnable();
            playerSettingsPanel.OnEnable();
        }
        private void Update()
        {
            settingPanel.Update();
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(6, 6, position.width - 12, settingPanelHeight), GUIContent.none, EditorStyles.helpBox);
            settingPanel.OnGUI();
            GUILayout.EndArea();

            Rect panelRect = new Rect(6, 6 + settingPanelHeight + 3, position.width - 12, position.height - settingPanelHeight - 3 - 12);
            GUILayout.BeginArea(panelRect, GUIContent.none, EditorStyles.helpBox);
            playerSettingsPanel.OnGUI();
            GUILayout.EndArea();
        }
        private void OnFocus()
        {
            settingPanel.OnFocus();
        }

        private void OnProjectChange()
        {
            settingPanel.OnProjectChange();
        }

        private void OnDestroy()
        {
            playerSettingsPanel.OnDestory();
            settingPanel.OnDestory();
            G.Clear();
            PlayerBuilder.G.Clear();
        }

        public void OnBeforeSerialize()
        {
            module = G.Module;
        }

        public void OnAfterDeserialize()
        {
            G.Module = module;
            G.g = new G.GlobalReference();
            PlayerBuilder.G.Module = G.Module.PlayerBuilderModule;
            PlayerBuilder.G.g = new PlayerBuilder.G.GlobalReference();
        }
    }
}
