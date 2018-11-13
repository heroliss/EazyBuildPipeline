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
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            //LuaScriptsPreProcessor.LuaEncryptAllThingsDone(true, () => { }); //下面三步代替这一步
            Module.DisplayProgressBar("Clear Lua Files...", 0, true);
            LuaScriptsPreProcessor.Clean();
            Module.DisplayProgressBar("Translate Lua to ByteFile...", 0.1f, true);
            LuaScriptsPreProcessor.DoByteCodeTranslationJob();
            Module.DisplayProgressBar("Encrypt Lua ByteFile...", 0.3f, true);
            LuaScriptsPreProcessor.DoTheEncryptionJob();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            //重新创建Wrap和Lua文件
            Module.DisplayProgressBar("Clear and Generate Wrap Files...", 0.5f, true);
            ToLuaMenu.ClearAndGen();

            Module.DisplayProgressBar("Clear and Generate Wrap and Lua Files Finished!", 1, true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
