using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace EazyBuildPipeline.Common.Editor
{
    public class EBPConfig
    {
        [NonSerialized]
        public string Path;

        public virtual void Load()
        {
            string s = File.ReadAllText(Path);
            EditorJsonUtility.FromJsonOverwrite(s, this);
        }
        public virtual void Save()
        {
            File.WriteAllText(Path, EditorJsonUtility.ToJson(this, true));
        }
        public override string ToString()
        {
            return EditorJsonUtility.ToJson(this, true);
        }
    }
}