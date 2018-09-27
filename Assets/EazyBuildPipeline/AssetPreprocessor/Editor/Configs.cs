using System;
using System.IO;
using System.Collections.Generic;

namespace EazyBuildPipeline.AssetPreprocessor.Configs
{
    [Serializable]
    public class ModuleConfig : Common.Configs.ModuleConfig<ModuleConfig.JsonClass>
    {
        //本地配置路径
        public string OptionsEnumConfigPath { get { return Path.Combine(ModuleRootPath, Json.OptionsEnumConfigRelativePath); } }
        public string ShellsFolderPath { get { return Path.Combine(ModuleRootPath, Json.ShellsFolderRelativePath); } }
        //Pipeline配置路径
        public string PreStoredAssetsFolderPath { get { return Path.Combine(WorkPath, Json.PreStoredAssetsFolderRelativePath); } }
        public string LogsFolderPath { get { return Path.Combine(WorkPath, Json.LogsFolderRelativePath); } }
        [Serializable]
        public class JsonClass : Common.Configs.ModuleConfigJsonClass
        {
            public string OptionsEnumConfigRelativePath;
            public string ShellsFolderRelativePath;
            public string PreStoredAssetsFolderRelativePath;
            public string LogsFolderRelativePath;
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
        public class Group { public string FullGroupName; public List<string> Options; }
        [Serializable]
        public class JsonClass
        {
            public List<Group> Groups = new List<Group>();
            public string[] Tags = new string[0];
        }
    }

    [Serializable]
    public class OptionsEnumConfig : EBPConfig<List<OptionsEnumConfig.Group>>
    {
        public OptionsEnumConfig()
        {
            Json = new List<Group>
            {
                 new Group
                 {
                     FullGroupName = "Title1/Title2/Example Group Name",
                     MultiSelect = true,
                     Options = new List<string>{ "Example Option 1","Example Option 2" ,"Example Option 3"},
                     Platform = new []{"android" }
                 },
                 new Group
                 {
                     FullGroupName = "Title1/Title3/Example Group Name 2",
                     MultiSelect = false,
                     Options =  new List<string>{ "Example Option 1","Example Option 2" ,"Example Option 3"},
                     Platform = new []{"android","ios" }
                 }
            };
        }
        [Serializable]
        public class Group
        {
            public string FullGroupName;
            public List<string> Options;
            public bool MultiSelect;
            public string[] Platform;
        }
    }
}
