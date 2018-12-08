using System;
using UnityEditor;

namespace EazyBuildPipeline.SVNUpdate
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
            Module.DisplayProgressBar("Clear Wrap Files...", 0, true);
            ToLuaMenu.ClearLuaWraps();
            Module.DisplayProgressBar("Clear Lua Files...", 0.2f, true);
            LuaScriptsPreProcessor.Clean();

            EBPUtility.RefreshAssets();
        }

        protected override void PostProcess()
        {
            EBPUtility.RefreshAssets();
        }
    }
}
