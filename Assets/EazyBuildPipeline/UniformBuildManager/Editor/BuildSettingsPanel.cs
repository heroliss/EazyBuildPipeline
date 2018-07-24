using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    [Serializable]
    public class BuildSettingsPanel
    {
        [SerializeField] bool creatingNewConfig;
        [SerializeField] string[] buildSettingNames;
        [SerializeField] int selectedBuildSettingIndex;
        [SerializeField] GUIStyle dropdownStyle;
        [SerializeField] GUIStyle buttonStyle;
        [SerializeField] GUIStyle labelStyle;

        const int defaultHeight = 22;
        GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.MaxHeight(defaultHeight), GUILayout.MaxWidth(70) };
        GUILayoutOption[] dropdownOptions = new GUILayoutOption[] { GUILayout.MaxHeight(defaultHeight), GUILayout.MaxWidth(80) };
        GUILayoutOption[] popupOptions = new GUILayoutOption[] { GUILayout.MaxHeight(defaultHeight), GUILayout.MaxWidth(200) };

        private void InitStyles()
        {
            dropdownStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            labelStyle = new GUIStyle(EditorStyles.label) { fixedWidth = 0, fixedHeight = 0, alignment = TextAnchor.MiddleLeft };
        }

        public void Awake()
        {
            InitStyles();
            try
            {
                LoadAllConfigs();
            }
            catch (Exception e)
            {
                G.configs.DisplayDialog("加载配置文件时发生错误：" + e.Message);
            }
        }
        public void OnEnable()
        {
        }

        private void LoadAllConfigs()
        {
            //G.configs.LoadAllConfigs();
            InitSelectedIndex();
            LoadBuildSettings();
            ConfigToIndex();
        }

        private void LoadBuildSettings()
        {
            G.configs.LoadCurrentBuildSetting();
            buildSettingNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.configs.LocalConfig.Local_BuildSettingsFolderPath);
        }

        private void InitSelectedIndex()
        {
            selectedBuildSettingIndex = -1;
        }

        public void OnDestory()
        {

        }

        public void OnGUI()
        {
            if (creatingNewConfig == true && GUI.GetNameOfFocusedControl() != "InputField1")
            {
                creatingNewConfig = false;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("aaa"))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(BuildTarget.iOS), "aaa;bbb,ccc,ddd eeefff");
                    //string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(BuildTarget.iOS));
                    //Debug.Log(defines);
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("New", "新建配置文件"), buttonStyle, buttonOptions))
                { ClickedNew(); }
                if (GUILayout.Button(new GUIContent("Save", "保存配置文件"), buttonStyle, buttonOptions))
                { ClickedSave(); return; }
                if (GUILayout.Button(new GUIContent("Revert", "保存配置文件"), buttonStyle, buttonOptions))
                { ClickedRevert(); return; }
                if (creatingNewConfig)
                {
                    ShowInputField();
                }
                else
                {
                    ShowBuildSettingDropdown();
                }
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("ViewToolOrbit"), "查看该文件"), buttonStyle, GUILayout.Width(25)))
                { ClickedShowConfigFile(); return; }
            }
        }
        private void ShowInputField()
        {
            GUI.SetNextControlName("InputField1");
            string tip = "<输入名称>(回车确定，空串取消)";
            string s = EditorGUILayout.DelayedTextField(tip, dropdownStyle, popupOptions);
            GUI.FocusControl("InputField1");
            s = s.Trim().Replace('\\', '/');
            if (s != tip)
            {
                if (s != "")
                {
                    try
                    {
                        string path = Path.Combine(G.configs.LocalConfig.Local_BuildSettingsFolderPath, s + ".json");
                        if (File.Exists(path))
                            EditorUtility.DisplayDialog("创建失败", "创建新文件失败，该名称已存在！", "确定");
                        else
                        {
                            CreateNewBuildSetting(s, path);
                        }
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("创建失败", "创建时发生错误：" + e.Message, "确定");
                    }
                }
                creatingNewConfig = false;
            }
        }
        private void CreateNewBuildSetting(string name, string path)
        {
            //新建
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Create(path).Close();
            EditorUtility.DisplayDialog("创建成功", "创建成功!", "确定");
            //更新列表
            buildSettingNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.configs.LocalConfig.Local_BuildSettingsFolderPath);
            //保存
            G.configs.BuildSettingConfig.JsonPath = path;
            SaveCurrentBuildSetting();
            //切换
            ChangeBuildSetting(buildSettingNames.IndexOf(name));
            //用于总控
            G.g.OnChangeConfigList();
        }

        private void ChangeBuildSetting(int selectedBuildSettingIndex_new)
        {
            bool ensureLoad = true;
            if (G.configs.BuildSettingConfig.Dirty)
            {
                ensureLoad = EditorUtility.DisplayDialog("切换配置", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
            }
            if (ensureLoad)
            {
                try
                {
                    var newBuildSettingConfig = new Configs.BuildSettingConfig();
                    string newBuildSettingConfigName = buildSettingNames[selectedBuildSettingIndex_new] + ".json";
                    newBuildSettingConfig.JsonPath = Path.Combine(G.configs.LocalConfig.Local_BuildSettingsFolderPath, newBuildSettingConfigName);
                    newBuildSettingConfig.Load();
                    //至此加载成功
                    G.configs.CurrentConfig.Json.CurrentBuildSettingName = newBuildSettingConfigName;
                    G.configs.BuildSettingConfig = newBuildSettingConfig;
                    selectedBuildSettingIndex = selectedBuildSettingIndex_new;
                    ConfigToIndex();
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("切换map", "切换Map配置时发生错误：" + e.Message, "确定");
                }
            }
        }

        private void ConfigToIndex()
        {
            if (G.configs.CurrentConfig.Json.CurrentBuildSettingName == null)
            {
                selectedBuildSettingIndex = -1;
            }
            else
            {
                selectedBuildSettingIndex = buildSettingNames.IndexOf(G.configs.CurrentConfig.Json.CurrentBuildSettingName.RemoveExtension());
            }
        }

        private void ClickedNew()
        {
            creatingNewConfig = true;
        }

        private void ShowBuildSettingDropdown()
        {
            if (G.configs.BuildSettingConfig.Dirty)
            {
                try
                {
                    buildSettingNames[selectedBuildSettingIndex] += "*";
                }
                catch { }
            }
            int selectedBuildSetting_new = EditorGUILayout.Popup(selectedBuildSettingIndex, buildSettingNames, dropdownStyle, popupOptions);
            if (G.configs.BuildSettingConfig.Dirty)
            {
                try
                {
                    buildSettingNames[selectedBuildSettingIndex] = buildSettingNames[selectedBuildSettingIndex].Remove(buildSettingNames[selectedBuildSettingIndex].Length - 1);
                }
                catch { }
            }
            if (selectedBuildSetting_new != selectedBuildSettingIndex)
            {
                ChangeBuildSetting(selectedBuildSetting_new);
            }
        }
        private void ClickedRevert()
        {
            ChangeBuildSetting(selectedBuildSettingIndex);
        }

        private void ClickedShowConfigFile()
        {
            string path = "";
            try
            {
                path = Path.Combine(G.configs.LocalConfig.Local_BuildSettingsFolderPath, buildSettingNames[selectedBuildSettingIndex] + ".json");
            }
            catch { }
            if (!File.Exists(path))
            {
                path = G.configs.LocalConfig.Local_BuildSettingsFolderPath;
            }
            EditorUtility.RevealInFinder(path);
        }
        private void ClickedSave()
        {
            bool ensure = true;
            if (G.configs.BuildSettingConfig.Dirty)
            {
                ensure = EditorUtility.DisplayDialog("保存", "是否保存并覆盖原配置：" + buildSettingNames[selectedBuildSettingIndex], "覆盖保存", "取消");
            }
            if (!ensure) return;

            SaveCurrentBuildSetting();
        }

        private void SaveCurrentBuildSetting()
        {
            try
            {
                G.configs.BuildSettingConfig.Save();

                EditorUtility.DisplayDialog("保存", "保存配置成功！", "确定");
                G.configs.BuildSettingConfig.Dirty = false;
            }

            catch (Exception e)
            {
                EditorUtility.DisplayDialog("保存", "保存配置时发生错误：\n" + e.Message, "确定");
            }
            //总控暂时用不上
            //G.g.OnChangeCurrentConfig();
        }
    }
}
