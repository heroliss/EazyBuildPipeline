using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EazyBuildPipeline.AssetManager.Editor
{
    [Serializable]
    public class Configs : EBPConfig<Configs.JsonClass>
    {
        public List<string> NoReferenceAssetList = new List<string>();
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
            public string ResultFileName = "NoReferenceAssets.txt";
        }
    }
}
