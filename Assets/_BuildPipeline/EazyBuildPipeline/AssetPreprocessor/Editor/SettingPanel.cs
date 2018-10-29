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
            //G.Module.DisplayRunError();
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
            EBPEditorGUILayout.RootSettingLine(G.Module, ChangeRootPath);
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
            if (G.Module.StateConfigAvailable)
            {
                if (GUILayout.Button(new GUIContent("Apply"), buttonStyle, buttonOptions))
                { ClickedApply(); return; }
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Check"), buttonStyle, buttonOptions))
                { ClickedCheck(); return; }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void ClickedCheck()
        {
            try
            {
                G.Runner.Check(true);
                G.Module.DisplayDialog("检查正常！");
            }
            catch(EBPCheckFailedException e)
            {
                G.Module.DisplayDialog(e.Message);
            }
        }
        private void ClickedApply()
		{
            try
            {
                G.Runner.Check();
            }
            catch(EBPCheckFailedException e)
            {
                G.Module.DisplayDialog(e.Message);
                return;
            }
			bool ensure = G.Module.DisplayDialog("确定应用当前配置？应用过程不可中断。", "确定", "取消");
			if (ensure)
            {
                try
                {
                    G.Runner.Run();
                }
                catch
                {
                    G.Module.DisplayRunError();
                    return;
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

				if (G.Module.DisplayDialog(s, "查看日志文件", "关闭"))
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
                ensureLoad = G.Module.DisplayDialog("更改未保存，是否要放弃更改？", "放弃保存", "返回");
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
					G.Module.DisplayDialog("切换配置时发生错误：" + e.Message);
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
                        {
                            G.Module.DisplayDialog("创建失败", "创建新文件失败，该名称已存在！", "确定");
                        }
                        else
                        {
                            CreateNewUserConfig(s, path);
                        }
					}
					catch (Exception e)
					{
                        G.Module.DisplayDialog("创建时发生错误：" + e.Message);
					}
				}
				creatingNewConfig = false;
			}
		}

		private void CreateNewUserConfig(string name, string path)
		{
            //新建
            if (!Directory.Exists(CommonModule.CommonConfig.UserConfigsRootPath))
            {
                G.Module.DisplayDialog("创建失败！用户配置根目录不存在：");
                return;
            }
            //保存
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            G.Module.UserConfig.JsonPath = path;
            SaveUserConfig();
            //更新列表
            userConfigNames = EBPUtility.FindFilesRelativePathWithoutExtension(G.Module.ModuleConfig.UserConfigsFolderPath);
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
				ensure = G.Module.DisplayDialog("是否保存并覆盖原配置：" + userConfigNames[selectedUserConfigIndex], "覆盖保存", "取消");
			}
			if (!ensure) return;

			SaveUserConfig();

        }

		private void SaveUserConfig()
		{
			try
			{
				G.Module.UserConfig.Save();
				G.Module.DisplayDialog("保存成功！");
				G.g.OnChangeCurrentUserConfig(); //用于刷新dirty 和 总控事件
			}
			catch (Exception e)
			{
				G.Module.DisplayDialog("保存配置时发生错误：" + e.Message);
			}
		}

		private void ClickedNew()
		{
			creatingNewConfig = true;
            ShowInputField();
		}

		private void ChangeRootPath(string path)
        {
            CommonModule.ChangeRootPath(path);
            G.Module.LoadAllConfigs();
            G.g.OnChangeCurrentUserConfig();
            selectedUserConfigIndex = userConfigNames.IndexOf(Path.GetFileNameWithoutExtension(G.Module.ModuleStateConfig.Json.CurrentUserConfigName));
            //G.Module.DisplayRunError();
        }
        double lastTime;
		private void ClickedSyncDirectory()
		{
			if (!Directory.Exists(Path.GetDirectoryName(G.Module.ModuleConfig.PreStoredAssetsFolderPath)))
			{
                G.Module.DisplayDialog("同步失败，找不到目录:" + Path.GetDirectoryName(G.Module.ModuleConfig.PreStoredAssetsFolderPath));
                return;
            }

			bool ensure = G.Module.DisplayDialog("确定要同步Assets的完整目录结构到PreStoredAssets下？（仅添加）", "同步", "取消");
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
                bool ensure = G.Module.DisplayDialog("修改未保存！是否保存修改并覆盖原配置：“" +
                    userConfigNames[selectedUserConfigIndex] + "”？", "保存并退出", "直接退出");
				if (ensure)
				{
					SaveUserConfig();
				}
			}
		}
	}
}