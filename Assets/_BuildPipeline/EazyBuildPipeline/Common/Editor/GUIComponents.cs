using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace EazyBuildPipeline
{
    public class EBPEditorGUILayout
    {
        static GUIStyle errorStyle = new GUIStyle(EditorStyles.label);
        static GUILayoutOption[] miniButtonOptions = { GUILayout.MaxHeight(18), GUILayout.MaxWidth(22) };
        public static void RootSettingLine(BaseModule module, Action<string> ChangeRootPath)
        {
            bool rootAvailable = module.RootAvailable;
            errorStyle.normal.textColor = Color.red; //TODO: 暂时把初始化放在这里
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Root:", rootAvailable ? null : module.StateConfigLoadFailedMessage),
                rootAvailable ? EditorStyles.label : errorStyle, GUILayout.Width(45));

            string path = EditorGUILayout.DelayedTextField(CommonModule.CommonConfig.Json.PipelineRootPath);
            if (GUILayout.Button("...", miniButtonOptions))
            {
                path = EditorUtility.OpenFolderPanel("打开根目录", CommonModule.CommonConfig.Json.PipelineRootPath, null);
            }
            if (!string.IsNullOrEmpty(path) && path != CommonModule.CommonConfig.Json.PipelineRootPath)
            {
                if (!Directory.Exists(path))
                {
                    module.DisplayDialog("目录不存在:" + path);
                    return;
                }
                bool ensure = true;
                if (module.IsDirty)
                {
                    ensure = EditorUtility.DisplayDialog("改变根目录", "更改未保存，是否要放弃更改？", "放弃保存", "返回");
                }
                if (ensure)
                {
                    ChangeRootPath(path);
                }
                return;
            }
            EditorGUILayout.EndHorizontal();
        }

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

        public static Enum EnumPopup(string label, Enum selected, Action action = null) //TODO:如何用ref传递枚举?
        {
            Enum selected_new = EditorGUILayout.EnumPopup(label, selected);
            if (!selected_new.Equals(selected))
            {
                //selected = selected_new;
                if (action != null)
                {
                    action();
                }
            }
            return selected_new;
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