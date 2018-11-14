using System;
using UnityEditor;

namespace EazyBuildPipeline.SVNUpdate
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
        }

        protected override void PostProcess()
        {
            Module.DisplayProgressBar("Clear Wrap Files...", 0, true);
            ToLuaMenu.ClearLuaWraps();
        }
    }
}
