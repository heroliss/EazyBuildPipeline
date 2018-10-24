using System;
using System.IO;
using EazyBuildPipeline.Common.Configs;
using UnityEditor;

namespace EazyBuildPipeline
{
    public interface IRunner
    {
        void Run(bool isPartOfPipeline = false);
        bool Check(bool onlyCheckConfig = false);
        BaseModule BaseModule { get; }
    }

    public abstract class EBPRunner<TModule, TModuleConfig, TModuleConfigJsonClass, TModuleStateConfig, TModuleStateConfigJsonClass> : IRunner
        where TModule : EBPModule<TModuleConfig, TModuleConfigJsonClass, TModuleStateConfig, TModuleStateConfigJsonClass>
        where TModuleConfig : ModuleConfig<TModuleConfigJsonClass>, new()
        where TModuleConfigJsonClass : ModuleConfigJsonClass, new()
        where TModuleStateConfig : ModuleStateConfig<TModuleStateConfigJsonClass>, new()
        where TModuleStateConfigJsonClass : ModuleStateConfigJsonClass, new()
    {
        public BaseModule BaseModule { get { return Module; } }
        public TModule Module;

        public EBPRunner(TModule module)
        {
            Module = module;
        }

        public void Run(bool isPartOfPipeline = false)
        {
            var state = Module.ModuleStateConfig.Json;
            try
            {
                Module.StartLog();
                Module.Log("## Start Module " + Module.ModuleName + " ##", true);
                state.IsPartOfPipeline = isPartOfPipeline;
                state.Applying = true;
                state.ErrorMessage = "Unexpected Halt!";
                state.DetailedErrorMessage = null;
                Module.ModuleStateConfig.Save();
                Module.Log("# Start PreProcess of " + Module.ModuleName + " #", true);
                PreProcess();
                Module.Log("# Start RunProcess of " + Module.ModuleName + " #", true);
                RunProcess();
                Module.Log("# Start PostProcess of " + Module.ModuleName + " #", true);
                PostProcess();
                Module.Log("## End Module " + Module.ModuleName + " ##", true);
                state.Applying = false;
                state.ErrorMessage = null;
                Module.ModuleStateConfig.Save();
                Module.Log("## Refresh Assets ##", true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch (Exception e)
            {
                Module.Log("[Error] " + e.ToString(), true);
                state.ErrorMessage = e.Message;
                state.DetailedErrorMessage = e.ToString();
                Module.ModuleStateConfig.Save();
                throw e;
            }
            finally
            {
                Module.EndLog();
            }
        }

        public bool Check(bool onlyCheckConfig = false)
        {
            try
            {
                Module.StartLog();
                Module.Log("## Start Check " + Module.ModuleName + " ##", true);
                if (CheckProcess(onlyCheckConfig))
                {
                    Module.Log("## End Check " + Module.ModuleName + " (Success) ##", true);
                    return true;
                }
                else
                {
                    Module.Log("## End Check " + Module.ModuleName + " (Failed) ##", true); //TODO:这个分支应该不会出现
                    return false;
                }
            }
            catch (EBPCheckFailedException e)
            {
                Module.Log("[CheckFailed] " + e.Message, true);
                throw e;
            }
            catch (Exception e)
            {
                Module.Log("[Error] " + e.ToString(), true);
                throw e;
            }
            finally
            {
                Module.EndLog();
            }
        }

        protected virtual bool CheckProcess(bool onlyCheckConfig)
        {
            //检查状态配置文件和工作目录
            if (!onlyCheckConfig)
            {
                if (!Module.RootAvailable)
                {
                    DisplayDialogOrThrowCheckFailedException(Module.StateConfigLoadFailedMessage);
                    return false;
                }
                if (!Directory.Exists(Module.ModuleConfig.WorkPath)) //这个检查冗余，放在这里为保险起见
                {
                    DisplayDialogOrThrowCheckFailedException("工作目录不存在：" + Module.ModuleConfig.WorkPath);
                    return false;
                }
            }
            return true;
        }

        protected abstract void PreProcess();
        protected abstract void RunProcess();
        protected abstract void PostProcess();

        protected void DisplayDialogOrThrowCheckFailedException(string text)
        {
            if (CommonModule.CommonConfig.IsBatchMode) //HACK: Application.isBatchMode(for Unity 2018.3+)
            {
                throw new EBPCheckFailedException(text);
            }
            else
            {
                Module.DisplayDialog(text);
            }
        }
    }
}