using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiXuFeng.AssetPreprocessor.Editor
{
    public class SettingPanel
	{
		bool creatingNewConfig;
		bool firstShow;
		GUIStyle buttonStyle;
		GUILayoutOption[] buttonOptions;
		private GUIStyle popupStyle;
		private GUILayoutOption[] popupOptions;
		List<string> savedConfigNames;
		int selectedSavedConfigIndex = -1;

		public void OnEnable()
		{
			firstShow = true;
			FindSavedConfigs(Configs.configs.LocalConfig.Local_SavedConfigsFolderPath);
			string extension = Path.GetExtension(Configs.configs.PreprocessorConfig.CurrentSavedConfigName);
            if (Configs.configs.PreprocessorConfig.CurrentSavedConfigName != null)
            {
                selectedSavedConfigIndex = savedConfigNames.IndexOf(Configs.configs.PreprocessorConfig.CurrentSavedConfigName.Remove(
                    Configs.configs.PreprocessorConfig.CurrentSavedConfigName.Length - extension.Length, extension.Length));
            }
			Configs.g.OnChangeCurrentConfig += () => { Configs.configs.Dirty = false; };
			HandleApplyingWarning();
		}

		public void OnGUI()
		{
			if (firstShow)
			{
				InitStyles();
				firstShow = false;
			}
			if (GUI.GetNameOfFocusedControl() != "InputField1")
			{
				creatingNewConfig = false;
			}
			GUILayout.FlexibleSpace();
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("根目录:", GUILayout.Width(45));
				string path = EditorGUILayout.DelayedTextField(Configs.configs.LocalConfig.RootPath);
				if (GUILayout.Button("...", GUILayout.MaxWidth(24)))
				{
					path = EditorUtility.OpenFolderPanel("打开根目录", Configs.configs.LocalConfig.RootPath, null);
				}
				if (!string.IsNullOrEmpty(path) && path != Configs.configs.LocalConfig.RootPath)
				{
					ChangeRootPath(path);
				}
			}
			GUILayout.FlexibleSpace();
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				GUILayout.Space(10);
				if (GUILayout.Button(new GUIContent("New", "新建配置文件"), buttonStyle, buttonOptions)) ClickedNew();
				if (GUILayout.Button(new GUIContent("Save", "保存配置文件"), buttonStyle, buttonOptions)) ClickedSave();
				{
					if (creatingNewConfig)
						ShowInputField();
					else
						ShowDropdown();
				}

				if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("ViewToolOrbit"), "查看该文件"), GUILayout.Height(25)))
					ClickedShowConfigFile();
			}
			GUILayout.FlexibleSpace();
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button(new GUIContent("Sync Directory", "同步Assets目录结构"), buttonStyle, GUILayout.MaxHeight(25), GUILayout.MaxWidth(120))) ClickedSyncDirectory();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(new GUIContent("Revert"), buttonStyle, buttonOptions)) ClickedRevert();
				if (GUILayout.Button(new GUIContent("Apply"), buttonStyle, buttonOptions)) ClickedApply();
			}
			GUILayout.FlexibleSpace();
		}

		private void InitStyles()
		{
			buttonStyle = new GUIStyle("Button") { fixedHeight = 0, fixedWidth = 0 };
			buttonOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(70) };
			popupStyle = new GUIStyle("dropdown") { fixedHeight = 0, fixedWidth = 0 };
			popupOptions = new GUILayoutOption[] { GUILayout.MaxHeight(25), GUILayout.MaxWidth(200) };
		}

		private void ClickedApply()
		{
			if (!Directory.Exists(Configs.configs.LocalConfig.PreStoredAssetsFolderPath))
			{
				EditorUtility.DisplayDialog("应用配置", "不能应用配置，找不到目录:" + Configs.configs.LocalConfig.PreStoredAssetsFolderPath, "确定");
				return;
			}
			bool ensure = EditorUtility.DisplayDialog("Preprocessor", "确定应用当前配置？应用过程不可中断。", "确定", "取消");
			if (ensure)
			{
				Configs.configs.PreprocessorConfig.Applying = true;
				Configs.configs.PreprocessorConfig.Save();
                try
                {
                    ApplyOptions();
                }
                catch(Exception e)
                {
                    EditorUtility.DisplayDialog("Preprocessor", "应用配置时发生错误：" + e.ToString(), "确定");
                }
				Configs.configs.PreprocessorConfig.Applying = false;
				Configs.configs.PreprocessorConfig.Save();
				AssetDatabase.Refresh();
				string s = "转换完成！\n";
                if (process.ExitCode != 0)
                {
					s = string.Format("操作中断！执行第{0}步时出错：{1}\n", currentShellIndex + 1, errorMessage);
                }
				s += string.Format("\n第一步(拷贝文件):\nPreStoredAssets中符合标签的文件共有{0}个，跳过{1}个，成功拷贝{2}个。\n", totalCountList[0], skipCountList[0], successCountList[0]);
                if (currentShellIndex >= 1)
				{
					s += string.Format("\n第二步(修改meta):\n共有{0}个meta文件，跳过{1}个，成功修改{2}个。\n", totalCountList[1], skipCountList[1], successCountList[1]);
				}
				else
				{
					s += "\n第二步未执行";
				}

				if (EditorUtility.DisplayDialog("Preprocessor", s, "查看日志文件", "关闭"))
				{
					foreach (string logFilePath in LogFilePathList)
					{
						if (!string.IsNullOrEmpty(logFilePath))
						{
							Process.Start(logFilePath);
						}
					}
				}
			}
		}

		private void ApplyOptions()
		{
			string platform = "";//TODO：这里能否使用EditorUserBuildSettings.activeBuildTarget？
#if UNITY_ANDROID
			platform = "Android";
#elif UNITY_IOS
            platform = "iPhone";
#endif
            //TODO:这里能否再优化一下结构？
            string copyFileArgs = "Assets/AssetsPipeline/AssetPreprocessor/Shell/CopyFile.sh " + Configs.configs.LocalConfig.LogsFolderPath + " Assets " + Configs.configs.LocalConfig.PreStoredAssetsFolderPath;
			string changeMetaArgs = "Assets/AssetsPipeline/AssetPreprocessor/Shell/ModifyMeta.sh " + Configs.configs.LocalConfig.LogsFolderPath + " Assets " + platform;
			int changeMetaArgsLength = changeMetaArgs.Length;
			foreach (var group in Configs.configs.CurrentSavedConfig.Groups)
			{
				switch (group.FullGroupName)
				{
					case "Language":
						copyFileArgs += Arguments(group.Options[0]);
						break;
					case "Texture Import Settings Override/Max Size":
						foreach (string option in group.Options)
						{
							string[] splits = option.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
							changeMetaArgs += Arguments("maxTextureSize", splits[0].Trim(), splits[1].Trim());
						}
						break;
					case "Texture Import Settings Override/Format":
						foreach (string option in group.Options)
						{
							string[] splits = option.Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
							TextureFormat source, target;
							if (!StringMaps.TextureMaps.TryGetValue(splits[0].Trim(), out source))
								source = (TextureFormat)Enum.Parse(typeof(TextureFormat), splits[0].Trim().Replace(' ', '_'));
							if (!StringMaps.TextureMaps.TryGetValue(splits[0].Trim(), out target))
								target = (TextureFormat)Enum.Parse(typeof(TextureFormat), splits[1].Trim().Replace(' ', '_'));
							changeMetaArgs += Arguments("textureFormat", ((int)source).ToString(), ((int)target).ToString());
						}
						break;
					default:
						break;
				}
			}
            currentShellIndex = 0;
			totalCountList = new List<int> { -1, -1 };
			skipCountList = new List<int>() { -1, -1 };
			successCountList = new List<int> { -1, -1 };
			LogFilePathList = new List<string> { null, null };
            
			RunShell_ShowProgress_WaitForExit(copyFileArgs, "正在从PreStoredAssets里查找文件...", "第1步(共2步) 从PreStoredAssets拷贝文件至Assets目录");
			if (process.ExitCode != 0 || changeMetaArgs.Length == changeMetaArgsLength) //第一步出错或第二步没有勾选任何选项则不再向下执行
			{
				return;
			}
			currentShellIndex++;
			RunShell_ShowProgress_WaitForExit(changeMetaArgs, "正在查找.meta文件...", "第2步(共2步) 修改meta文件");
		}

		private void RunShell_ShowProgress_WaitForExit(string arguments, string startMessage, string titleMessage)
		{
			applyingFile = startMessage;
			applyingID = 0;
			totalApplyCount = 0;
			RunShell(arguments);
			while (RuningShellCount > 0)
			{
				string s = string.Format("({0}/{1})", applyingID, totalApplyCount);
				EditorUtility.DisplayProgressBar(titleMessage + s, applyingFile, (float)applyingID / totalApplyCount);
				System.Threading.Thread.Sleep(50);
			}
			EditorUtility.ClearProgressBar();
		}

		string Arguments(params string[] arguments)
		{
			return " " + string.Join(" ", arguments);
		}
		void RunShell(string arguments)
		{
			//实例一个process类
			process = new System.Diagnostics.Process();
			//设定程序名
			process.StartInfo.FileName = "/bin/bash";
			process.StartInfo.Arguments = arguments;
			//Shell的使用
			process.StartInfo.UseShellExecute = false;
			//重新定向标准输入，错误输出
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			//设置cmd窗口的显示
			process.StartInfo.CreateNoWindow = true;
			//设置标准输出响应
			process.OutputDataReceived += OnShellOutputDataReceived;
			process.ErrorDataReceived += OnShellErrorDataReceived;
			process.EnableRaisingEvents = true;
			process.Exited += OnShellExited;
			//开始
			RuningShellCount++;
			process.Start();
			process.BeginOutputReadLine();
		}

		private void OnShellErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			errorMessage = e.Data;
		}

		private void OnShellExited(object sender, EventArgs e)
		{
			RuningShellCount--;
		}

		Process process;
		int RuningShellCount;
		int applyingID;
		int totalApplyCount;
		List<int> totalCountList;
		List<int> skipCountList;
		List<int> successCountList;
		List<string> LogFilePathList;
		int currentShellIndex;
		string applyingFile;
		string errorMessage;

		private void OnShellOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				string[] messages = e.Data.Split();
				switch (messages[0])
				{
					case "Total:":
						totalApplyCount = int.Parse(messages[1]);
						totalCountList[currentShellIndex] = totalApplyCount;
						break;
					case "Applying:":
						applyingID = int.Parse(messages[1]);
						applyingFile = string.Join(" ", messages.Skip(2).ToArray());
						break;
					case "Error:":
						errorMessage = string.Join(" ", messages.Skip(1).ToArray());
						break;
					case "Skip:":
						skipCountList[currentShellIndex] = int.Parse(messages[1]);
						break;
					case "Success:":
						successCountList[currentShellIndex] = int.Parse(messages[1]);
						break;
					case "LogFilePath:":
						LogFilePathList[currentShellIndex] = string.Join(" ", messages.Skip(1).ToArray());
						break;
					default:
						break;
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
				path = Path.Combine(Configs.configs.LocalConfig.Local_SavedConfigsFolderPath, savedConfigNames[selectedSavedConfigIndex] + ".json");
			}
			catch { }
			if (!File.Exists(path))
			{
				path = Configs.configs.LocalConfig.Local_SavedConfigsFolderPath;
			}
			EditorUtility.RevealInFinder(path);
		}

		private void ShowDropdown()
		{
			if (Configs.configs.Dirty && selectedSavedConfigIndex != -1)
				savedConfigNames[selectedSavedConfigIndex] += "*";
			int selectedIndex_new = EditorGUILayout.Popup(selectedSavedConfigIndex, savedConfigNames.ToArray(), popupStyle, popupOptions);
			if (Configs.configs.Dirty && selectedSavedConfigIndex != -1)
				savedConfigNames[selectedSavedConfigIndex] = savedConfigNames[selectedSavedConfigIndex].Remove(savedConfigNames[selectedSavedConfigIndex].Length - 1);

			if (selectedIndex_new != selectedSavedConfigIndex)
			{
				ChangeSavedConfig(selectedIndex_new);
			}
		}

		private void ChangeSavedConfig(int selectedIndex_new)
		{
			bool ensureLoad = true;
			if (Configs.configs.Dirty)
			{
				ensureLoad = !EditorUtility.DisplayDialog("切换配置", "更改未保存，是否要放弃更改？", "返回", "放弃保存");
			}
			if (ensureLoad)
			{
				try
				{
					var newCurrentSavedConfig = new Config.CurrentSavedConfig();
					string newConfigName = savedConfigNames[selectedIndex_new] + ".json";
					newCurrentSavedConfig.Path = Path.Combine(Configs.configs.LocalConfig.Local_SavedConfigsFolderPath, newConfigName);
					newCurrentSavedConfig.Load();
					//至此加载成功
					Configs.configs.PreprocessorConfig.CurrentSavedConfigName = newConfigName;
					Configs.configs.CurrentSavedConfig = newCurrentSavedConfig;
					selectedSavedConfigIndex = selectedIndex_new;
					Configs.g.OnChangeCurrentConfig();
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
						string path = Path.Combine(Configs.configs.LocalConfig.Local_SavedConfigsFolderPath, s + ".json");
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
			FindSavedConfigs(Configs.configs.LocalConfig.Local_SavedConfigsFolderPath);
			//保存
			Configs.configs.CurrentSavedConfig.Path = path;
			Configs.configs.CurrentSavedConfig.Save();
			//切换
			Configs.configs.Dirty = false;
			ChangeSavedConfig(savedConfigNames.IndexOf(name));
		}

		private void ClickedSave()
		{
			if (selectedSavedConfigIndex == -1)
			{
				return;
			}
			bool ensure = true;
			if (Configs.configs.Dirty)
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
				Configs.configs.CurrentSavedConfig.Save();
				EditorUtility.DisplayDialog("保存配置", "保存成功！", "确定");
				Configs.g.OnChangeCurrentConfig(); //用于刷新dirty
			}
			catch (Exception e)
			{
				EditorUtility.DisplayDialog("保存配置", "保存配置时发生错误：" + e.Message, "确定");
			}
		}

		private void ClickedNew()
		{
			creatingNewConfig = true;
		}

		private void ChangeRootPath(string path)
		{
			bool ensure = true;
			if (Configs.configs.Dirty)
			{
				ensure = !EditorUtility.DisplayDialog("改变根目录", "更改未保存，是否要放弃更改？", "返回", "放弃保存");
			}
			if (ensure)
			{
				Config.Configs newConfigs = new Config.Configs();
				if (!newConfigs.LoadLocalConfig()) return;
				newConfigs.LocalConfig.RootPath = path;
				if (!newConfigs.LoadAllConfigsByLocalConfig()) return;
				Configs.configs = newConfigs;
				Configs.g.OnChangeCurrentConfig();
				Configs.configs.LocalConfig.Save();
				selectedSavedConfigIndex = savedConfigNames.IndexOf(Path.GetFileNameWithoutExtension(Configs.configs.PreprocessorConfig.CurrentSavedConfigName));
				HandleApplyingWarning();
			}
		}
		float lastTime;
		private void ClickedSyncDirectory()
		{
			if (!Directory.Exists(Configs.configs.LocalConfig.PreStoredAssetsFolderPath))
			{
				EditorUtility.DisplayDialog("同步目录", "同步失败，找不到目录:" + Configs.configs.LocalConfig.PreStoredAssetsFolderPath, "确定");
				return;
			}
			bool ensure = EditorUtility.DisplayDialog("同步目录", "确定要同步Assets的完整目录结构到PreStoredAssets下？（仅添加）", "同步", "取消");
			if (ensure)
			{
				EditorUtility.DisplayProgressBar("同步目录", "正在读取Assets目录信息", 0);
				var allDirectories = Directory.GetDirectories("Assets/", "*", SearchOption.AllDirectories);
				int total = allDirectories.Length;
				int i;
				string item = "";
				for (i = 0; i < total; i++)
				{
					item = allDirectories[i];
					string path = Path.Combine(Configs.configs.LocalConfig.PreStoredAssetsFolderPath, item.Remove(0, 7));
					Directory.CreateDirectory(path);
					if (Time.realtimeSinceStartup - lastTime > 0.06f)
					{
						EditorUtility.DisplayProgressBar("同步目录", path, (float)i / total);
						lastTime = Time.realtimeSinceStartup;
					}
				}
				EditorUtility.ClearProgressBar();
			}
		}

		private void FindSavedConfigs(string path)
		{
			try
			{
				List<string> savedConfigPaths = new List<string>();
				savedConfigNames = new List<string>();
				RecursiveFindJson(path, savedConfigPaths);
				foreach (var configPath in savedConfigPaths)
				{
					string extension = Path.GetExtension(configPath);
					savedConfigNames.Add(configPath.Remove(configPath.Length - extension.Length, extension.Length).Remove(0, path.Length + 1).Replace('\\', '/'));
				}
			}
			catch (Exception e)
			{
				EditorUtility.DisplayDialog("错误", "查找映射文件列表时发生错误：" + e.Message, "确定");
			}
		}

		private static void RecursiveFindJson(string path, List<string> jsonPaths)
		{
			jsonPaths.AddRange(Directory.GetFiles(path, "*.json"));
			foreach (var folder in Directory.GetDirectories(path))
			{
				RecursiveFindJson(folder, jsonPaths);
			}
		}

		public void OnDisable()
		{
			if (Configs.configs.Dirty && selectedSavedConfigIndex != -1)
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
			if (Configs.configs.PreprocessorConfig.Applying)
			{
				EditorUtility.DisplayDialog("提示", "上次应用该配置时被异常中断（可能是操作异常，死机，停电等原因）" +
					"，建议重新应用该配置", "确定");
			}
		}
	}
}
