using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EazyBuildPipeline.AssetPreprocessor.Configs
{
    [Serializable]
    public class ModuleConfig : Common.Configs.ModuleConfig<ModuleConfig.JsonClass>
    {
        public string ShellsFolderPath { get { return Path.Combine(ModuleRootPath, Json.ShellsFolderRelativePath); } }
        public string PreStoredAssetsFolderPath { get { return Path.Combine(WorkPath, Json.PreStoredAssetsFolderRelativePath); } }
        public override string UserConfigsFolderPath { get { return CommonModule.CommonConfig.UserConfigsFolderPath_AssetPreprocessor; } }

        [Serializable]
        public class JsonClass : Common.Configs.ModuleConfigJsonClass
        {
            public string ShellsFolderRelativePath;
            public string PreStoredAssetsFolderRelativePath;
        }
    }

    [Serializable]
    public class ModuleStateConfig : Common.Configs.ModuleStateConfig<ModuleStateConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass : Common.Configs.ModuleStateConfigJsonClass
        {
        }
    }

    [Serializable]
    public class UserConfig : EBPConfig<UserConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass
        {
            public string[] Tags;
            public string CopyFileTags;
            public List<ImporterGroup> ImporterGroups = new List<ImporterGroup>();
        }

        [Serializable]
        class Box<T>
        {
            public Box(T value)
            {
                Value = value;
            }
            public Box() { }
            public T Value;
        }

        public void PushAll()
        {
            foreach (var importerGroup in Json.ImporterGroups)
            {
                foreach (var labelGroup in importerGroup.LabelGroups)
                {
                    foreach (var propertyGroup in labelGroup.PropertyGroups)
                    {
                        foreach (var property in propertyGroup.Properties)
                        {
                            foreach (var field in importerGroup.Fields)
                            {
                                if (field.Name == property.Name)
                                {
                                    PushProperty(propertyGroup, property, field);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void PushProperty(PropertyGroup propertyGroup, Property property, FieldInfo field)
        {
            var boxType = typeof(Box<>).MakeGenericType(field.FieldType);
            var valueInstance = field.GetValue(propertyGroup.SerializedObject.targetObject);
            var boxInstance = Activator.CreateInstance(boxType, valueInstance);
            property.Value = EditorJsonUtility.ToJson(boxInstance);
        }

        public void PullAll()
        {
            foreach (var importerGroup in Json.ImporterGroups)
            {
                foreach (var labelGroup in importerGroup.LabelGroups)
                {
                    foreach (var propertyGroup in labelGroup.PropertyGroups)
                    {
                        propertyGroup.ImporterSetting = (ImporterSetting)ScriptableObject.CreateInstance(importerGroup.SettingType);
                        propertyGroup.SerializedObject = new SerializedObject(propertyGroup.ImporterSetting);
                        foreach (var property in propertyGroup.Properties)
                        {
                            foreach (var field in importerGroup.Fields)
                            {
                                if (field.Name == property.Name)
                                {
                                    PullProperty(propertyGroup, property, field);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void PullProperty(PropertyGroup propertyGroup, Property property, FieldInfo field)
        {
            var boxType = typeof(Box<>).MakeGenericType(field.FieldType);
            var boxInstance = Activator.CreateInstance(boxType);
            EditorJsonUtility.FromJsonOverwrite(property.Value, boxInstance);
            object valueInstance = boxType.GetField("Value").GetValue(boxInstance);
            field.SetValue(propertyGroup.SerializedObject.targetObject, valueInstance);
        }

        public void InitImporterGroups()
        {
            Type baseType = typeof(ImporterSetting);
            foreach (Type type in baseType.Assembly.GetTypes())
            {
                if (type.IsSubclassOf(baseType))
                {
                    Type settingType = type;
                    var attribute = (SetForAttribute)settingType.GetCustomAttributes(typeof(SetForAttribute), true)[0];
                    Type targetType = attribute.TargetType;
                    string targetName = targetType.Name;
                    var importerGroup = Json.ImporterGroups.Find(x => x.Name == targetName);
                    if (importerGroup == null)
                    {
                        importerGroup = new ImporterGroup();
                        importerGroup.Name = targetName;
                        Json.ImporterGroups.Add(importerGroup);
                    }
                    importerGroup.SettingType = settingType;
                    importerGroup.TargetType = targetType;
                    importerGroup.Fields = settingType.GetFields(BindingFlags.Instance | BindingFlags.Public);
                    importerGroup.FieldNames = importerGroup.Fields.Select(x => x.Name).ToArray();
                    importerGroup.SearchFilter = attribute.SearchFilter;
                }
            };
        }
    }
}
