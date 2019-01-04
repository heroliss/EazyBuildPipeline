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
            using (new GUILayout.VerticalScope(importerGroup.Name, "flow overlay box"))
            {
                using (new GUILayout.HorizontalScope(GUILayout.MinWidth(200)))
                {
                    if (GUILayout.Button("+"))
                    {
                        //添加LabelGroup
                        var labelGroup = new LabelGroup();
                        foreach (var propertyGroup in labelGroup.PropertyGroups)
                        {
                            propertyGroup.ImporterSetting = (ImporterSetting)ScriptableObject.CreateInstance(importerGroup.SettingType);
                            propertyGroup.SerializedObject = new SerializedObject(propertyGroup.ImporterSetting);
                        }
                        importerGroup.LabelGroups.Add(labelGroup);
                        G.Module.IsDirty = true;
                    }
                    GUILayout.FlexibleSpace();
                }
                foreach (var labelGroup in importerGroup.LabelGroups)
                {
                    using (new GUILayout.VerticalScope("GroupBox"))
                    {
                        foreach (var propertyGroup in labelGroup.PropertyGroups)
                        {
                            propertyGroup.SerializedObject.Update();
                        }
                        //删除LabelGroup
                        using (new GUILayout.HorizontalScope())
                        {
                            labelGroup.Label = EditorGUILayout.TextField(labelGroup.Label);
                            if (GUILayout.Button("×", GUILayout.Width(20)))
                            {
                                importerGroup.LabelGroups.Remove(labelGroup);
                                G.Module.IsDirty = true;
                                break;
                            }
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            //添加字段
                            if (GUILayout.Button("Choose Properties", GUILayout.MaxWidth(120)))
                            {
                                GenericMenu menu = new GenericMenu();
                                foreach (string fieldName in importerGroup.FieldNames)
                                {
                                    Property property = labelGroup.CurrentPropertyGroup.Properties.Find(x => x.Name == fieldName);
                                    bool exist = property != null;
                                    menu.AddItem(new GUIContent(fieldName), exist, () =>
                                    {
                                        if (!exist)
                                        {
                                            labelGroup.CurrentPropertyGroup.Properties.Add(new Property() { Name = fieldName });
                                        }
                                        else
                                        {
                                            labelGroup.CurrentPropertyGroup.Properties.Remove(property);
                                        }
                                        G.Module.IsDirty = true;
                                    });
                                }
                                menu.ShowAsContext();
                            }

                            //切换平台
                            GUILayout.FlexibleSpace();
                            labelGroup.CurrentPropertyIndex = GUILayout.Toolbar(labelGroup.CurrentPropertyIndex, LabelGroup.PropertyGroupNames);

                        }
                        //显示字段
                        foreach (var fieldName in importerGroup.FieldNames)
                        {
                            if (labelGroup.CurrentPropertyGroup.Properties.Exists(x => x.Name == fieldName))
                            {
                                EditorGUILayout.PropertyField(labelGroup.CurrentPropertyGroup.SerializedObject.FindProperty(fieldName), true);
                            }
                        }
                        if(labelGroup.CurrentPropertyGroup.SerializedObject.ApplyModifiedProperties())
                        {
                            G.Module.IsDirty = true;
                        }
                    }
                }
            }
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
        public static string[] PropertyGroupNames = { "iPhone", "Android" };
        public int CurrentPropertyIndex; //该字段不写入json，但要序列化
        public PropertyGroup CurrentPropertyGroup { get { return PropertyGroups[CurrentPropertyIndex]; } }

        public PropertyGroup[] PropertyGroups = { new PropertyGroup() { Name = "iPhone" }, new PropertyGroup() { Name = "Android" } };
        public string Label;

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

