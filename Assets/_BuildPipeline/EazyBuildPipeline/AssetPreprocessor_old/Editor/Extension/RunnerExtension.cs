using UnityEngine;
using UnityEditor;
using EazyBuildPipeline.AssetPreprocessor_old.Configs;
using System;

namespace EazyBuildPipeline.AssetPreprocessor_old
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