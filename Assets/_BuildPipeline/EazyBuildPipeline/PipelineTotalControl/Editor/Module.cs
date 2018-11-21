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
        public SVNUpdate.Module SVNUpdateModule = new SVNUpdate.Module();
        public SVNUpdate.Runner SVNUpdateRunner = new SVNUpdate.Runner();
        public AssetPreprocessor.Module AssetPreprocessorModule = new AssetPreprocessor.Module();
        public AssetPreprocessor.Runner AssetPreprocessorRunner = new AssetPreprocessor.Runner();
        public BundleManager.Module BundleManagerModule = new BundleManager.Module();
        public BundleManager.Runner BundleManagerRunner = new BundleManager.Runner();
        public PackageManager.Module PackageManagerModule = new PackageManager.Module();
        public PackageManager.Runner PackageManagerRunner = new PackageManager.Runner();
        public PlayerBuilder.Module PlayerBuilderModule = new PlayerBuilder.Module();
        public PlayerBuilder.Runner PlayerBuilderRunner = new PlayerBuilder.Runner();

        [NonSerialized]
        public List<IRunner> Runners;

        public override string ModuleName { get { return "TotalControl"; } }

        public void Init()
        {
            //以下均为引用关系的初始化
            InitRunners();
            Runners = new List<IRunner>
            {
                SVNUpdateRunner,
                AssetPreprocessorRunner,
                BundleManagerRunner,
                PackageManagerRunner,
                PlayerBuilderRunner
            };
            //这里需要将静态的Module与TotalControl中的Module同步(用于反序列化时重新指定新的引用)
            PlayerBuilder.G.Module = PlayerBuilderModule;
            PlayerBuilder.G.Runner = PlayerBuilderRunner;
        }
        public Module()
        {
            Init();
        }

        public bool LoadAllModules()
        {
            //加载所有模块的模块配置、状态配置、用户配置
            foreach (var item in Runners)
            {
                item.BaseModule.LoadAllConfigs();
            }
            //这里需要将静态的Module与TotalControl中的Module同步，因为该窗口总控面板与PlayerSetting面板结合了
            PlayerBuilder.G.Module = PlayerBuilderModule;
            PlayerBuilder.G.Runner = PlayerBuilderRunner;
            return true;
        }

        public void InitRunners()
        {
            //SVNUpdateRunner = new SVNUpdate.Runner(SVNUpdateModule);
            //AssetPreprocessorRunner = new AssetPreprocessor.Runner(AssetPreprocessorModule);
            //BundleManagerRunner = new BundleManager.Runner(BundleManagerModule);
            //PackageManagerRunner = new PackageManager.Runner(PackageManagerModule);
            //PlayerBuilderRunner = new PlayerBuilder.Runner(PlayerBuilderModule);

            SVNUpdateRunner.Module = SVNUpdateModule;
            AssetPreprocessorRunner.Module = AssetPreprocessorModule;
            BundleManagerRunner.Module = BundleManagerModule;
            PackageManagerRunner.Module = PackageManagerModule;
            PlayerBuilderRunner.Module = PlayerBuilderModule;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Init();
        }

        public override bool LoadAllConfigs(bool NOTLoadUserConfig = false)
        {
            throw new NotImplementedException("总控模块暂时不需任何配置");
        }

        public override bool LoadUserConfig()
        {
            throw new NotImplementedException("总控模块暂时不需要用户配置");
        }
    }
}