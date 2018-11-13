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
            ClearWrapFilesAndGenerateLuaAllAndEncrypt(Module);
        }

        public static void ClearWrapFilesAndGenerateLuaAllAndEncrypt(BaseModule Module)
        {
            //重新创建Wrap和Lua文件
            float progress = 0f;
            Module.DisplayProgressBar("Start Clear Wrap Files & Generate Lua All.", progress, true);
            var steps = new ToLuaMenu.ClearWrapFilesAndCreateSteps();
            foreach (var step in steps)
            {
                progress += 0.1f;
                Module.DisplayProgressBar(step, progress, true);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            //LuaScriptsPreProcessor.LuaEncryptAllThingsDone(true, () => { }); //下面三步代替这一步
            Module.DisplayProgressBar("Clear Lua Files...", 0.8f, true);
            LuaScriptsPreProcessor.Clean();
            Module.DisplayProgressBar("Translate Lua to ByteFile...", 0.85f, true);
            LuaScriptsPreProcessor.DoByteCodeTranslationJob(true);
            Module.DisplayProgressBar("Encrypt Lua ByteFile...", 0.9f, true);
            LuaScriptsPreProcessor.DoTheEncryptionJob();

            Module.DisplayProgressBar("Clear Wrap Files & Generate Lua & Encrypt All Finished!", 1, true);
        }
    }
}
