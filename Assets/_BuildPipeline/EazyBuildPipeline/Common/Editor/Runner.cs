using System;
using System.IO;
using EazyBuildPipeline.Common.Configs;
using UnityEditor;

namespace EazyBuildPipeline
{
    public interface IRunner
    {
        BaseModule BaseModule { get; }
        void Run(bool isPartOfPipeline = false);
        void Check(bool onlyCheckConfig = false);
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
                if (!string.IsNullOrEmpty(Module.ModuleStateConfig.JsonPath)) Module.ModuleStateConfig.Save();

                Module.Log("# PreProcess of " + Module.ModuleName + " #", true);
                PreProcess();
                Module.Log("# RunProcess of " + Module.ModuleName + " #", true);
                RunProcess();
                Module.Log("# PostProcess of " + Module.ModuleName + " #", true);
                PostProcess();
                Module.Log("## End Module " + Module.ModuleName + " ##", true);

                state.Applying = false;
                state.ErrorMessage = null;
                if (!string.IsNullOrEmpty(Module.ModuleStateConfig.JsonPath)) Module.ModuleStateConfig.Save();
            }
            catch (Exception e)
            {
                Exception e_catch = null;
                try { Catch(e); }
                catch(Exception e_in)
                {
                    Module.Log("Catch内部错误：" + e_in.ToString());
                    e_catch = e_in;
                }
                finally
                {
                    state.ErrorMessage = e.Message + (e_catch == null ? "" : "\n同时发生Catch内部错误:" + e_catch.Message);
                    state.DetailedErrorMessage = e.ToString() + (e_catch == null ? "" : "\nCatch内部异常:" + e_catch.ToString());
                    if (!string.IsNullOrEmpty(Module.ModuleStateConfig.JsonPath)) Module.ModuleStateConfig.Save();
                    Module.Log(state.DetailedErrorMessage);
                    throw new EBPException(state.DetailedErrorMessage);
                }
            }
            finally
            {
                try { Finally(); }
                catch (Exception e_in)
                {
                    Module.Log("Finally内部错误：" + e_in.ToString());
                    throw e_in;
                }
                finally
                {
                    Module.EndLog();
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        public void Check(bool onlyCheckConfig = false)
        {
            try
            {
                Module.StartLog();
                Module.Log("## Check " + Module.ModuleName + " ##", true);
                CheckProcess(onlyCheckConfig);
            }
            catch (EBPCheckFailedException e)
            {
                Module.Log("[CheckFailed] " + e.Message);
                throw e;
            }
            catch (Exception e)
            {
                Module.Log(e.ToString());
                throw e;
            }
            finally
            {
                Module.EndLog();
            }
        }

        protected virtual void CheckProcess(bool onlyCheckConfig)
        {
            //检查状态配置文件和工作目录
            if (!onlyCheckConfig)
            {
                if (!Module.StateConfigAvailable)
                {
                    throw new EBPCheckFailedException(Module.StateConfigLoadFailedMessage);
                }
                if (!Directory.Exists(Module.ModuleConfig.WorkPath)) //这个检查冗余，放在这里为保险起见
                {
                    throw new EBPCheckFailedException("工作目录不存在：" + Module.ModuleConfig.WorkPath);
                }
            }
        }

        protected virtual void PreProcess() { }
        protected abstract void RunProcess();
        protected virtual void PostProcess() { }

        protected virtual void Catch(Exception e) { }
        protected virtual void Finally() { }
    }
}