using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleManagement2
{
    partial class AssetBundleBuildContent
    {
        public void ExecuteBuild_extension(BuildTarget target, int optionsValue, string path)
        {
            //开始创建Bundles
            try
            {
                EditorUtility.DisplayProgressBar("Build Bundles", "Starting...", 0);
                List<AssetBundleBuild> buildMaps = GetBuildMaps();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var manifest = BuildPipeline.BuildAssetBundles(path, buildMaps.ToArray(), (BuildAssetBundleOptions)optionsValue, target);
                EditorUtility.DisplayDialog("Build Bundles", "创建AssetBundles完成！", "确定");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Build Bundles", "创建AssetBundles时发生错误：" + e.Message, "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}