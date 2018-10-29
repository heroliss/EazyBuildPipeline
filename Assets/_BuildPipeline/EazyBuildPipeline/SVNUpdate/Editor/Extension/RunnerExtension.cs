using System;
using UnityEditor;

namespace EazyBuildPipeline.SVNUpdate
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
            //删除lua的生成文件
            EditorUtility.DisplayProgressBar("Clear Wrap Files...", "", 0);
            AssetDatabase.DeleteAsset("Assets/ToLua/ToLua/Source/Generate");
        }

        protected override void PostProcess()
        {
            //重新创建Wrap和Lua文件
            EditorUtility.DisplayProgressBar("Clear and Generate Wrap Files...", "", 0.2f);
            ToLuaMenu.ClearWrapFilesAndCreate();

            //LuaScriptsPreProcessor.LuaEncryptAllThingsDone(true, () => { }); //下面三步代替这一步
            EditorUtility.DisplayProgressBar("Clear Lua Files...", "", 0.4f);
            LuaScriptsPreProcessor.Clean();
            EditorUtility.DisplayProgressBar("Translate Lua to ByteFile...", "", 0.6f);
            LuaScriptsPreProcessor.DoByteCodeTranslationJob();
            EditorUtility.DisplayProgressBar("Encrypt Lua ByteFile...", "", 0.8f);
            LuaScriptsPreProcessor.DoTheEncryptionJob();

            EditorUtility.DisplayProgressBar("Clear and Generate Wrap and Lua Files Finished!", "", 1);
        }
    }
}
