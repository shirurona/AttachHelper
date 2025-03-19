using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AttachHelper.Editor
{
    [FilePath("ProjectSettings/AttachHelperEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class AttachHelperEditorSettings : ScriptableSingleton<AttachHelperEditorSettings>
    {
        public static readonly string UserScriptLabel = "User Created Scripts";
        /// <summary>
        /// AttachHelperの対象として有効にするコンポーネント with MonoBehaviourスクリプト
        /// </summary>
        /// <remarks>
        /// 本当はHashSetを使いたかったが、HashSetはデフォルトでシリアライズできないのでDictionaryで代用
        /// </remarks>
        [SerializeField] private SerializedDictionary<string, bool> set = new SerializedDictionary<string, bool>()
        {
            {UserScriptLabel, true}
        };

        public bool GetValue(string key)
        {
            return set.ContainsKey(key) && set[key];
        }

        public void ChangeValue(string key, bool value)
        {
            set[key] = value;
        }

        public void ChangeValueAll(bool value)
        {
            string[] keys = set.Keys.ToArray();
            foreach (var key in keys)
            {
                set[key] = value;
            }
        }
        
        public void Save()
        {
            Save(true);
        }
    }
}
