using UnityEngine;
using UnityEditor;
using System;
using EazyBuildPipeline.PackageManager.Configs;
using System.IO;

namespace EazyBuildPipeline.PackageManager
{
    [Serializable]
    public class Styles
    {
        //为解决bundleTree和packageTree中不能直接调用EditorStyles的问题
        public GUIStyle LabelStyle;
        public GUIStyle InDropDownStyle;
        public GUIStyle InToggleStyle;
        public GUIStyle ButtonStyle;
        public void InitStyles() //这个函数只能在Awake和OnGUI里调用
        {
            LabelStyle = new GUIStyle("Label");
            InDropDownStyle = new GUIStyle("IN DropDown");
            InToggleStyle = new GUIStyle(EditorGUIUtility.isProSkin ? "OL ToggleWhite" : "OL Toggle");//这样做是因为OL Toggle样式在专业版皮肤下有Bug，因此用OL ToggleWhite代替
            ButtonStyle = new GUIStyle("Button");
        }
    }

    public static class G
    {
        public static string OverrideCurrentUserConfigName = null;

        public static Module Module;
        public static Runner Runner;
        public static GlobalReference g;
        public class GlobalReference
        {
            public Action OnChangeCurrentConfig = () => { };
            public Action OnChangeConfigList = () => { };
            public Editor.PackageTree packageTree;
            public Editor.BundleTree bundleTree;
            public Styles styles;
        }

        public static void Init()
        {
            Module = new Module();
            Runner = new Runner(Module);
            g = new GlobalReference();
            g.styles = new Styles();
            g.styles.InitStyles();
        }

        public static void Clear()
        {
            Module = null;
            Runner = null;
            g = null;
            OverrideCurrentUserConfigName = null;
        }

        public static string[] NecesseryEnum = { "Immediate", "Delayed" };
        public static string[] DeploymentLocationEnum = { "Built-in", "Server" };
        public static string[] PackageModeEnum = { "Addon", "Patch" };
        public static string[] LuaSourceEnum = { "None", "Origin", "ByteCode", "Encrypted" };
    }

    [Serializable]
    public class Module : EBPModule<
        ModuleConfig, ModuleConfig.JsonClass,
        ModuleStateConfig, ModuleStateConfig.JsonClass>
    {
        public override string ModuleName { get { return "PackageManager"; } }

        public string GetBundleFolderPath() { return Path.Combine(ModuleConfig.BundleWorkFolderPath, EBPUtility.GetTagStr(ModuleStateConfig.Json.CurrentTag) + "/Bundles"); }
        public string GetBundleInfoFolderPath() { return Path.Combine(ModuleConfig.BundleWorkFolderPath, EBPUtility.GetTagStr(ModuleStateConfig.Json.CurrentTag) + "/_Info"); }
        public int GetBundleFolderPathStrCount() { return GetBundleFolderPath().Length + 1; }

        public UserConfig UserConfig = new UserConfig();

        public override bool LoadAllConfigs(string pipelineRootPath, bool NOTLoadUserConfig = false)
        {
            bool success =
                LoadModuleConfig(pipelineRootPath);
            if (LoadModuleStateConfig(pipelineRootPath))
            {
                if (G.OverrideCurrentUserConfigName != null)
                {
                    ModuleStateConfig.Json.CurrentUserConfigName = G.OverrideCurrentUserConfigName;
                    G.OverrideCurrentUserConfigName = null;
                }
                if (!NOTLoadUserConfig)
                {
                    LoadUserConfig();
                }
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
                DisplayDialog("载入用户配置文件：" + ModuleStateConfig.CurrentUserConfigPath + " 时发生错误：" + e.Message);
                return false;
            }
        }
    }
}