﻿using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace EazyBuildPipeline.Common.Configs
{
    [Serializable]
    public class CommonConfig : EBPConfig<CommonConfig.JsonClass>, ISerializationCallbackReceiver
    {
        public CommonConfig()
        {
            Json.TagEnum = new Dictionary<string, string[]> //这里不能在JsonClass里初始化，因为这个东西需要自定义序列化方法来处理
                { { "Example Group 1:",new []{"example tag1","example tag2","example tag3"} },
                  { "Example Group 2:",new []{"example tag a","example tag b"} } };
        }
        public string IconsFolderPath { get { return Path.Combine(CommonRootPath, Json.IconsFolderRelativePath); } }
        public string CommonRootPath { get { return Path.GetDirectoryName(JsonPath); } }
        [Serializable]
        public class JsonClass
        {
            public string PipelineRootPath;
            public string[] CurrentAssetTag = { "Example Tag1", "Example Tag2" };
            public Dictionary<string, string[]> TagEnum;
            public string IconsFolderRelativePath;
        }

        #region 自定义序列化
        //由于EBPConfig使用的NewtonJson可以序列化字典，但Unity自动序列化不支持字典,
        //所以需要在此针对TagEnum自定义序列化来保证Unity的自动序列化正常可用。
        [SerializeField] private List<TagType> tagEnum = new List<TagType>(); //用于Unity自动序列化的转换变量
        [Serializable]
        public class TagType //用此类型包装Dictionary<string, string[]>
        {
            public string type;
            public string[] tags;
        }

        public void OnBeforeSerialize()
        {
            tagEnum.Clear();
            tagEnum.Capacity = Json.TagEnum.Count;
            foreach (var item in Json.TagEnum)
            {
                tagEnum.Add(new TagType { type = item.Key, tags = item.Value });
            }
        }
        public void OnAfterDeserialize()
        {
            Json.TagEnum.Clear();
            for (int i = 0; i < tagEnum.Count; ++i)
            {
                Json.TagEnum.Add(tagEnum[i].type, tagEnum[i].tags);
            }
        }
        #endregion
    }

    [Serializable]
    public class ModuleConfig<TJsonClass> : EBPConfig<TJsonClass> where TJsonClass : ModuleConfigJsonClass, new()
    {
        public string PipelineRootPath;
        public string WorkPath { get { return Path.Combine(PipelineRootPath, Json.WorkRelativePath); } }
        public string StateConfigPath { get { return Path.Combine(WorkPath, Json.StateConfigRelativePath); } }
        public string UserConfigsFolderPath { get { return Path.Combine(ModuleRootPath, Json.UserConfigsFolderRelativePath); } }
        public string ModuleRootPath { get { return Path.GetDirectoryName(JsonPath); } }
    }

    [Serializable]
    public class ModuleConfigJsonClass
    {
        public string UserConfigsFolderRelativePath = "UserConfigs";
        public string WorkRelativePath;
        public string StateConfigRelativePath = "_Configs/State.json";
    }

    [Serializable]
    public class ModuleStateConfig<TJsonClass> : EBPConfig<TJsonClass> where TJsonClass : ModuleStateConfigJsonClass, new()
    {
        public string UserConfigsFolderPath;
        public string CurrentUserConfigPath { get { return Path.Combine(UserConfigsFolderPath, Json.CurrentUserConfigName); } }
    }

    [Serializable]
    public class ModuleStateConfigJsonClass
    {
        public string[] CurrentTag = { "Example Platform" };
        public string CurrentUserConfigName;
        public bool Applying;
        public bool IsPartOfPipeline;
        public string Message;
    }
}

namespace EazyBuildPipeline
{
    /// <summary> EazyBuildPipeline配置基类 </summary>
    /// <typeparam name="TJson">与Json文件对应的配置类</typeparam>
    [Serializable]
    public class EBPConfig<TJson> where TJson : new()
    {
        public TJson Json = new TJson();
        public string JsonPath;

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <param name="path">文件路径，若为null则根据JsonPath加载，否则修改JsonPath并加载。</param>
        public virtual void Load(string path = null)
        {
            JsonPath = path ?? JsonPath;
            Json = JsonConvert.DeserializeObject<TJson>(File.ReadAllText(JsonPath));
        }
        /// <summary>
        /// 保存配置
        /// </summary>
        /// <param name="path">保存路径，若为null则根据JsonPath保存，否则根据此参数保存，JsonPath不会改变。</param>
        public virtual void Save(string path = null)
        {
            File.WriteAllText(path ?? JsonPath, JsonConvert.SerializeObject(Json, Formatting.Indented));
        }
        /// <summary>
        /// 返回与保存到Json文件相同的内容
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(Json, Formatting.Indented);
        }
    }
}