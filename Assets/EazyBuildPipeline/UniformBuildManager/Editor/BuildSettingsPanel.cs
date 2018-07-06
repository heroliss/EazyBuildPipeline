using UnityEngine;
using UnityEditor;

namespace EazyBuildPipeline.UniformBuildManager.Editor
{
    public class BuildSettingsPanel
    {
        public void Awake()
        {

        }

        public void OnDestory()
        {

        }

        public void OnGUI()
        {
            ; GUI.Button(new Rect(200, 200, 200, 100), "这里是BuildSettings");
        }
    }
}