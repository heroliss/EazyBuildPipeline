using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LiXuFeng.PackageManager.Editor
{
    public class PackageManagerWindow : EditorWindow
    {
        private PackageManagerTab tab;

        [MenuItem("Window/BuildPipeline/PackageManager")]
        static void ShowWindow()
        {
            GetWindow<PackageManagerWindow>();
        }
        public PackageManagerWindow()
        {
            titleContent = new GUIContent("Package");
        }
        private void OnEnable()
        {
            Rect tabRect = new Rect(6, 6, position.width - 12, position.height - 12);
            //Rect tabRect = GetSubWindowArea(); //面板选择时使用这句
            if (tab == null)
                tab = new PackageManagerTab();
            tab.OnEnable(tabRect, this);
        }
        private void OnDisable()
        {
            tab.OnDisable();
        }
        private void OnGUI()
        {
            tab.OnGUI(new Rect(6, 6, position.width - 12, position.height - 12));
        }
    }
}
