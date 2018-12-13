using UnityEngine;
using UnityEditor;

namespace EazyBuildPipeline.PackageManager
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
            EBPUtility.RefreshAssets();

            Module.DisplayProgressBar("-- Start Handle Lua Files --", 0f, true);
            HandleLuaFiles(0f, 1f);
            Module.DisplayProgressBar("-- End Handle Lua Files --", 1f, true);

            EBPUtility.RefreshAssets();
        }

        protected override void PostProcess()
        {
            EBPUtility.RefreshAssets();
        }

        private void HandleLuaFiles(float startProgress, float endProgress)
        {
            float progressLength = endProgress - startProgress;
            //LuaScriptsPreProcessor.LuaEncryptAllThingsDone(true, () => { }); //下面三步代替这一步
            Module.DisplayProgressBar("Clear Lua Files...", startProgress + progressLength * 0f, true);
            LuaScriptsPreProcessor.Clean();
            Module.DisplayProgressBar("Translate Lua to ByteFile...", startProgress + progressLength * 0.2f, true);
            LuaScriptsPreProcessor.DoByteCodeTranslationJob(true);
            Module.DisplayProgressBar("Encrypt Lua ByteFile...", startProgress + progressLength * 0.4f, true);
            LuaScriptsPreProcessor.DoTheEncryptionJob();
            Module.DisplayProgressBar("Lua Clear & ToByte & Encrypt All Finish!", startProgress + progressLength * 1f, true);
        }
    }
}