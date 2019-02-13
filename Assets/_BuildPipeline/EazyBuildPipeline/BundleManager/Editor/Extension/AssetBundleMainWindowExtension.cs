using UnityEngine;
using UnityEditor;

namespace AssetBundleManagement2
{
	partial class AssetBundleMainWindow
    {
        internal void OnEnable_extension()
        {
            OnEnable();
        }

        internal void OnDestroy_extension()
        {
            OnDestroy();
        }

        internal void OnGUI_extension(Rect rect)
        {
            position = rect;
            OnGUI();
        }

        internal void OnFocus_extension()
        {
            OnFocus();
        }

        internal void Update_extension()
        {
            Update();
        }

        internal AssetBundleBuild[] GetBuildMap_extension()
        {
            return buildTab.GetBuildMap_extension();
        }

        public void AutoSaveBuildMap(string path)
        {
            if (EazyBuildPipeline.BundleManager.G.g != null)
            {
                if (!System.IO.Directory.Exists(EazyBuildPipeline.CommonModule.CommonConfig.UserConfigsRootPath))
                {
                    EazyBuildPipeline.BundleManager.G.Module.DisplayDialog("创建失败！用户配置根目录不存在：" + EazyBuildPipeline.CommonModule.CommonConfig.UserConfigsRootPath);
                }
                else
                {
                    var buildMap = EazyBuildPipeline.BundleManager.G.g.mainTab.GetBuildMap_extension();
                    System.IO.Directory.CreateDirectory(EazyBuildPipeline.BundleManager.G.Module.ModuleConfig.UserConfigsFolderPath);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(
                        EazyBuildPipeline.BundleManager.G.Module.ModuleConfig.UserConfigsFolderPath,
                        System.IO.Path.GetFileName(path) + ".json"),
                        Newtonsoft.Json.JsonConvert.SerializeObject(buildMap, Newtonsoft.Json.Formatting.Indented));
                    EazyBuildPipeline.BundleManager.G.g.OnChangeConfigList();
                }
            }
            //            buildTab_.UpdateBuildMap(path);
            if (AssetBundleModel.IsBundleMapDirty)
            {
                buildTab.UpdateConfigFile(path);
                AssetBundleModel.IsBundleMapDirty = false;
            }
        }
    }
}