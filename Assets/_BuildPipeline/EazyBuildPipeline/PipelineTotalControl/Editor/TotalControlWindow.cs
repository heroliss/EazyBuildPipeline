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
        Module module; //仅用于提供给Unity自动序列化
        Common.Configs.CommonConfig commonConfig; //仅用于提供给Unity自动序列化

        readonly int settingPanelHeight = 160;
        PlayerSettingsPanel playerSettingsPanel;
        SettingPanel settingPanel;


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
            commonConfig = CommonModule.CommonConfig;
        }

        public void OnAfterDeserialize()
        {
            CommonModule.CommonConfig = commonConfig;
            G.Module = module;
            //G.Runner = new Runner(module); //暂时没有Runner
            G.g = new G.GlobalReference();

            //重新初始化所有Runner
            G.Module.InitRunners();

            //把实例关联到PlayerBuilder的G中
            PlayerBuilder.G.Module = G.Module.PlayerBuilderModule;
            PlayerBuilder.G.Runner = G.Module.PlayerBuilderRunner;
            PlayerBuilder.G.g = new PlayerBuilder.G.GlobalReference();
        }
    }
}