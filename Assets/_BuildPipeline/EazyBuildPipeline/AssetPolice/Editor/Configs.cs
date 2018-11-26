using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EazyBuildPipeline.AssetPolice.Editor
{
    /// <summary>
    /// Bundle Reverse Dependence Item
    /// </summary>
    [Serializable]
    public class BundleRDItem
    {
        public List<string> RDBundles = new List<string>();
        public bool IsRDRoot;
    }
    [Serializable] public class BundleRDDictionary : SerializableDictionary<string, BundleRDItem> { }
    [Serializable]
    public class ModuleConfig : EBPConfig<ModuleConfig.JsonClass>
    {
        public BundleRDDictionary AllBundles = new BundleRDDictionary();
        [Serializable]
        public class JsonClass
        {
            public string AssetsDirectories;
            public string DependenceFilterDirectories;
            public string ExcludeSubStringWhenFind;
            public string ExcludeExtensionsWhenBuildBundles;
            public string OutputPath;
            public string Separator = " --";
            public int InitialLeftWidth = 400;
            public string StateConfigName = "StateConfig.json";
        }
    }

    [Serializable]
    public class StateConfig : EBPConfig<StateConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass
        {
            public string CurrentMapFilePath;
        }
    }
}
