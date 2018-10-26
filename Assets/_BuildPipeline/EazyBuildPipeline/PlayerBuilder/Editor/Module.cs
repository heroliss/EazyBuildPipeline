using System;
using System.IO;
using EazyBuildPipeline.PlayerBuilder.Configs;
using UnityEditor;

namespace EazyBuildPipeline.PlayerBuilder
{
    public static class G
    {
        public static Module Module;
        public static Runner Runner;
        public static GlobalReference g;
        public class GlobalReference
        {
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
        }
    }

    [Serializable]
    public class Module : EBPModule<
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public override string ModuleName { get { return "PlayerBuilder"; } }
        public UserConfig UserConfig = new UserConfig();

        public override bool LoadAllConfigs(bool NOTLoadUserConfig = false)
        {
            bool success = LoadModuleConfig();
            LoadModuleStateConfig();
            //if (G.OverrideCurrentUserConfigName != null)
            //{
            //    ModuleStateConfig.Json.CurrentUserConfigName = G.OverrideCurrentUserConfigName;
            //    G.OverrideCurrentUserConfigName = null;
            //}
            if (!NOTLoadUserConfig && ModuleStateConfig.CurrentUserConfigPath != null)
            {
                LoadUserConfig();
            }
            return success;
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
                DisplayDialog("载入用户配置文件：" + UserConfig.JsonPath + " 时发生错误：" + e.Message);
                return false;
            }
        }
    }
}