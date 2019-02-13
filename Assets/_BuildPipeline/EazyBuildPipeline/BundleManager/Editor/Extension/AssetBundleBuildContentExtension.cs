using UnityEditor;

namespace AssetBundleManagement2
{
    partial class AssetBundleBuildContent
    {
        public AssetBundleBuild[] GetBuildMap_extension()
        {
            return GetBuildMaps().ToArray();
        }
    }
}