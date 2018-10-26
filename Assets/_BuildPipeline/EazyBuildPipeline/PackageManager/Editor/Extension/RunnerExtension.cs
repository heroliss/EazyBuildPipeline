using UnityEngine;
using UnityEditor;

namespace EazyBuildPipeline.PackageManager
{
    public partial class Runner
    {
        protected override void PostProcess()
        {
        }

        protected override void PreProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}