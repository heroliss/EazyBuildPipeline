using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EazyBuildPipeline
{
    public static class ArrayExtension
    {
        /// <summary>
        /// 查找字符串在数组中的索引，若没有则返回-1
        /// </summary>
        public static int IndexOf(this string[] array, string s)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (s == array[i])
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public static class StringExtension
    {
        /// <summary>
        /// 返回路径字符串去掉后缀之后的字符串
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string RemoveExtension(this string path)
        {
            int len = Path.GetExtension(path).Length;
            return path.Remove(path.Length - len, len);
        }
    }
}
