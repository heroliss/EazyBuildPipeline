#pragma warning disable 0649
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace EazyBuildPipeline.PipelineTotalControl.Configs
{
    [Serializable]
    public class ModuleConfig : Common.Configs.ModuleConfig<ModuleConfig.JsonClass>
    {
        [Serializable]
        public class JsonClass : Common.Configs.ModuleConfigJsonClass
        {
            public bool EnableCheckDiff;
        }
    }

    //暂时无用
    [Serializable]
    public class ModuleStateConfig : Common.Configs.ModuleStateConfig<ModuleStateConfig.JsonClass> 
    {
        [Serializable]
        public class JsonClass : Common.Configs.ModuleStateConfigJsonClass
        {
        }
    }
}