﻿using System;
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
                EditorUtility.DisplayProgressBar("Build Bundles", "正在重建目录:" + path, 0.1f);
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true); //清空目录
                }
                Directory.CreateDirectory(path);
                EditorUtility.DisplayProgressBar("Build Bundles", "准备工作完成，正在创建AssetBundles...", 0.2f);

                var manifest = BuildPipeline.BuildAssetBundles(path, buildMaps.ToArray(), (BuildAssetBundleOptions)optionsValue, target);
                try //TODO:临时解决ios平台下没有主bundle文件的问题
                {
                    RenameManifest_extension(path);
                }
                catch(Exception e)
                {
                }
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
        public void RenameManifest_extension(string folderPath)
        {
            string oldName = Path.GetFileName(folderPath);
            string oldPath = Path.Combine(folderPath, oldName);
            string newPath = Path.Combine(folderPath, "assetbundlemanifest");
            File.Move(oldPath, newPath);
            File.Move(oldPath + ".manifest", newPath + ".manifest");
        }
    }
}