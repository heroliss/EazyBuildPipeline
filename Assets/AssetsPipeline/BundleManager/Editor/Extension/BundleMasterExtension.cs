using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace AssetBundleManagement2
{
    partial class AssetBundleMainWindow
    {
        internal void OnEnable_extension()
        {
            LiXuFeng.BundleManager.Editor.Configs.g.Apply += BuildBundles_extension;
            OnEnable();
        }

        internal void BuildBundles_extension()
        {
            Debug.Log("build bundles at " + string.Join("_",
                LiXuFeng.BundleManager.Editor.Configs.configs.BundleManagerConfig.CurrentTags));
        }

        internal void OnDisable_extension()
        {
            LiXuFeng.BundleManager.Editor.Configs.g.Apply -= BuildBundles_extension;
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
    }
}
