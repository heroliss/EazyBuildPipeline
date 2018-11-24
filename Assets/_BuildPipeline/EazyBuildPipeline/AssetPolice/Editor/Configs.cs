using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EazyBuildPipeline.AssetPolice.Editor
{
    [Serializable] public class StringList : List<string> { }
    [Serializable] public class BundleIDDictionary : SerializableDictionary<string, StringList> { }
    [Serializable]
    public class Configs : EBPConfig<Configs.JsonClass>
    {
        public BundleIDDictionary AllBundles = new BundleIDDictionary();
        public string ResultFilePath { get { return Path.Combine(Json.OutputPath, Json.ResultFileName); } }
        [Serializable]
        public class JsonClass
        {
            public string AssetsDirectories;
            public string DependenceFilterDirectories;
            public string ExcludeSubStringWhenFind;
            public string ExcludeExtensionsWhenBuildBundles;
            public string OutputPath;
            public string Separator = " --";
            public string ResultFileName = "InverseDependenceMap.json";
        }
    }
}
