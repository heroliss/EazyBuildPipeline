using System;
using UnityEditor;

namespace EazyBuildPipeline.SVNUpdate
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Module.DisplayProgressBar("Clear Wrap Files...", 0, true);
            AssetDatabase.DeleteAsset("Assets/ToLua/ToLua/Source/Generate");
        }

        protected override void PostProcess()
        {
            Module.DisplayProgressBar("Clear Wrap Files...", 0, true);
            AssetDatabase.DeleteAsset("Assets/ToLua/ToLua/Source/Generate");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
