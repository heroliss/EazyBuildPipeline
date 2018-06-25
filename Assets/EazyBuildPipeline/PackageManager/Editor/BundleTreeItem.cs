using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public class BundleTreeItem : TreeViewItem
    {
        public GUIStyle style = UnityEditor.EditorStyles.label;
        public List<PackageTreeItem> packageItems = new List<PackageTreeItem>(); //在package tree中对应的项
        public bool isFolder;
        public bool verify; //该manifest文件同级目录下是否包含同名文件
        public long size;
        public string path;
        public string relativePath;
        public string bundlePath; //去掉manifest后缀后的完整路径

        public BundleTreeItem() : base()
        {
        }

        public BundleTreeItem(int id, int depth, string displayName) : base(id, depth, displayName)
        {

        }
    }
}
