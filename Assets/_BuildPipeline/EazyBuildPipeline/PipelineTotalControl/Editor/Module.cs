using UnityEngine;
using UnityEditor;
using System;
using EazyBuildPipeline.PipelineTotalControl.Configs;
using System.Collections.Generic;

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
        ModuleStateConfig, ModuleStateConfig.JsonClass>, ISerializationCallbackReceiver
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

        [NonSerialized]
        public List<ModuleItem> Modules;
        public struct ModuleItem
        {
            public ModuleItem(BaseModule module, IRunner runner)
            {
                Module = module;
                Runner = runner;
            }
            public BaseModule Module;
            public IRunner Runner;
        }
        public void Init()
        {
            InitRunners();
            Modules = new List<ModuleItem>
            {
                new ModuleItem(AssetPreprocessorModule, AssetPreprocessorRunner),
                new ModuleItem(BundleManagerModule, BundleManagerRunner),
                new ModuleItem(PackageManagerModule, PackageManagerRunner),
                new ModuleItem(PlayerBuilderModule, PlayerBuilderRunner),
            };
            //这里需要将静态的Module与TotalControl中的Module同步(用于反序列化时重新指定新的引用)
            PlayerBuilder.G.Module = PlayerBuilderModule;
            PlayerBuilder.G.Runner = PlayerBuilderRunner;
        }
        public Module()
        {
            Init();
        }

        public override bool LoadAllConfigs(string pipelineRootPath)
        {
            if (!LoadModuleConfig(pipelineRootPath)) return false;
            //这里暂时不需要ModuleStateConfig，所以不加载
            
            //加载所有模块的模块配置、状态配置、用户配置
            foreach (var item in Modules)
            {
                item.Module.LoadAllConfigs(pipelineRootPath);
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

        public override bool LoadUserConfig()
        {
            throw new NotImplementedException("总控模块当前不存在用户配置，应该避免加载和使用。");
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Init();
        }
    }
}