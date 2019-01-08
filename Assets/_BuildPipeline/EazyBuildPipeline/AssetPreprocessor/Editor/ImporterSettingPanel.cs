using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.AssetPreprocessor.Editor
{
    class ImporterSettingPanel
    {
        EditorWindow window;
        List<ImporterGroup> importerGroups { get { return G.Module.UserConfig.Json.ImporterGroups; } }

        public void OnGUI()
        {
            using (new GUILayout.HorizontalScope())
            {
                foreach (var importerGroup in importerGroups)
                {
                    ImporterGroupPanel(importerGroup);
                }
            }
        }

        private void ImporterGroupPanel(ImporterGroup importerGroup)
        {
            using (new GUILayout.VerticalScope(GUILayout.MaxWidth(100000)))
            {
                //头部
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        AddLabelGroup(importerGroup);
                    }
                    GUILayout.Label(importerGroup.Name, "BoldLabel");
                }
                //每一个LabelGroup
                foreach (var labelGroup in importerGroup.LabelGroups)
                {
                    if (LabelGroupPanel(importerGroup, labelGroup))//返回true表示发生了删除操作
                    {
                        break;
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        private bool LabelGroupPanel(ImporterGroup importerGroup, LabelGroup labelGroup)
        {
            using (new GUILayout.VerticalScope("GroupBox"))
            {
                //头部
                using (new GUILayout.HorizontalScope())
                {
                    labelGroup.Active = EditorGUILayout.ToggleLeft(GUIContent.none, labelGroup.Active, GUILayout.Width(20));
                    labelGroup.LabelExpression = EditorGUILayout.TextField(labelGroup.LabelExpression);
                    if (GUILayout.Button("×", GUILayout.Width(20))) //删除LabelGroup
                    {
                        RemoveLabelGroup(importerGroup, labelGroup);
                        return true;
                    }
                }
                using (new EditorGUI.DisabledGroupScope(!labelGroup.Active))
                {
                    //每一个属性
                    using (new GUILayout.HorizontalScope())
                    {
                        foreach (var propertyGroup in labelGroup.PropertyGroups)
                        {
                            PropertyGroupPanel(importerGroup, propertyGroup);
                        }
                    }
                }
            }
            return false;
        }

        private void PropertyGroupPanel(ImporterGroup importerGroup, PropertyGroup propertyGroup)
        {
            propertyGroup.SerializedObject.Update();
            using (new GUILayout.VerticalScope("CN Box", GUILayout.MaxWidth(100000)))
            {
                //头部
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(propertyGroup.Name, "BoldLabel");
                    GUILayout.FlexibleSpace();
                    //选择字段
                    if (GUILayout.Button("Choose Properties", GUILayout.MaxWidth(120)))
                    {
                        ShowChoosePropertiesMenu(importerGroup, propertyGroup);
                    }
                }
                EditorGUILayout.Separator();
                //显示字段
                foreach (var fieldName in importerGroup.FieldNames)
                {
                    if (propertyGroup.Properties.Exists(x => x.Name == fieldName))
                    {
                        EditorGUILayout.PropertyField(propertyGroup.SerializedObject.FindProperty(fieldName), true);
                    }
                }
                //应用修改
                if (propertyGroup.SerializedObject.ApplyModifiedProperties())
                {
                    G.Module.IsDirty = true;
                }
            }
        }

        private void RemoveLabelGroup(ImporterGroup importerGroup, LabelGroup labelGroup)
        {
            if (labelGroup.PropertyGroups.All(x => x.Properties.Count == 0) || G.Module.DisplayDialog("确定删除该组？\n\n" + labelGroup.LabelExpression, "确定", "取消"))
            {
                importerGroup.LabelGroups.Remove(labelGroup);
                G.Module.IsDirty = true;
                EditorGUIUtility.editingTextField = false;
            }
        }

        private void AddLabelGroup(ImporterGroup importerGroup)
        {
            var labelGroup = new LabelGroup();
            foreach (var propertyGroup in labelGroup.PropertyGroups)
            {
                propertyGroup.ImporterSetting = (ImporterSetting)ScriptableObject.CreateInstance(importerGroup.SettingType);
                propertyGroup.SerializedObject = new SerializedObject(propertyGroup.ImporterSetting);
            }
            importerGroup.LabelGroups.Add(labelGroup);
            G.Module.IsDirty = true;
            EditorGUIUtility.editingTextField = false;
        }

        private void ShowChoosePropertiesMenu(ImporterGroup importerGroup, PropertyGroup propertyGroup)
        {
            GenericMenu menu = new GenericMenu();
            foreach (string fieldName in importerGroup.FieldNames)
            {
                Property property = propertyGroup.Properties.Find(x => x.Name == fieldName);
                bool exist = property != null;
                menu.AddItem(new GUIContent(fieldName), exist, () =>
                {
                    if (!exist)
                    {
                        propertyGroup.Properties.Add(new Property() { Name = fieldName });
                    }
                    else
                    {
                        propertyGroup.Properties.Remove(property);
                    }
                    G.Module.IsDirty = true;
                });
            }
            menu.ShowAsContext();
        }

        public void Awake()
        {
        }

        public void OnEnable(EditorWindow window)
        {
            this.window = window;
            G.Module.UserConfig.InitImporterGroups();
            G.Module.UserConfig.PullAll();
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            G.Module.UserConfig.PushAll();
        }

        private void OnUndoRedo()
        {
            window.Repaint();
        }
    }
}

/// <summary>
/// Config和UI共同需要的数据结构
/// </summary>
namespace EazyBuildPipeline.AssetPreprocessor
{
    [Serializable]
    public class ImporterGroup
    {
        [NonSerialized] public Type SettingType;
        [NonSerialized] public Type TargetType;
        [NonSerialized] public string[] FieldNames;
        [NonSerialized] public FieldInfo[] Fields;
        [NonSerialized] public string SearchFilter;
        public string Name;
        public List<LabelGroup> LabelGroups = new List<LabelGroup>();
    }
    [Serializable]
    public class LabelGroup
    {
        public bool Active = true;
        public string LabelExpression;
        public PropertyGroup[] PropertyGroups = { new PropertyGroup() { Name = "iPhone" }, new PropertyGroup() { Name = "Android" } };

        public bool SetPropertyGroups(string assetPath)
        {
            bool dirty = false;
            var importer = AssetImporter.GetAtPath(assetPath);
            foreach (var propertyGroup in PropertyGroups) //遍历 iPhone、Android 属性组
            {
                dirty |= propertyGroup.ImporterSetting.Set(importer, propertyGroup.Properties.Select(x => x.Name), propertyGroup.Name);
            }
            if (dirty)
            {
                importer.SaveAndReimport();
            }
            return dirty;
        }
    }

    [Serializable]
    public class PropertyGroup
    {
        [NonSerialized] public SerializedObject SerializedObject;
        [NonSerialized] public ImporterSetting ImporterSetting;
        public string Name;
        public List<Property> Properties = new List<Property>();
    }

    [Serializable]
    public class Property
    {
        public string Name;
        public string Value;
    }

    public abstract class ImporterSetting : ScriptableObject
    {
        public abstract bool Set(AssetImporter importer, IEnumerable<string> properties, string platform);
    }

    [System.AttributeUsage(AttributeTargets.Class)]
    sealed class SetForAttribute : Attribute
    {
        readonly Type targetType;
        readonly string searchFilter;
        public SetForAttribute(Type targetType, string searchFilter)
        {
            this.targetType = targetType;
            this.searchFilter = searchFilter;
        }
        public Type TargetType { get { return targetType; } }
        public string SearchFilter { get { return searchFilter; } }
    }
}

