using UnityEngine;
using UnityEditor;
using System;

namespace EazyBuildPipeline
{
    public class EBPEditorGUILayout
    {
        public static void TextField(string label, ref string text, Action action = null)
        {
            string text_new = EditorGUILayout.TextField(label, text);
            if (text_new != text)
            {
                text = text_new;
                if (action != null)
                {
                    action();
                }
            }
        }

        public static void Toggle(string label, ref bool value, Action action = null)
        {
            bool value_new = EditorGUILayout.Toggle(label, value);
            if (value_new != value)
            {
                value = value_new;
                if (action != null)
                {
                    action();
                }
            }
        }

        public static void EnumPopup(string label, Enum selected, Action action = null)//TODO:如何能传递属性进来??
        {
            Enum selected_new = EditorGUILayout.EnumPopup(label, selected);
            if (!selected_new.Equals(selected))
            {
                selected = selected_new;
                if (action != null)
                {
                    action();
                }
            }
        }

        public static void IntField(string label, ref int value, Action action = null)
        {
            int value_new = EditorGUILayout.IntField(label, value);
            if (value_new != value)
            {
                value = value_new;
                if (action != null)
                {
                    action();
                }
            }
        }
    }
}