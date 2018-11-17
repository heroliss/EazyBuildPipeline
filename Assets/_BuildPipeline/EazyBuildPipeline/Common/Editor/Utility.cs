using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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

        public static void RefreshAssets()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            while (EditorApplication.isCompiling)
            {
                Debug.Log("-------------------------> Compiling!");
                //System.Threading.Thread.Sleep(100);
            }
        }

        public static string Quote(string s)
        {
            return '\"' + s + '\"';
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

        #region CopyDirectory

        public static List<string> CopyDirectory(string source, string target, CopyMode copyMode, Regex directoryRegex = null, Regex fileRegex = null)
        {
            if (!Directory.Exists(source))
            {
                throw new EBPException("要拷贝的源目录不存在:" + source);
            }
            List<string> copiedFiles = new List<string>(); //仅用于返回拷贝了哪些文件(目标文件路径)
            switch (copyMode)
            {
                case CopyMode.New:
                    if (Directory.Exists(target))
                    {
                        throw new EBPException("目录已存在:" + target);
                    }
                    RecursiveCopyDirectory(source, target, copiedFiles, directoryRegex, fileRegex);
                    break;
                case CopyMode.Add:
                    RecursiveCopyDirectory(source, target, copiedFiles, directoryRegex, fileRegex, true);
                    break;
                case CopyMode.Overwrite:
                    RecursiveCopyDirectory(source, target, copiedFiles, directoryRegex, fileRegex);
                    break;
                case CopyMode.Replace:
                    if (Directory.Exists(target))
                    {
                        Directory.Delete(target, true);
                    }
                    RecursiveCopyDirectory(source, target, copiedFiles, directoryRegex, fileRegex);
                    break;
                default:
                    break;
            }
            return copiedFiles;
        }

        private static void RecursiveCopyDirectory(string source, string target, List<string> copiedFiles, Regex directoryRegex = null, Regex fileRegex = null, bool checkFileExist = false)
        {
            Directory.CreateDirectory(target);

            string folderName = null;
            foreach (string folderPath in Directory.GetDirectories(source))
            {
                folderName = Path.GetFileName(folderPath);
                if (directoryRegex == null || directoryRegex.IsMatch(folderName))
                {
                    RecursiveCopyDirectory(folderPath, Path.Combine(target, folderName), copiedFiles, directoryRegex, fileRegex, checkFileExist);
                }
            }

            string fileName = null;
            string targetFilePath = null;
            foreach (string filePath in Directory.GetFiles(source))
            {
                fileName = Path.GetFileName(filePath);
                if (fileRegex == null || fileRegex.IsMatch(fileName))
                {
                    targetFilePath = Path.Combine(target, fileName);
                    if (checkFileExist && File.Exists(targetFilePath))
                    {
                        continue;
                    }
                    File.Copy(filePath, targetFilePath, true);
                    copiedFiles.Add(targetFilePath);
                }
            }
        }

        #endregion

        public static string GetEnumDescription<TEnum>(object value)
        {
            Type enumType = typeof(TEnum);
            if (!enumType.IsEnum)
                throw new ArgumentException("不是枚举类型");
            var name = Enum.GetName(enumType, value);
            if (name == null)
                return string.Empty;
            object[] objs = enumType.GetField(name).GetCustomAttributes(typeof(EnumDescriptionAttribute), false);
            if (objs == null || objs.Length == 0)
                return string.Empty;
            EnumDescriptionAttribute attr = objs[0] as EnumDescriptionAttribute;
            return attr.Description;
        }
    }

    /// <summary>
    /// 拷贝目录的模式
    /// </summary>
    public enum CopyMode
    {
        [EnumDescription("新建目录，拷贝所有文件，若有同名根目录则抛出异常")] New,
        [EnumDescription("补全目录结构，拷贝不存在的文件")] Add,
        [EnumDescription("补全目录结构，拷贝所有文件，替换已存在的文件")] Overwrite,
        [EnumDescription("重建目录，拷贝所有文件，所有旧文件都会被删除")] Replace
    }

    public class EnumDescriptionAttribute : Attribute
    {
        public string Description { get; private set; }
        public EnumDescriptionAttribute(string description)
        {
            this.Description = description;
        }
    }
}
