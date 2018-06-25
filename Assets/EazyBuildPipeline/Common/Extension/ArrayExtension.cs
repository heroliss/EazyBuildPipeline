using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LiXuFeng
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
}
