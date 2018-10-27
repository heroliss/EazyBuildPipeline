using UnityEngine;
using UnityEditor;
using EazyBuildPipeline.Common.Configs;

namespace EazyBuildPipeline.BundleManager.Editor
{
    public class BundleManagerWindow : EditorWindow, ISerializationCallbackReceiver
    {
        Module module; //仅用于提供给Unity自动序列化
        Common.Configs.CommonConfig commonConfig; //仅用于提供给Unity自动序列化

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

            G.g.mainTab = mainTab = new AssetBundleManagement2.AssetBundleMainWindow();
        }
        private void OnEnable()
        {
            mainTab.OnEnable_extension();
            settingPanel.OnEnable();
        }

        private void OnDestroy()
        {
            mainTab.OnDestroy_extension();
            G.Clear();
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(6, 6, position.width - 12, settingPanelHeight), GUIContent.none, EditorStyles.helpBox);
		    settingPanel.OnGUI();
            GUILayout.EndArea();

			Rect mainTabRect = new Rect(6, settingPanelHeight + 6 + 3,
                position.width - 12, position.height - settingPanelHeight - 12 - 3);
            GUILayout.BeginArea(mainTabRect, GUIContent.none, EditorStyles.helpBox);
			mainTab.OnGUI_extension(mainTabRect);
            GUILayout.EndArea();
        }
        private void OnFocus()
        {
            mainTab.OnFocus_extension();
        }
        private void Update()
        {
            mainTab.Update_extension();
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
            G.Runner = new Runner(module);
            G.g = new G.GlobalReference();

            G.g.mainTab = mainTab; //TODO:对BundleMaster的特殊处理
        }
    }
}
