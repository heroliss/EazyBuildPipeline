using AssetBundleManagement;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

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