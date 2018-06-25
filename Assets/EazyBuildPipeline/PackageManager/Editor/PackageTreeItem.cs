using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public class PackageTreeItem : TreeViewItem
    {
        public string fileName;
        public bool locked;//若true则锁定该packageTreeItem不可改名或删除或修改其子项 //TODO：暂时无用
        public bool lost; //是否存在对应的BundleTreeItem
        public bool isPackage;
        public bool complete; //是否完全包含bundleTree中对应的递归子节点中的所有项
        public Color packageColor;  //package的color，仅在是包时有效
        public PackageTreeItem package; //所属的package
        public BundleTreeItem bundleItem;
        public string necessery;
        public string deploymentLocation;
        public bool copyToStreaming;

        public PackageTreeItem() : base()
        {
        }

        public PackageTreeItem(int id, int depth, string displayName) : base(id, depth, displayName)
        {

        }
    }
}
