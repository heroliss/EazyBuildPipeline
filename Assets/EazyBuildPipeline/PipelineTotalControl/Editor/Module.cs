using UnityEngine;
using UnityEditor;
using System;
using EazyBuildPipeline.PipelineTotalControl.Configs;

namespace EazyBuildPipeline.PipelineTotalControl
{
    public static class G
    {
        public static Module Module;
        //这里可能需要抽象出来一个总控用的Runner
        public static GlobalReference g;
        public class GlobalReference
        {
            public EditorWindow MainWindow;
        }

        public static void Init()
        {
            Module = new Module();
            g = new GlobalReference();
        }

        public static void Clear()
        {
            Module = null;
            g = null;
        }
    }

    [Serializable]
    public class Module : EBPModule<
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public override string ModuleName { get { return "TotalControl"; } }
        public AssetPreprocessor.Module AssetPreprocessorModule = new AssetPreprocessor.Module();
        public AssetPreprocessor.Runner AssetPreprocessorRunner;
        public BundleManager.Module BundleManagerModule = new BundleManager.Module();
        public BundleManager.Runner BundleManagerRunner;
        public PackageManager.Module PackageManagerModule = new PackageManager.Module();
        public PackageManager.Runner PackageManagerRunner;
        public PlayerBuilder.Module PlayerBuilderModule = new PlayerBuilder.Module();
        public PlayerBuilder.Runner PlayerBuilderRunner;
        //TODO:可以构建模块列表来减少重复代码

        public Module()
        {
            InitRunners();
        }

        public bool LoadAllConfigs(string pipelineRootPath = null)
        {
            if (!CommonModule.LoadCommonConfig()) return false;
            if (pipelineRootPath != null)
            {
                CommonModule.CommonConfig.Json.PipelineRootPath = pipelineRootPath;
            }
            if (!LoadModuleConfig()) return false;
            //这里暂时不需要ModuleStateConfig，所以不加载

            if (AssetPreprocessorModule.LoadModuleConfig())
            { AssetPreprocessorModule.LoadModuleStateConfig(); }
            if (BundleManagerModule.LoadModuleConfig())
            { BundleManagerModule.LoadModuleStateConfig(); }
            if (PackageManagerModule.LoadModuleConfig())
            { PackageManagerModule.LoadModuleStateConfig(); }
            if (PlayerBuilderModule.LoadModuleConfig())
            {
                if (PlayerBuilderModule.LoadModuleStateConfig())
                {
                    PlayerBuilderModule.LoadUserConfig();
                }
            }

            //这里需要将静态的Module与TotalControl中的Module同步，因为该窗口总控面板与PlayerSetting面板结合了
            PlayerBuilder.G.Module = PlayerBuilderModule; 
            PlayerBuilder.G.Runner = PlayerBuilderRunner;
            return true;
        }

        public void InitRunners()
        {
            AssetPreprocessorRunner = new AssetPreprocessor.Runner(AssetPreprocessorModule);
            BundleManagerRunner = new BundleManager.Runner(BundleManagerModule);
            PackageManagerRunner = new PackageManager.Runner(PackageManagerModule);
            PlayerBuilderRunner = new PlayerBuilder.Runner(PlayerBuilderModule);
        }
    }
}