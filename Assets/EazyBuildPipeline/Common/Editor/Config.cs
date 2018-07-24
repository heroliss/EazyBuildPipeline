using UnityEditor;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace EazyBuildPipeline.Common.Editor
{
    /// <summary>
    /// 公共EazyBuildPipeline配置集
    /// 其中各种Config类继承自EBPConfig类，这种继承本可以省略，直接产生一个 EBPConfig<JsonClass> 即可，
    /// 但考虑两点：
    /// 1.最终产生的EBPConfig对象中的Json对象需要一个初始化值，封装到一个子Config类中容易初始化
    /// 2.Unity序列化不支持对泛型类的序列化，Config子类可以消除EBPConfig的泛型特性
    /// 另外：
    /// EBPConfig的Load和Save函数不使用Unity内置序列化工具是为了对字典等类型的序列化保存到文件时有更好看的字符串结果
    /// (由于Unity内置序列化工具不支持字典，所以使用Unity的JsonUtility序列化字典只能变为序列化两个List)
    /// </summary>
    [Serializable]
    public class EBPConfigs
    {
        public virtual string ModuleName { get { return "EazyBuildPipeline"; } }
        private readonly string commonConfigSearchText = "EazyBuildPipeline CommonConfig";
        public CommonLocalConfig Common_LocalConfig = new CommonLocalConfig();
        public CommonTagEnumConfig Common_TagEnumConfig = new CommonTagEnumConfig();
        public CommonAssetsTagsConfig Common_AssetsTagsConfig = new CommonAssetsTagsConfig();

        public bool LoadCommonAssetsTagsConfig()
        {
            try
            {
                Common_AssetsTagsConfig.JsonPath = Common_LocalConfig.AssetsTagsConfigPath;
                Common_AssetsTagsConfig.Load();
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载公共AssetsTags文件时发生错误：" + e.Message
                    + "\n加载路径：" + Common_AssetsTagsConfig.JsonPath
                    + "\n请正确设置形如以下所示的配置文件：\n" + Common_AssetsTagsConfig.ToString());
                return false;
            }
        }

        public bool LoadCommonTagEnumConfig()
        {
            try
            {
                Common_TagEnumConfig.JsonPath = Common_LocalConfig.TagEnumConfigPath;
                Common_TagEnumConfig.Load();
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载公共枚举配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + Common_TagEnumConfig.JsonPath
                    + "\n请正确设置形如以下所示的配置文件：\n" + Common_TagEnumConfig.ToString());
                return false;
            }
        }

        public bool LoadCommonLocalConfig()
        {
            try
            {
                string[] guids = AssetDatabase.FindAssets(commonConfigSearchText);
                if (guids.Length == 0)
                {
                    throw new ApplicationException("未能找到公共本地配置文件! 搜索文本：" + commonConfigSearchText);
                }
                Common_LocalConfig.JsonPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                Common_LocalConfig.LocalRootPath = Path.GetDirectoryName(Common_LocalConfig.JsonPath);
                Common_LocalConfig.Load();
                return true;
            }
            catch (Exception e)
            {
                DisplayDialog("加载公共本地配置文件时发生错误：" + e.Message
                    + "\n加载路径：" + Common_LocalConfig.JsonPath
                    + "\n请设置正确的文件名以及形如以下所示的配置文件：\n" + Common_LocalConfig.ToString());
                return false;
            }
        }

        [Serializable]
        public class CommonLocalConfig : EBPConfig<CommonLocalConfig.JsonClass>
        {
            public CommonLocalConfig()
            {
                Json = new JsonClass();
            }
            public string TagEnumConfigPath { get { return Path.Combine(LocalRootPath, Json.TagEnumConfigRelativePath); } }
            public string AssetsTagsConfigPath { get { return Path.Combine(LocalRootPath, Json.AssetsTagsConfigRelativePath); } }
            public string BundleIconPath { get { return Path.Combine(LocalRootPath, Json.BundleIconRelativePath); } }
            public string BundleIcon_SceneConfigPath { get { return Path.Combine(LocalRootPath, Json.BundleIcon_SceneRelativePath); } }
            public string PackageIconPath { get { return Path.Combine(LocalRootPath, Json.PackageIconRelativePath); } }
            public string SettingIconPath { get { return Path.Combine(LocalRootPath, Json.SettingIconRelativePath); } }
            public string LocalRootPath;
            [Serializable]
            public class JsonClass
            {
                public string TagEnumConfigRelativePath;
                public string AssetsTagsConfigRelativePath;
                public string BundleIconRelativePath;
                public string BundleIcon_SceneRelativePath;
                public string PackageIconRelativePath;
                public string SettingIconRelativePath;
            }
        }

        [Serializable]
        public class CommonTagEnumConfig : EBPConfig<Dictionary<string,string[]>>, ISerializationCallbackReceiver
        {
            [SerializeField] private List<TageEnumClass> tags = new List<TageEnumClass>();
            public Dictionary<string,string[]> Tags { get { return Json; } }
            public CommonTagEnumConfig()
            {
                Json = new Dictionary<string,string[]>()
                {
                    { "Example Group 1:",new string[]{"example tag1","example tag2","example tag3"} },
                    { "Example Group 2:",new string[]{"example tag a","example tag b"} },
                };
            }
            public void OnBeforeSerialize()
            {
                tags.Clear();
                tags.Capacity = Json.Count;
                foreach (var item in Json)
                {
                    tags.Add(new TageEnumClass() { type = item.Key, tags = item.Value });
                }
            }
            public void OnAfterDeserialize()
            {
                Json.Clear();
                for (int i = 0; i < tags.Count; ++i)
                {
                    Json.Add(tags[i].type, tags[i].tags);
                }
            }
            [Serializable]
            public class TageEnumClass
            {
                public string type;
                public string[] tags;
            }
        }

        [Serializable]
        public class CommonAssetsTagsConfig : EBPConfig<string[]>
        {
            public CommonAssetsTagsConfig()
            {
                Json = new string[] { "Example Tag1", "Example Tag2" };
            }
        }

        public void DisplayDialog(string text)
        {
            EditorUtility.DisplayDialog(ModuleName, text, "确定");
        }
    }

    [Serializable]
    public class EBPConfig<TJson>
    {
        public TJson Json;
        public string JsonPath;

        public virtual void Load(string path = null)
        {
            Json = JsonConvert.DeserializeObject<TJson>(File.ReadAllText(path ?? JsonPath));
        }
        public virtual void Save(string path = null)
        {
            File.WriteAllText(path ?? JsonPath, JsonConvert.SerializeObject(Json, Formatting.Indented));
        }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(Json, Formatting.Indented);
        }
    }
}