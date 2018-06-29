using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace EazyBuildPipeline.PackageManager.Editor
{
    public class PackageTreeItem : TreeViewItem
    {
        private int colorBlockEdgeWidth = 1;
        private int colorBlockSize = 8;
        private Texture2D colorBlocktexture;
        private Texture2D colorBlocktexture_hollow;
        private Color _packageColor;  //package的color，仅在是包时有效

        public string fileName;
        public bool locked;//若true则锁定该packageTreeItem不可改名或删除或修改其子项 //TODO：暂时无用
        public bool lost; //是否存在对应的BundleTreeItem
        public bool isPackage;
        public bool complete; //是否完全包含bundleTree中对应的递归子节点中的所有项
        public Color packageColor
        {
            get { return _packageColor; }
            set
            {
                _packageColor = value;
                Color32[] colorsLine = new Color32[colorBlockSize * colorBlockEdgeWidth];
                for (int i = 0; i < colorsLine.Length; i++)
                {
                    colorsLine[i] = _packageColor;
                }
                Color32[] colors = new Color32[colorBlockSize * colorBlockSize];
                for (int i = 0; i < colors.Length; i++)
                {
                    colors[i] = Color.clear;
                }
                colorBlocktexture_hollow.SetPixels32(colors);
                colorBlocktexture_hollow.SetPixels32(0, 0, colorBlockSize, colorBlockEdgeWidth, colorsLine);
                colorBlocktexture_hollow.SetPixels32(0, 0, colorBlockEdgeWidth, colorBlockSize, colorsLine);
                colorBlocktexture_hollow.SetPixels32(0, colorBlockSize - colorBlockEdgeWidth, colorBlockSize, colorBlockEdgeWidth, colorsLine);
                colorBlocktexture_hollow.SetPixels32(colorBlockSize - colorBlockEdgeWidth, 0, colorBlockEdgeWidth, colorBlockSize, colorsLine);

                colorBlocktexture_hollow.Apply();

                colorBlocktexture.SetPixel(0, 0, _packageColor);
                colorBlocktexture.Apply();
            }
        }

        public GUIStyle colorBlockStyle_hollow;
        public GUIStyle colorBlockStyle;
        public PackageTreeItem package; //所属的package
        public BundleTreeItem bundleItem;
        public string necessery;
        public string deploymentLocation;
        public bool copyToStreaming;

        public PackageTreeItem() : base()
        {
            Init();
        }

        public PackageTreeItem(int id, int depth, string displayName) : base(id, depth, displayName)
        {
            Init();
        }
        private void Init()
        {
            colorBlocktexture_hollow = new Texture2D(colorBlockSize, colorBlockSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                anisoLevel = 1,
                mipMapBias = 0,
            };
            colorBlocktexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                anisoLevel = 1,
                mipMapBias = 0,
            };
            colorBlockStyle_hollow = new GUIStyle("Button");
            colorBlockStyle_hollow.normal.background = colorBlocktexture_hollow;
            colorBlockStyle = new GUIStyle("Button");
            colorBlockStyle.normal.background = colorBlocktexture;
        }
    }
}
