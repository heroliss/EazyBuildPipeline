using System;
using System.IO;
using EazyBuildPipeline.Common.Configs;
using UnityEditor;

namespace EazyBuildPipeline
{
    public interface IRunner
    {
        void Run(bool isPartOfPipeline = false);
        void Check(bool onlyCheckConfig = false);
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
                CreateLogFolder();
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
                CommonModule.CommonConfig.CurrentLogFolderPath = null;
                Module.Log(e.ToString());
                state.ErrorMessage = e.Message;
                state.DetailedErrorMessage = e.ToString();
                if (!string.IsNullOrEmpty(Module.ModuleStateConfig.JsonPath)) Module.ModuleStateConfig.Save();
                throw e;
            }
            finally
            {
                Module.EndLog();
                EditorUtility.ClearProgressBar();
                if (!isPartOfPipeline) //若为管线一部分时，让所有模块使用相同的CurrentLogFolderPath
                {
                    CommonModule.CommonConfig.CurrentLogFolderPath = null; //赋值为null，会让下次Run时产生新的目录名（根据时间）
                }
            }
        }

        private static void CreateLogFolder()
        {
            if (CommonModule.CommonConfig.CurrentLogFolderPath == null)
            {
                CommonModule.CommonConfig.CurrentLogFolderPath = Path.Combine(CommonModule.CommonConfig.LogsRootPath, DateTime.Now.ToString("[yyyyMMddHHmmss]"));
                Directory.CreateDirectory(CommonModule.CommonConfig.CurrentLogFolderPath);
            }
        }

        public void Check(bool onlyCheckConfig = false)
        {
            try
            {
                if (!onlyCheckConfig) //这个变量说明是否是Run前检查，若是则记录日志，否则不记录日志
                {
                    CreateLogFolder();
                    Module.StartLog();
                    Module.Log("## Check " + Module.ModuleName + " ##", true);
                }
                CheckProcess(onlyCheckConfig);
            }
            catch (EBPCheckFailedException e)
            {
                if (!onlyCheckConfig)
                {
                    CommonModule.CommonConfig.CurrentLogFolderPath = null;
                    Module.Log("[CheckFailed] " + e.Message);
                }
                throw e;
            }
            catch (Exception e)
            {
                if (!onlyCheckConfig)
                {
                    CommonModule.CommonConfig.CurrentLogFolderPath = null;
                    Module.Log(e.ToString());
                }
                throw e;
            }
            finally
            {
                if (!onlyCheckConfig)
                {
                    Module.EndLog();
                }
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

        protected abstract void PreProcess();
        protected abstract void RunProcess();
        protected abstract void PostProcess();
    }
}