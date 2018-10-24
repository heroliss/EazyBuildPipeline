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
        public static string GetTagStr(string[] tags)
        {
            string s = null;
            for (int i = 0; i < tags.Length; i++)
            {
                s += "[" + tags[i] + "]";
            }
            return s;
        }

        /// <summary>
        /// 在给定的根目录(rootPath)下递归查找所有匹配搜索模式(searchPattern)的文件，返回在根目录下的相对路径，不包含文件名后缀
        /// </summary>
        /// <param name="rootPath">根目录</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <returns></returns>
        public static string[] FindFilesRelativePathWithoutExtension(string rootPath, string searchPattern = "*.json")
        {
            if (!Directory.Exists(rootPath))
            {
                return new string[0];
            }
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

        public static void HandleApplyingWarning(BaseModule module)
        {
            var stateConfig = module.BaseModuleStateConfig;
            if (stateConfig.BaseJson.Applying)
            {
                if (EditorUtility.DisplayDialog(module.ModuleName, "上次运行时发生错误：" + stateConfig.BaseJson.ErrorMessage, "详细信息", "确定"))
                {
                    System.Diagnostics.Process.Start(stateConfig.JsonPath);
                }
            }
        }

        //获取命令行参数
        public static string GetArgValue(string argName)
        {
            int index = CommonModule.CommonConfig.Args_lower.IndexOf("--" + argName.ToLower());
            if (index < 0 || index == CommonModule.CommonConfig.Args.Count - 1)
            {
                return null;
            }
            return CommonModule.CommonConfig.Args[index + 1];
        }
        public static string GetArgValueLower(string argName)
        {
            int index = CommonModule.CommonConfig.Args_lower.IndexOf("--" + argName.ToLower());
            if (index < 0 || index == CommonModule.CommonConfig.Args.Count - 1)
            {
                return null;
            }
            return CommonModule.CommonConfig.Args_lower[index + 1];
        }
        public static List<string> GetArgValues(string argName)
        {
            List<string> values = new List<string>();
            int i = 0;
            while (true)
            {
                i = CommonModule.CommonConfig.Args_lower.IndexOf("--" + argName.ToLower(), i);
                if (i < 0)
                {
                    break;
                }
                else
                {
                    i++;
                    values.Add(CommonModule.CommonConfig.Args[i]);
                }
            }
            return values;
        }
        public static List<string> GetArgValuesLower(string argName)
        {
            List<string> values = new List<string>();
            int i = 0;
            while (true)
            {
                i = CommonModule.CommonConfig.Args_lower.IndexOf("--" + argName.ToLower(), i);
                if (i < 0)
                {
                    break;
                }
                else
                {
                    i++;
                    values.Add(CommonModule.CommonConfig.Args_lower[i]);
                }
            }
            return values;
        }
    }
}
