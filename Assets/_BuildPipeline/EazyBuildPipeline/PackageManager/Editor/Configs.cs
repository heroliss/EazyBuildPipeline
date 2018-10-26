#pragma warning disable 0649
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace EazyBuildPipeline.PackageManager.Configs
{
    [Serializable]
    public class UserConfig : EBPConfig<UserConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass
        {
            public List<Package> Packages = new List<Package>(); //该项不随UI操作实时改动
            public string PackageMode;
            public string LuaSource;
            public int CompressionLevel = -1;

            [Serializable]
            public struct Package
            {
                public string PackageName;
                public string Color;
                public List<string> Bundles;
                public List<string> EmptyFolders;
                public string Necessery;
                public string DeploymentLocation;
                public bool CopyToStreaming;
            }
        }
    }

    [Serializable]
    public class ModuleConfig : Common.Configs.ModuleConfig<ModuleConfig.JsonClass>
    {
        public string BundleWorkFolderPath { get { return Path.Combine(CommonModule.CommonConfig.DataRootPath, Json.BundleWorkFolderRelativePath); } }
        public override string UserConfigsFolderPath { get { return CommonModule.CommonConfig.UserConfigsFolderPath_PackageManager; } }

        [Serializable]
        public class JsonClass : Common.Configs.ModuleConfigJsonClass
        {
            public string BundleWorkFolderRelativePath;
            public string PackageExtension;
            public bool CheckBundle;
        }
    }

    [Serializable]
    public class ModuleStateConfig : Common.Configs.ModuleStateConfig<ModuleStateConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass : Common.Configs.ModuleStateConfigJsonClass
        {
            public string CurrentAddonVersion;
        }
    }
}