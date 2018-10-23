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
            //后处理过程：重新创建Wrap和Lua文件
            EditorUtility.DisplayProgressBar("Clear and Generate Wrap Files...", "", 1);
            ToLuaMenu.ClearWrapFilesAndCreate();
            EditorUtility.DisplayProgressBar("Clear and Generate Lua Files...", "", 1);
            LuaScriptsPreProcessor.LuaEncryptAllThingsDone(true, () => { });
            EditorUtility.DisplayProgressBar("Clear and Generate Wrap and Lua Files Finished!", "", 1);
        }
    }
}
