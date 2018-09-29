﻿using System;
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
        [SerializeField] string[] userConfigNames;
        [SerializeField] int selectedUserConfigIndex = -1;
        [SerializeField] GUIStyle buttonStyle;
        [SerializeField] GUIStyle popupStyle;
        GUILayoutOption[] buttonOptions = { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
        GUILayoutOption[] popupOptions = { GUILayout.MaxHeight(25), GUILayout.MaxWidth(200) };
        public void Awake()
        {
            InitStyles();
            userConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.ModuleConfig.UserConfigsFolderPath);
            string currentUserConfigName = G.Module.ModuleStateConfig.Json.CurrentUserConfigName;
            if (currentUserConfigName != null)
            {
                string extension = Path.GetExtension(currentUserConfigName);
                selectedUserConfigIndex = userConfigNames.IndexOf(currentUserConfigName.Remove(
                    currentUserConfigName.Length - extension.Length, extension.Length));
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
            G.g.OnChangeCurrentUserConfig += () => { G.Module.IsDirty = false; };
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
            string path = EditorGUILayout.DelayedTextField(G.Module.ModuleConfig.PipelineRootPath);
            if (GUILayout.Button("...", GUILayout.MaxWidth(24)))
            {
                path = EditorUtility.OpenFolderPanel("打开根目录", G.Module.ModuleConfig.PipelineRootPath, null);
            }
            if (!string.IsNullOrEmpty(path) && path != G.Module.ModuleConfig.PipelineRootPath)
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
                    G.Runner.Run();
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
                if (G.Runner.process.ExitCode != 0)
                {
					s = string.Format("操作中断！执行第{0}步时出错：{1}\n", G.Runner.currentShellIndex + 1, G.Runner.errorMessage);
                }
				s += string.Format("\n第一步(拷贝文件):\nPreStoredAssets中符合标签的文件共有{0}个，跳过{1}个，成功拷贝{2}个。\n",
                    G.Runner.totalCountList[0], G.Runner.skipCountList[0], G.Runner.successCountList[0]);
                if (G.Runner.currentShellIndex >= 1)
				{
					s += string.Format("\n第二步(修改meta):\n共有{0}个meta文件，跳过{1}个，成功修改{2}个。\n",
                        G.Runner.totalCountList[1], G.Runner.skipCountList[1], G.Runner.successCountList[1]);
				}
				else
				{
					s += "\n第二步未执行";
				}

				if (EditorUtility.DisplayDialog("Preprocessor", s, "查看日志文件", "关闭"))
				{
					foreach (string logFilePath in G.Runner.LogFilePathList)
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
			ChangeUserConfig(selectedUserConfigIndex);
		}

		private void ClickedShowConfigFile()
		{
			string path = "";
			try
			{
				path = Path.Combine(G.Module.ModuleConfig.UserConfigsFolderPath, userConfigNames[selectedUserConfigIndex] + ".json");
			}
			catch { }
			if (!File.Exists(path))
			{
				path = G.Module.ModuleConfig.UserConfigsFolderPath;
			}
			EditorUtility.RevealInFinder(path);
		}

		private bool ShowDropdown()
		{
			if (G.Module.IsDirty && selectedUserConfigIndex != -1)
				userConfigNames[selectedUserConfigIndex] += "*";
			int selectedIndex_new = EditorGUILayout.Popup(selectedUserConfigIndex, userConfigNames.ToArray(), popupStyle, popupOptions);
			if (G.Module.IsDirty && selectedUserConfigIndex != -1)
				userConfigNames[selectedUserConfigIndex] = userConfigNames[selectedUserConfigIndex].Remove(userConfigNames[selectedUserConfigIndex].Length - 1);
            
			if (selectedIndex_new != selectedUserConfigIndex)
			{
				ChangeUserConfig(selectedIndex_new);
                return true;
			}
            return false;
		}

		private void ChangeUserConfig(int selectedIndex_new)
		{
			bool ensureLoad = true;
			if (G.Module.IsDirty)
			{
                ensureLoad = EditorUtility.DisplayDialog("切换配置", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
			}
			if (ensureLoad)
			{
				try
				{
					var newUserConfig = new Configs.UserConfig();
					string newConfigName = userConfigNames[selectedIndex_new] + ".json";
					newUserConfig.JsonPath = Path.Combine(G.Module.ModuleConfig.UserConfigsFolderPath, newConfigName);
					newUserConfig.Load();
					//至此加载成功
					G.Module.ModuleStateConfig.Json.CurrentUserConfigName = newConfigName;
					G.Module.UserConfig = newUserConfig;
					selectedUserConfigIndex = selectedIndex_new;
					G.g.OnChangeCurrentUserConfig();
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
						string path = Path.Combine(G.Module.ModuleConfig.UserConfigsFolderPath, s + ".json");
						if (File.Exists(path))
							EditorUtility.DisplayDialog("创建失败", "创建新文件失败，该名称已存在！", "确定");
						else
						{
							CreateNewUserConfig(s, path);
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

		private void CreateNewUserConfig(string name, string path)
		{
			//新建   
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.Create(path).Close();
			EditorUtility.DisplayDialog("创建成功", "创建成功!", "确定");
			//更新列表
			userConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.ModuleConfig.UserConfigsFolderPath);
			//保存
			G.Module.ModuleStateConfig.JsonPath = path;
			G.Module.ModuleStateConfig.Save();
			//切换
			G.Module.IsDirty = false;
			ChangeUserConfig(userConfigNames.IndexOf(name));
            //用于总控
            G.g.OnChangeConfigList();
        }

		private void ClickedSave()
		{
			if (selectedUserConfigIndex == -1)
			{
				return;
			}
			bool ensure = true;
			if (G.Module.IsDirty)
			{
				ensure = EditorUtility.DisplayDialog("保存", "是否保存并覆盖原配置：" + userConfigNames[selectedUserConfigIndex], "覆盖保存", "取消");
			}
			if (!ensure) return;

			SaveUserConfig();
            
        }

		private void SaveUserConfig()
		{
			try
			{
				G.Module.UserConfig.Save();
				EditorUtility.DisplayDialog("保存配置", "保存成功！", "确定");
				G.g.OnChangeCurrentUserConfig(); //用于刷新dirty 和 总控事件
			}
			catch (Exception e)
			{
				EditorUtility.DisplayDialog("保存配置", "保存配置时发生错误：" + e.Message, "确定");
			}
		}

		private void ClickedNew()
		{
			creatingNewConfig = true;
            ShowInputField();
		}

		private void ChangeRootPath(string path)
		{
			bool ensure = true;
			if (G.Module.IsDirty)
			{
				ensure = EditorUtility.DisplayDialog("改变根目录", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
			}
			if (ensure)
			{
                string originPipelineRootPath = CommonModule.CommonConfig.Json.PipelineRootPath;
                Module newModule = new Module();
                if (!newModule.LoadAllConfigs(path))
                {
                    CommonModule.CommonConfig.Json.PipelineRootPath = originPipelineRootPath;
                    return;
                }
                G.Module = newModule;
                G.Runner.Module = newModule;
				G.g.OnChangeCurrentUserConfig();
				CommonModule.CommonConfig.Save();
				selectedUserConfigIndex = userConfigNames.IndexOf(Path.GetFileNameWithoutExtension(G.Module.ModuleStateConfig.Json.CurrentUserConfigName));
				HandleApplyingWarning();
			}
		}
		double lastTime;
		private void ClickedSyncDirectory()
		{
			if (!Directory.Exists(Path.GetDirectoryName(G.Module.ModuleConfig.PreStoredAssetsFolderPath)))
			{
				EditorUtility.DisplayDialog("同步目录", "同步失败，找不到目录:" +
											Path.GetDirectoryName(G.Module.ModuleConfig.PreStoredAssetsFolderPath), "确定");
                return;
            }

			bool ensure = EditorUtility.DisplayDialog("同步目录", "确定要同步Assets的完整目录结构到PreStoredAssets下？（仅添加）", "同步", "取消");
            if (ensure)
            {
                EditorUtility.DisplayProgressBar("同步目录", "正在读取Assets目录信息", 0);
                if (!Directory.Exists(G.Module.ModuleConfig.PreStoredAssetsFolderPath))
                {
                    Directory.CreateDirectory(G.Module.ModuleConfig.PreStoredAssetsFolderPath);
                }
                var allDirectories = Directory.GetDirectories("Assets/", "*", SearchOption.AllDirectories);
                int total = allDirectories.Length;
				int i;
				string item = "";
				for (i = 0; i < total; i++)
				{
					item = allDirectories[i];
					string path = Path.Combine(G.Module.ModuleConfig.PreStoredAssetsFolderPath, item.Remove(0, 7));
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
			if (G.Module.IsDirty && selectedUserConfigIndex != -1)
			{
				bool ensure = EditorUtility.DisplayDialog("Preprocessor", "修改未保存！是否保存修改并覆盖原配置：“" +
					userConfigNames[selectedUserConfigIndex] + "”？", "保存并退出", "直接退出");
				if (ensure)
				{
					SaveUserConfig();
				}
			}
		}

		private void HandleApplyingWarning()
		{
			if (G.Module.ModuleStateConfig.Json.Applying)
			{
                EditorUtility.DisplayDialog("提示", "上次应用配置时发生错误或被强制中断，可能导致对Unity内的文件替换不完全或错误、对meta文件的修改不完全或错误，建议还原meta文件、重新应用配置。", "确定");
			}
		}
	}
}