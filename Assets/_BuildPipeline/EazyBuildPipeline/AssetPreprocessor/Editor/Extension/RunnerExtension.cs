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
            EBPUtility.RefreshAssets();
        }

        protected override void PostProcess()
        {
            EBPUtility.RefreshAssets();
        }
    }
}