using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace EazyBuildPipeline.Common.Configs
{
    [Serializable]
    public class CommonConfig : EBPConfig<CommonConfig.JsonClass>, ISerializationCallbackReceiver
    {
        public List<string> Args;
        public List<string> Args_lower;
        public bool IsBatchMode;
        public CommonConfig()
        {
            //获取参数
            Args = Environment.GetCommandLineArgs().ToList();
            Args_lower = new List<string>(Args.Count);
            for (int i = 0; i < Args.Count; i++)
            {
                Args_lower.Add(Args[i].ToLower());
            }
            if (Args_lower.Contains("-batchmode")) //HACK: Application.isBatchMode(for Unity 2018.3+)
            {
                IsBatchMode = true;
            }

            Json.TagEnum = new Dictionary<string, string[]> //这里不能在JsonClass里初始化，因为这个东西需要自定义序列化方法来处理
                { { "Example Group 1:",new []{"example tag1","example tag2","example tag3"} },
                  { "Example Group 2:",new []{"example tag a","example tag b"} } };
        }
        public string CurrentLogFolderPath; //该值是否为空，是否记录日志的主开关，该变量会经常被修改
        //主日志路径，若为空则不记录日志，可由命令行参数赋值
        public string PipelineLogPath { get { return string.IsNullOrEmpty(CurrentLogFolderPath) || string.IsNullOrEmpty(Json.PipelineLogFileName) ? null : Path.Combine(CurrentLogFolderPath, Json.PipelineLogFileName); } }
        public string IconsFolderPath { get { return Path.Combine(CommonConfigRootPath, Json.IconsFolderRelativePath); } }
        public string CommonConfigRootPath { get { return Path.GetDirectoryName(JsonPath); } }
        public string UserConfigsFolderPath_AssetPreprocessor { get { return Path.Combine(UserConfigsRootPath, Json.UserConfigsFolderName_AssetPreprocessor); } }
        public string UserConfigsFolderPath_BundleManager { get { return Path.Combine(UserConfigsRootPath, Json.UserConfigsFolderName_BundleManager); } }
        public string UserConfigsFolderPath_PackageManager { get { return Path.Combine(UserConfigsRootPath, Json.UserConfigsFolderName_PackageManager); } }
        public string UserConfigsFolderPath_PlayerBuilder { get { return Path.Combine(UserConfigsRootPath, Json.UserConfigsFolderName_PlayerBuilder); } }
        public string UserConfigsRootPath { get { return Path.Combine(Json.PipelineRootPath, Json.UserConfigsRootName); } }
        public string LogsRootPath { get { return Path.Combine(Json.PipelineRootPath, Json.LogsRootName); } }
        public string DataRootPath { get { return Path.Combine(Json.PipelineRootPath, Json.DataRootName); } }

        [Serializable]
        public class JsonClass
        {
            public string PipelineRootPath;
            public string[] CurrentAssetTag = { "Example Tag1", "Example Tag2" };
            public Dictionary<string, string[]> TagEnum;
            public string IconsFolderRelativePath;

            public string PipelineLogFileName; //该值为空同样不记录日志
            public string DataRootName;
            public string LogsRootName;
            public string UserConfigsRootName;
            public string UserConfigsFolderName_AssetPreprocessor;
            public string UserConfigsFolderName_BundleManager;
            public string UserConfigsFolderName_PackageManager;
            public string UserConfigsFolderName_PlayerBuilder;

            public string DirectoryRegex, FileRegex;
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

    public interface IModuleConfig
    {
        string JsonPath { get; set; }
        string WorkPath { get; }
        string StateConfigPath { get; }
        string UserConfigsFolderPath { get; }
        string ModuleRootPath { get; }
        ModuleConfigJsonClass BaseJson { get; }
    }

    [Serializable]
    public abstract class ModuleConfig<TJsonClass> : EBPConfig<TJsonClass>, IModuleConfig where TJsonClass : ModuleConfigJsonClass, new()
    {
        public ModuleConfigJsonClass BaseJson { get { return Json; } }
        public string WorkPath { get { return Path.Combine(CommonModule.CommonConfig.DataRootPath, Json.WorkRelativePath); } }
        public string StateConfigPath { get { return Path.Combine(WorkPath, Json.StateConfigRelativePath); } }
        public abstract string UserConfigsFolderPath { get; }
        public string ModuleRootPath { get { return Path.GetDirectoryName(JsonPath); } }
    }

    [Serializable]
    public class ModuleConfigJsonClass
    {
        public string WorkRelativePath;
        public string StateConfigRelativePath = "_Configs/State.json";
    }

    public interface IModuleStateConfig
    {
        string JsonPath { get; set; }
        string CurrentUserConfigPath { get; }
        ModuleStateConfigJsonClass BaseJson { get; }
    }

    [Serializable]
    public class ModuleStateConfig<TJsonClass> : EBPConfig<TJsonClass>, IModuleStateConfig where TJsonClass : ModuleStateConfigJsonClass, new()
    {
        public ModuleStateConfigJsonClass BaseJson { get { return Json; } }
        public string UserConfigsFolderPath;
        public string CurrentUserConfigPath
        {
            get
            {
                if (string.IsNullOrEmpty(UserConfigsFolderPath) || string.IsNullOrEmpty(Json.CurrentUserConfigName))
                    return null;
                return Path.Combine(UserConfigsFolderPath, Json.CurrentUserConfigName);
            }
        }
    }

    [Serializable]
    public class ModuleStateConfigJsonClass
    {
        public string[] CurrentTag = { };
        public string CurrentUserConfigName;
        public bool Applying;
        public bool IsPartOfPipeline;
        public string ErrorMessage;
        public string DetailedErrorMessage;
    }
}

namespace EazyBuildPipeline
{
    /// <summary> EazyBuildPipeline配置基类 </summary>
    /// <typeparam name="TJson">与Json文件对应的配置类</typeparam>
    [Serializable]
    public class EBPConfig<TJson> where TJson : new()
    {
        public TJson Json { get { return json; } set { json = value; } }
        [SerializeField] TJson json = new TJson();
        public string JsonPath { get { return jsonPath; } set { jsonPath = value; } }
        [SerializeField] string jsonPath;

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