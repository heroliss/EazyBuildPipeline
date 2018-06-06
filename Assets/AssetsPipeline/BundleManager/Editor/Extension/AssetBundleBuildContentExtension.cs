using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleManagement2
{
    partial class AssetBundleBuildContent
    {
        public void ExecuteBuild_extension(string path)
        {
            //TODO: 未来可能扩展为多option合并
            BuildAssetBundleOptions options;
            BuildTarget target = BuildTarget.NoTarget;
            try
            {
                options = (BuildAssetBundleOptions)LiXuFeng.BundleManager.Editor.Configs.configs.BundleManagerConfig.CurrentBuildAssetBundleOptionsValue;
                target = (BuildTarget)Enum.Parse(typeof(BuildTarget), LiXuFeng.BundleManager.Editor.Configs.configs.BundleManagerConfig.CurrentTags[0]);
            }
            catch(Exception e)
            {
                EditorUtility.DisplayDialog("Build Bundles", "获取参数错误：" + e.Message, "确定");
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget != target)
            {//TODO: 是否允许强制运行？
                EditorUtility.DisplayDialog("Build Bundles", string.Format("当前平台({0})与设置的平台({1})不一致，请改变设置或切换平台。", EditorUserBuildSettings.activeBuildTarget, target), "确定");
                return;
            }
            List<AssetBundleBuild> buildMaps = GetBuildMaps();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            EditorUtility.DisplayProgressBar("Build Bundles", "Starting...", 0);
            try //TODO:这里好像获取不到异常
            {
                var manifest = BuildPipeline.BuildAssetBundles(path, buildMaps.ToArray(), options, target);
            }
            catch(Exception e)
            {
                EditorUtility.DisplayDialog("Build Bundles", "创建AssetBundles时发生错误：" + e.Message, "确定");
            }
            EditorUtility.DisplayDialog("Build Bundles", "创建AssetBundles完成！", "确定");
        }
    }
}