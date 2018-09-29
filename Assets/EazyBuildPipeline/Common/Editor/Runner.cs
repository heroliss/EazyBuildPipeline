using System;
using EazyBuildPipeline.Common.Configs;
using UnityEditor;

namespace EazyBuildPipeline
{
    public abstract class EBPRunner<TModule, TModuleConfig, TModuleConfigJsonClass, TModuleStateConfig, TModuleStateConfigJsonClass>
        where TModule : EBPModule<TModuleConfig, TModuleConfigJsonClass, TModuleStateConfig, TModuleStateConfigJsonClass>
        where TModuleConfig : ModuleConfig<TModuleConfigJsonClass>, new()
        where TModuleConfigJsonClass : ModuleConfigJsonClass, new()
        where TModuleStateConfig : ModuleStateConfig<TModuleStateConfigJsonClass>, new()
        where TModuleStateConfigJsonClass : ModuleStateConfigJsonClass, new()
    {
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
                state.IsPartOfPipeline = isPartOfPipeline;
                state.Applying = true;
                state.ErrorMessage = "Unexpected Halt!";
                state.DetailedErrorMessage = null;
                Module.ModuleStateConfig.Save(); 

                PreProcess();
                RunProcess();
                PostProcess(); 

                state.Applying = false;
                state.ErrorMessage = null;
                Module.ModuleStateConfig.Save();

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch(Exception e)
            {
                state.ErrorMessage = e.Message;
                state.DetailedErrorMessage = e.ToString();
                Module.ModuleStateConfig.Save();
                throw e;
            }
        }
        public abstract bool Check();
        protected abstract void PreProcess();
        protected abstract void RunProcess();
        protected abstract void PostProcess();
    }
}