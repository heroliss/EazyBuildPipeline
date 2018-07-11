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
            if (EazyBuildPipeline.BundleManager.Editor.G.g != null)
            {
                var buildMap = EazyBuildPipeline.BundleManager.Editor.G.g.mainTab.GetBuildMap_extension();
                System.IO.File.WriteAllText(System.IO.Path.Combine(
                    EazyBuildPipeline.BundleManager.Editor.G.configs.LocalConfig.Local_BundleMapsFolderPath,
                    System.IO.Path.GetFileName(path) + ".json"),
                    Newtonsoft.Json.JsonConvert.SerializeObject(buildMap, Newtonsoft.Json.Formatting.Indented));
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