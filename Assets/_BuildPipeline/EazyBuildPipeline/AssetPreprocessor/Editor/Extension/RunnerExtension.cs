using UnityEngine;
using UnityEditor;
using EazyBuildPipeline.AssetPreprocessor.Configs;
using System;

namespace EazyBuildPipeline.AssetPreprocessor
{
    public partial class Runner
    {
        protected override void PreProcess()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        protected override void PostProcess()
        {
        }
    }
}