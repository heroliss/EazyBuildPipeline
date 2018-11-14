using UnityEngine;
using UnityEditor;

namespace EazyBuildPipeline.PackageManager
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
            EBPUtility.RefreshAssets();
        }

        protected override void PostProcess()
        {
            EBPUtility.RefreshAssets();
        }
    }
}