﻿using UnityEngine;
using UnityEditor;

namespace EazyBuildPipeline.PackageManager
{
    public partial class Runner
    {
        protected override void PostProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        protected override void PreProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}