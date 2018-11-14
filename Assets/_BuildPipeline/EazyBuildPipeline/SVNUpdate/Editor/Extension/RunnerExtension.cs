using System;
using UnityEditor;

namespace EazyBuildPipeline.SVNUpdate
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
            EBPUtility.RefreshAssets();

            Module.DisplayProgressBar("Clear Wrap Files...", 0, true);
            AssetDatabase.DeleteAsset("Assets/ToLua/ToLua/Source/Generate");
        }

        protected override void PostProcess()
        {
            Module.DisplayProgressBar("Clear Wrap Files...", 0, true);
            AssetDatabase.DeleteAsset("Assets/ToLua/ToLua/Source/Generate");

            EBPUtility.RefreshAssets();
        }
    }
}
