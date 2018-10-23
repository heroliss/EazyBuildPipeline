using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline
{
    public static class ArrayExtension
    {
        /// <summary>
        /// 查找字符串在数组中的索引，若没有则返回-1
        /// </summary>
        public static int IndexOf<T>(this T[] array, T s)
        {
            if (array == null || s == null)
            {
                return -1;
            }
            for (int i = 0; i < array.Length; i++)
            {
                if (s.Equals(array[i]))
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
            if (path == null)
            {
                return null;
            }
            int len = Path.GetExtension(path).Length;
            return path.Remove(path.Length - len, len);
        }

        public static T ToEnum<T>(this string s)
        {
            try
            {
               return (T)Enum.Parse(typeof(T), s);
            }
            catch
            {
                return default(T);
            }
        }
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> _keys = new List<TKey>();
        [SerializeField]
        private List<TValue> _values = new List<TValue>();

        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            _keys.Capacity = this.Count;
            _values.Capacity = this.Count;
            foreach (var kvp in this)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();
            int count = Mathf.Min(_keys.Count, _values.Count);
            for (int i = 0; i < count; ++i)
            {
                this.Add(_keys[i], _values[i]);
            }
        }

        public SerializableDictionary<TKey,TValue> CopyFrom(Dictionary<TKey,TValue> dictionary)
        {
            this.Clear();
            foreach (var item in dictionary)
            {
                this.Add(item.Key, item.Value);
            }
            return this;
        }
    }
}
