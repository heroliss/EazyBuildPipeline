using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace AssetBundleManagement2
{
    partial class AssetBundleMainWindow
    {
        internal void OnEnable_extension()
        {
            OnEnable();
        }

        internal void BuildBundles_extension(BuildTarget target,int optionsValue, string path)
        {
            buildTab.ExecuteBuild_extension(target, optionsValue, path);
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
