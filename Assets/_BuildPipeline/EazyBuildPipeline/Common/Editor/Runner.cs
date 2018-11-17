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
                Module.LogHead("Start Module " + Module.ModuleName, 4);

                state.IsPartOfPipeline = isPartOfPipeline;
                state.Applying = true;
                state.ErrorMessage = "Unexpected Halt!";
                state.DetailedErrorMessage = null;
                if (!string.IsNullOrEmpty(Module.ModuleStateConfig.JsonPath)) Module.ModuleStateConfig.Save();

                Module.LogHead("PreProcess of " + Module.ModuleName, 3);
                PreProcess();
                Module.LogHead("RunProcess of " + Module.ModuleName, 3);
                RunProcess();
                Module.LogHead("PostProcess of " + Module.ModuleName, 3);
                PostProcess();

                state.Applying = false;
                state.ErrorMessage = null;
                if (!string.IsNullOrEmpty(Module.ModuleStateConfig.JsonPath)) Module.ModuleStateConfig.Save();
            }
            catch (Exception e)
            {
                Exception e_catch = null;
                try
                {
                    Module.LogHead("Enter Catch", 3);
                    Catch(e);
                    Module.LogHead("Exit Catch", 3);
                }
                catch (Exception e_in)
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
                try
                {
                    Module.LogHead("Enter Finally", 3);
                    Finally();
                    Module.LogHead("Exit Finally", 3);
                }
                catch (Exception e_in)
                {
                    Module.Log("Finally内部错误：" + e_in.ToString());
                    throw e_in;
                }
                finally
                {
                    Module.LogHead("End Module " + Module.ModuleName, 4);
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
                Module.LogHead("Check " + Module.ModuleName, 4);
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
            if (!onlyCheckConfig)
            {
                //检查状态配置文件
                if (!Module.StateConfigAvailable)
                {
                    throw new EBPCheckFailedException(Module.StateConfigLoadFailedMessage);
                }
                //检查工作目录
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