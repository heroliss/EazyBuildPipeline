using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace EazyBuildPipeline.AssetPolice.Editor
{
    [Serializable]
    public class Module
    {
        public string ModuleName { get { return "AssetPolice"; } }
        readonly string configSearchText = "EazyBuildPipeline AssetPoliceConfig";
        public ModuleConfig ModuleConfig = new ModuleConfig();
        public StateConfig StateConfig = new StateConfig();

        public void LoadConfigs()
        {
            string[] guids = AssetDatabase.FindAssets(configSearchText);
            if (guids.Length == 0)
            {
                throw new EBPException("未能找到配置文件! 搜索文本：" + configSearchText);
            }
            string moduleConfigPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string configsFolderPath = Path.GetDirectoryName(moduleConfigPath);
            ModuleConfig.Load(moduleConfigPath);
            StateConfig.Load(Path.Combine(configsFolderPath, ModuleConfig.Json.StateConfigName));
        }
    }
}
