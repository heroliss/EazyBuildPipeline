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
    }
}
