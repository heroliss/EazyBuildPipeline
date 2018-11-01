using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace EazyBuildPipeline
{
    //TODO: 有空整理全部代码的异常处理,并把下面的异常类放到合适的地方
    class EBPException : ApplicationException
    {
        public EBPException(string message) : base(message) { }
        public EBPException(string message, Exception innerException) : base(message, innerException) { }
    }
    class EBPCheckFailedException : EBPException
    {
        public EBPCheckFailedException(string message) : base(message) { }
    }

    public static class CommandLineRunner
    {
        public static void Run()
        {
            //加载公共基础配置
            CommonModule.LoadCommonConfig();
            CommonModule.CommonConfig.Json.PipelineRootPath = EBPUtility.GetArgValue("PipelineRootPath");
            PipelineTotalControl.Module totalModule = new PipelineTotalControl.Module();
            //加载和检查每一个模块
            string platform = EBPUtility.GetArgValue("Platform");
            switch (platform.ToLower())
            {
                case "android": platform = "Android"; break;
                case "ios": platform = "iOS"; break;
            }
            string[] assetTag = new[] { platform, EBPUtility.GetArgValue("Definition").ToUpper(), EBPUtility.GetArgValue("Language").ToUpper() };
            var disableModules = EBPUtility.GetArgValuesLower("DisableModule");
            bool checkMode = CommonModule.CommonConfig.Args_lower.Contains("--checkmode");
            foreach (var runner in totalModule.Runners)
            {
                if (disableModules.Contains(runner.BaseModule.ModuleName.ToLower()))
                {
                    continue; //跳过该模块
                }
                //加载模块配置
                runner.BaseModule.LoadAllConfigs(true);
                //设置Tag
                runner.BaseModule.BaseModuleStateConfig.BaseJson.CurrentTag = assetTag;
                //覆盖当前状态配置
                switch (runner.BaseModule.ModuleName)
                {
                    case "SVNUpdate":
                        break;
                    case "AssetPreprocessor":
                        runner.BaseModule.BaseModuleStateConfig.BaseJson.CurrentUserConfigName = EBPUtility.GetTagStr(assetTag) + ".json";
                        break;
                    case "BundleManager":
                        BuildAssetBundleOptions compressOption = BuildAssetBundleOptions.None;
                        switch (EBPUtility.GetArgValue("BundleCompressMode"))
                        {
                            case "Uncompress":
                                compressOption = BuildAssetBundleOptions.UncompressedAssetBundle;
                                break;
                            case "LZMA":
                                compressOption = BuildAssetBundleOptions.None;
                                break;
                            case "LZ4":
                                compressOption = BuildAssetBundleOptions.ChunkBasedCompression;
                                break;
                        }
                        totalModule.BundleManagerModule.ModuleStateConfig.Json.CompressionOption = compressOption;
                        totalModule.BundleManagerModule.ModuleStateConfig.Json.CurrentResourceVersion = int.Parse(EBPUtility.GetArgValue("ResourceVersion"));
                        break;
                    case "PackageManager":
                        runner.BaseModule.BaseModuleStateConfig.BaseJson.CurrentUserConfigName = EBPUtility.GetArgValue("subpackage") + ".json";
                        totalModule.PackageManagerModule.ModuleStateConfig.Json.CurrentAddonVersion = EBPUtility.GetArgValue("ClientVersion");
                        break;
                    case "PlayerBuilder":
                        runner.BaseModule.BaseModuleStateConfig.BaseJson.CurrentUserConfigName = EBPUtility.GetArgValue("channel") + ".json";
                        break;
                    default:
                        break;
                }
                //加载用户配置
                runner.BaseModule.LoadUserConfig();
                //覆盖用户配置
                switch (runner.BaseModule.ModuleName)
                {
                    case "SVNUpdate":
                        break;
                    case "AssetPreprocessor":
                        break;
                    case "BundleManager":
                        break;
                    case "PackageManager":
                        break;
                    case "PlayerBuilder":
                        totalModule.PlayerBuilderRunner.ConfigURL_Game = EBPUtility.GetArgValue("ConfigURL-Game");
                        totalModule.PlayerBuilderRunner.ConfigURL_Language = EBPUtility.GetArgValue("ConfigURL-Language");
                        totalModule.PlayerBuilderRunner.ConfigURL_LanguageVersion = EBPUtility.GetArgValue("ConfigURL-LanguageVersion");
                        switch (assetTag[0])
                        {
                            case "iOS":
                                totalModule.PlayerBuilderModule.UserConfig.Json.PlayerSettings.IOS.BundleID = EBPUtility.GetArgValue("BundleID");
                                totalModule.PlayerBuilderModule.UserConfig.Json.PlayerSettings.IOS.ClientVersion = EBPUtility.GetArgValue("ClientVersion");
                                totalModule.PlayerBuilderModule.UserConfig.Json.PlayerSettings.IOS.BuildNumber = EBPUtility.GetArgValue("BuildNumber");
                                break;
                            case "Android":
                                totalModule.PlayerBuilderModule.UserConfig.Json.PlayerSettings.Android.PackageName = EBPUtility.GetArgValue("BundleID");
                                totalModule.PlayerBuilderModule.UserConfig.Json.PlayerSettings.Android.ClientVersion = EBPUtility.GetArgValue("ClientVersion");
                                totalModule.PlayerBuilderModule.UserConfig.Json.PlayerSettings.Android.BundleVersionCode = int.Parse(EBPUtility.GetArgValue("BuildNumber"));
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
                //检查配置
                runner.Check();
            }
            //运行每一个模块
            if (!checkMode)
            {
                foreach (var runner in totalModule.Runners)
                {
                    if (disableModules.Contains(runner.BaseModule.ModuleName.ToLower()))
                    {
                        continue; //跳过该模块
                    }
                    runner.Run(true);
                }
                totalModule.StartLog();
                totalModule.Log("[BuildPipeline Running Successfully]");
                totalModule.EndLog();
            }
        }
    }
}
