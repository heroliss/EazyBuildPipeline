#pragma warning disable 0649

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using EazyBuildPipeline.AssetPreprocessor.Configs;

namespace EazyBuildPipeline.AssetPreprocessor
{
    public static class G
    {
        public static string OverrideCurrentUserConfigName = null;

        public static Module Module;
        public static Runner Runner;
        public static GlobalReference g;
        public class GlobalReference
        {
            public Action OnChangeCurrentUserConfig = () => { };
            public Action OnChangeConfigList = () => { };
        }
        public static void Init()
        {
            Module = new Module();
            Runner = new Runner(Module);
            g = new GlobalReference();
        }
        public static void Clear()
        {
            Module = null;
            Runner = null;
            g = null;
            OverrideCurrentUserConfigName = null;
        }
    }

    [Serializable]
    public class Module : EBPModule<
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public override string ModuleName { get { return "AssetPreprocessor"; } }
        public OptionsEnumConfig OptionsEnumConfig = new OptionsEnumConfig();
        public UserConfig UserConfig = new UserConfig();

        public override bool LoadAllConfigs(string pipelineRootPath)
        {
            bool success =
              LoadModuleConfig(pipelineRootPath) &&
              LoadOptionsEnumConfig();
            if (LoadModuleStateConfig(pipelineRootPath))
            {
                if (G.OverrideCurrentUserConfigName != null)
                {
                    ModuleStateConfig.Json.CurrentUserConfigName = G.OverrideCurrentUserConfigName;
                    G.OverrideCurrentUserConfigName = null;
                }
                LoadUserConfig();
            }
            return success;
        }

        public bool LoadOptionsEnumConfig()
        {
            try
            {
                OptionsEnumConfig.Load(ModuleConfig.OptionsEnumConfigPath);
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载选项配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + OptionsEnumConfig.JsonPath
                    + "\n请设置正确的路径以及形如以下所示的配置文件：\n" + new OptionsEnumConfig());
                return false;
            }
        }

        public override bool LoadUserConfig()
        {
            try
            {
                if (!string.IsNullOrEmpty(ModuleStateConfig.CurrentUserConfigPath))
                {
                    UserConfig.Load(ModuleStateConfig.CurrentUserConfigPath);
                }
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载当前用户配置时发生错误：" + e.Message
                    + "\n加载路径：" + UserConfig.JsonPath);
                return false;
            }
        }
    }
}