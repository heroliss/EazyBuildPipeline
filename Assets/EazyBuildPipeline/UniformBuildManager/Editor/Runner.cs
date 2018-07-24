using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    public class Runner
    {
        public Configs.Configs configs;
        public Runner(Configs.Configs configs)
        {
            this.configs = configs;
        }
        public bool Check()
        {        
            //验证根目录
            if (!Directory.Exists(configs.LocalConfig.PlayersFolderPath))
            {
                configs.DisplayDialog("目录不存在：" + configs.LocalConfig.PlayersFolderPath);
                return false;
            }
            var target = BuildTarget.NoTarget;
            string targetStr = configs.Common_AssetsTagsConfig.Json[0];
            try
            {
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), configs.Common_AssetsTagsConfig.Json[0], true);
            }
            catch
            {
                EditorUtility.DisplayDialog("Build Bundles", "没有此平台：" + targetStr, "确定");
                return false;
            }
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                EditorUtility.DisplayDialog("Build Bundles", string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target), "确定");
                return false;
            }
            return true;
        }

        public void Apply(bool isPartOfPipeline)
        {
            //开始
            configs.CurrentConfig.Json.IsPartOfPipeline = isPartOfPipeline;
            configs.CurrentConfig.Json.Applying = true;
            configs.CurrentConfig.Save();
            //重建目录
            string tagsPath = Path.Combine(configs.LocalConfig.PlayersFolderPath, EBPUtility.GetTagStr(configs.Common_AssetsTagsConfig.Json));
            if (Directory.Exists(tagsPath))
            {
                Directory.Delete(tagsPath, true);
            }
            Directory.CreateDirectory(tagsPath);
            //Build Player
            BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), configs.Common_AssetsTagsConfig.Json[0], true);
            string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = tagsPath;
            buildPlayerOptions.target = target;
            buildPlayerOptions.options = BuildOptions.None;
            string error = BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log(error);

            //结束
            configs.CurrentConfig.Json.Applying = false;
            configs.CurrentConfig.Save();
        }
    }
}
