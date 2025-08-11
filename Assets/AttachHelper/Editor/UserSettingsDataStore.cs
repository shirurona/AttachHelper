using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AttachHelper.Editor
{
    public class UserSettingsDataStore
    {
        public static void ClearData()
        {
            if (string.IsNullOrEmpty(EditorUserSettings.GetConfigValue("ignoreCount")))
            {
                return;
            }
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            for (int i = 0; i < ignoreCount; i++)
            {
                EditorUserSettings.SetConfigValue($"globalObjectIdString{i}", null);
                EditorUserSettings.SetConfigValue($"propertyPath{i}", null);
            }
            EditorUserSettings.SetConfigValue("ignoreCount", null);
        }
        
        public static HashSet<UniquePropertyInfo> RestoreData()
        {
            if (string.IsNullOrEmpty(EditorUserSettings.GetConfigValue("ignoreCount")))
            {
                EditorUserSettings.SetConfigValue("ignoreCount", "0");
            }
            
            HashSet<UniquePropertyInfo> ignores = new HashSet<UniquePropertyInfo>(new PropertyComparer());
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            for (int i = 0; i < ignoreCount; i++)
            {
                string globalObjectId = EditorUserSettings.GetConfigValue($"globalObjectIdString{i}");
                string propertyPath = EditorUserSettings.GetConfigValue($"propertyPath{i}");
                ignores.Add(new UniquePropertyInfo(globalObjectId, propertyPath));
            }
            return ignores;
        }
            
        public static void AddIgnore(UniquePropertyInfo uniquePropertyInfo)
        {
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            EditorUserSettings.SetConfigValue($"globalObjectIdString{ignoreCount}", uniquePropertyInfo.GlobalObjectIdString);
            EditorUserSettings.SetConfigValue($"propertyPath{ignoreCount}", uniquePropertyInfo.PropertyPath);
            EditorUserSettings.SetConfigValue("ignoreCount", (ignoreCount + 1).ToString());
        }
    }
}