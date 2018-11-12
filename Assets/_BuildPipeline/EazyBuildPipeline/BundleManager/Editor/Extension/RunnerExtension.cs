using UnityEngine;
using UnityEditor;

namespace EazyBuildPipeline.BundleManager
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        protected override void PostProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}