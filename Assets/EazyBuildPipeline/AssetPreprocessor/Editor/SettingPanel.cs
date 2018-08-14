using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.AssetPreprocessor.Editor
{
    [Serializable]
    public class SettingPanel
    {
        [SerializeField] bool creatingNewConfig;
        [SerializeField] string[] savedConfigNames;
        [SerializeField] int selectedSavedConfigIndex = -1;
        [SerializeField] GUIStyle buttonStyle;
        [SerializeField] GUIStyle popupStyle;
        GUILayoutOption[] buttonOptions = { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
        GUILayoutOption[] popupOptions = { GUILayout.MaxHeight(25), GUILayout.MaxWidth(200) };
        public void Awake()
        {
            InitStyles();

            savedConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.configs.LocalConfig.Local_SavedConfigsFolderPath);
            string extension = Path.GetExtension(G.configs.CurrentConfig.Json.CurrentSavedConfigName);
            if (G.configs.CurrentConfig.Json.CurrentSavedConfigName != null)
            {
                selectedSavedConfigIndex = savedConfigNames.IndexOf(G.configs.CurrentConfig.Json.CurrentSavedConfigName.Remove(
                    G.configs.CurrentConfig.Json.CurrentSavedConfigName.Length - extension.Length, extension.Length));
            }
            HandleApplyingWarning();
        }

        private void InitStyles()
        {
            buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
            popupStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
        }

        public void OnEnable()
        {
            G.g.OnChangeCurrentConfig += () => { G.configs.Dirty = false; };
        }
        public void OnDisable()
        {

        }
		public void OnGUI()
		{
            if (creatingNewConfig == true && GUI.GetNameOfFocusedControl() != "InputField1")
            {
                creatingNewConfig = false;
            }
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Root:", GUILayout.Width(45));
            string path = EditorGUILayout.DelayedTextField(G.configs.LocalConfig.Json.RootPath);
            if (GUILayout.Button("...", GUILayout.MaxWidth(24)))
            {
                path = EditorUtility.OpenFolderPanel("打开根目录", G.configs.LocalConfig.Json.RootPath, null);
            }
            if (!string.IsNullOrEmpty(path) && path != G.configs.LocalConfig.Json.RootPath)
            {
                ChangeRootPath(path);
                return;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(10);
            if (GUILayout.Button(new GUIContent("New", "新建配置文件"), buttonStyle, buttonOptions))
            { ClickedNew(); return; }
            if (GUILayout.Button(new GUIContent("Save", "保存配置文件"), buttonStyle, buttonOptions))
            { ClickedSave(); return; }

            if (creatingNewConfig)
            {
                ShowInputField();
            }
            else
            {
                if (ShowDropdown()) return;
            }

            if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("ViewToolOrbit"), "查看该文件"), GUILayout.Height(25)))
            {
                ClickedShowConfigFile();
                return;
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Sync Directory", "同步Assets目录结构"), buttonStyle, GUILayout.MaxHeight(25), GUILayout.MaxWidth(120)))
            { ClickedSyncDirectory(); return; }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent("Revert"), buttonStyle, buttonOptions))
            { ClickedRevert(); return; }
            if (GUILayout.Button(new GUIContent("Apply"), buttonStyle, buttonOptions))
            { ClickedApply(); return; }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void ClickedApply()
		{
			bool ensure = EditorUtility.DisplayDialog("Preprocessor", "确定应用当前配置？应用过程不可中断。", "确定", "取消");
			if (ensure)
            {
                try
                {
                    G.configs.Runner.Run();
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog("Preprocessor", "应用配置时发生错误：" + e.ToString(), "确定");
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }

				string s = "转换完成！\n";
                if (G.configs.Runner.process.ExitCode != 0)
                {
					s = string.Format("操作中断！执行第{0}步时出错：{1}\n", G.configs.Runner.currentShellIndex + 1, G.configs.Runner.errorMessage);
                }
				s += string.Format("\n第一步(拷贝文件):\nPreStoredAssets中符合标签的文件共有{0}个，跳过{1}个，成功拷贝{2}个。\n",
                    G.configs.Runner.totalCountList[0], G.configs.Runner.skipCountList[0], G.configs.Runner.successCountList[0]);
                if (G.configs.Runner.currentShellIndex >= 1)
				{
					s += string.Format("\n第二步(修改meta):\n共有{0}个meta文件，跳过{1}个，成功修改{2}个。\n",
                        G.configs.Runner.totalCountList[1], G.configs.Runner.skipCountList[1], G.configs.Runner.successCountList[1]);
				}
				else
				{
					s += "\n第二步未执行";
				}

				if (EditorUtility.DisplayDialog("Preprocessor", s, "查看日志文件", "关闭"))
				{
					foreach (string logFilePath in G.configs.Runner.LogFilePathList)
					{
						if (!string.IsNullOrEmpty(logFilePath))
						{
							Process.Start(logFilePath);
						}
					}
				}
			}
		}

		private void ClickedRevert()
		{
			ChangeSavedConfig(selectedSavedConfigIndex);
		}

		private void ClickedShowConfigFile()
		{
			string path = "";
			try
			{
				path = Path.Combine(G.configs.LocalConfig.Local_SavedConfigsFolderPath, savedConfigNames[selectedSavedConfigIndex] + ".json");
			}
			catch { }
			if (!File.Exists(path))
			{
				path = G.configs.LocalConfig.Local_SavedConfigsFolderPath;
			}
			EditorUtility.RevealInFinder(path);
		}

		private bool ShowDropdown()
		{
			if (G.configs.Dirty && selectedSavedConfigIndex != -1)
				savedConfigNames[selectedSavedConfigIndex] += "*";
			int selectedIndex_new = EditorGUILayout.Popup(selectedSavedConfigIndex, savedConfigNames.ToArray(), popupStyle, popupOptions);
			if (G.configs.Dirty && selectedSavedConfigIndex != -1)
				savedConfigNames[selectedSavedConfigIndex] = savedConfigNames[selectedSavedConfigIndex].Remove(savedConfigNames[selectedSavedConfigIndex].Length - 1);
            
			if (selectedIndex_new != selectedSavedConfigIndex)
			{
				ChangeSavedConfig(selectedIndex_new);
                return true;
			}
            return false;
		}

		private void ChangeSavedConfig(int selectedIndex_new)
		{
			bool ensureLoad = true;
			if (G.configs.Dirty)
			{
                ensureLoad = EditorUtility.DisplayDialog("切换配置", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
			}
			if (ensureLoad)
			{
				try
				{
					var newCurrentSavedConfig = new Configs.CurrentSavedConfig();
					string newConfigName = savedConfigNames[selectedIndex_new] + ".json";
					newCurrentSavedConfig.JsonPath = Path.Combine(G.configs.LocalConfig.Local_SavedConfigsFolderPath, newConfigName);
					newCurrentSavedConfig.Load();
					//至此加载成功
					G.configs.CurrentConfig.Json.CurrentSavedConfigName = newConfigName;
					G.configs.CurrentSavedConfig = newCurrentSavedConfig;
					selectedSavedConfigIndex = selectedIndex_new;
					G.g.OnChangeCurrentConfig();
				}
				catch (Exception e)
				{
					EditorUtility.DisplayDialog("切换配置", "切换配置时发生错误：" + e.Message, "确定");
				}
			}
		}

		private void ShowInputField()
		{
			GUI.SetNextControlName("InputField1");
			string tip = "<输入名称>(回车确定，空串取消)";
			string s = EditorGUILayout.DelayedTextField(tip, popupStyle, popupOptions);
			GUI.FocusControl("InputField1");
			s = s.Trim().Replace('\\', '/');
			if (s != tip)
			{
				if (s != "")
				{
					try
					{
						string path = Path.Combine(G.configs.LocalConfig.Local_SavedConfigsFolderPath, s + ".json");
						if (File.Exists(path))
							EditorUtility.DisplayDialog("创建失败", "创建新文件失败，该名称已存在！", "确定");
						else
						{
							CreateNewSavedConfig(s, path);
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

		private void CreateNewSavedConfig(string name, string path)
		{
			//新建   
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.Create(path).Close();
			EditorUtility.DisplayDialog("创建成功", "创建成功!", "确定");
			//更新列表
			savedConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.configs.LocalConfig.Local_SavedConfigsFolderPath);
			//保存
			G.configs.CurrentSavedConfig.JsonPath = path;
			G.configs.CurrentSavedConfig.Save();
			//切换
			G.configs.Dirty = false;
			ChangeSavedConfig(savedConfigNames.IndexOf(name));
            //用于总控
            G.g.OnChangeConfigList();
        }

		private void ClickedSave()
		{
			if (selectedSavedConfigIndex == -1)
			{
				return;
			}
			bool ensure = true;
			if (G.configs.Dirty)
			{
				ensure = EditorUtility.DisplayDialog("保存", "是否保存并覆盖原配置：" + savedConfigNames[selectedSavedConfigIndex], "覆盖保存", "取消");
			}
			if (!ensure) return;

			SaveCurrentConfig();
            
        }

		private void SaveCurrentConfig()
		{
			try
			{
				G.configs.CurrentSavedConfig.Save();
				EditorUtility.DisplayDialog("保存配置", "保存成功！", "确定");
				G.g.OnChangeCurrentConfig(); //用于刷新dirty
			}
			catch (Exception e)
			{
				EditorUtility.DisplayDialog("保存配置", "保存配置时发生错误：" + e.Message, "确定");
			}
            //用于总控
            G.g.OnChangeCurrentConfig();
		}

		private void ClickedNew()
		{
			creatingNewConfig = true;
		}

		private void ChangeRootPath(string path)
		{
			bool ensure = true;
			if (G.configs.Dirty)
			{
				ensure = EditorUtility.DisplayDialog("改变根目录", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
			}
			if (ensure)
			{
				Configs.Configs newConfigs = new Configs.Configs();
				if (!newConfigs.LoadAllConfigs(path)) return;
				G.configs = newConfigs;
				G.g.OnChangeCurrentConfig();
				G.configs.LocalConfig.Save();
				selectedSavedConfigIndex = savedConfigNames.IndexOf(Path.GetFileNameWithoutExtension(G.configs.CurrentConfig.Json.CurrentSavedConfigName));
				HandleApplyingWarning();
			}
		}
		double lastTime;
		private void ClickedSyncDirectory()
		{
			if (!Directory.Exists(Path.GetDirectoryName(G.configs.LocalConfig.PreStoredAssetsFolderPath)))
			{
				EditorUtility.DisplayDialog("同步目录", "同步失败，找不到目录:" +
											Path.GetDirectoryName(G.configs.LocalConfig.PreStoredAssetsFolderPath), "确定");
                return;
            }

			bool ensure = EditorUtility.DisplayDialog("同步目录", "确定要同步Assets的完整目录结构到PreStoredAssets下？（仅添加）", "同步", "取消");
            if (ensure)
            {
                EditorUtility.DisplayProgressBar("同步目录", "正在读取Assets目录信息", 0);
                if (!Directory.Exists(G.configs.LocalConfig.PreStoredAssetsFolderPath))
                {
                    Directory.CreateDirectory(G.configs.LocalConfig.PreStoredAssetsFolderPath);
                }
                var allDirectories = Directory.GetDirectories("Assets/", "*", SearchOption.AllDirectories);
                int total = allDirectories.Length;
				int i;
				string item = "";
				for (i = 0; i < total; i++)
				{
					item = allDirectories[i];
					string path = Path.Combine(G.configs.LocalConfig.PreStoredAssetsFolderPath, item.Remove(0, 7));
					Directory.CreateDirectory(path);
                    if (EditorApplication.timeSinceStartup - lastTime > 0.06f)
					{
						EditorUtility.DisplayProgressBar("同步目录", path, (float)i / total);
                        lastTime = EditorApplication.timeSinceStartup;
					}
				}
				EditorUtility.ClearProgressBar();
			}
		}

		public void OnDestory()
		{
			if (G.configs.Dirty && selectedSavedConfigIndex != -1)
			{
				bool ensure = EditorUtility.DisplayDialog("Preprocessor", "修改未保存！是否保存修改并覆盖原配置：“" +
					savedConfigNames[selectedSavedConfigIndex] + "”？", "保存并退出", "直接退出");
				if (ensure)
				{
					SaveCurrentConfig();
				}
			}
		}

		private void HandleApplyingWarning()
		{
			if (G.configs.CurrentConfig.Json.Applying)
			{
                EditorUtility.DisplayDialog("提示", "上次应用配置时发生错误或被强制中断，可能导致对Unity内的文件替换不完全或错误、对meta文件的修改不完全或错误，建议还原meta文件、重新应用配置。", "确定");
			}
		}
	}
}