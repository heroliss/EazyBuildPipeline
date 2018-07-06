using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline
{
    /// <summary>
    /// EazyGame Build Pipeline Utility
    /// </summary>
    public class EBPUtility
    {
        /// <summary>
        /// 在给定的根目录(rootPath)下递归查找所有匹配搜索模式(searchPattern)的文件，返回在根目录下的相对路径，不包含文件名后缀
        /// </summary>
        /// <param name="rootPath">根目录</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <returns></returns>
        public static string[] FindFilesRelativePathWithoutExtension(string rootPath, string searchPattern = "*.json")
        {
            List<string> filePaths = new List<string>();
            List<string> fileNames = new List<string>();
            RecursiveFindFiles(rootPath, filePaths, searchPattern);
            foreach (var configPath in filePaths)
            {
                string extension = Path.GetExtension(configPath);
                fileNames.Add(configPath.Remove(configPath.Length - extension.Length, extension.Length).Remove(0, rootPath.Length + 1).Replace('\\', '/'));
            }
            return fileNames.ToArray();
        }

        private static void RecursiveFindFiles(string path, List<string> jsonPaths, string searchPattern)
        {
            jsonPaths.AddRange(Directory.GetFiles(path, searchPattern));
            foreach (var folder in Directory.GetDirectories(path))
            {
                RecursiveFindFiles(folder, jsonPaths, searchPattern);
            }
        }
    }
}
