using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AttachHelper.Editor
{
    public class AttachHelper : EditorWindow
    {
        public class PropertyCompararer : IEqualityComparer<UniquePropertyInfo>
        {
            public bool Equals(UniquePropertyInfo x, UniquePropertyInfo y)
            {
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return Equals(x.index, y.index) && Equals(x.propertyPath, y.propertyPath);
            }
        
            public int GetHashCode(UniquePropertyInfo obj)
            {
                return HashCode.Combine(obj.index, obj.propertyPath);
            }
        }
        
        public class UniqueProperty : UniquePropertyInfo
        {
            public SerializedProperty SerializedProperty;
            public GameObject GameObject;
        
            public UniqueProperty(Component component, SerializedProperty serializedProperty) : base(component, serializedProperty)
            {
                SerializedProperty = serializedProperty.Copy();
                GameObject = component.gameObject;
            }
        }
    
        public class UniquePropertyInfo
        {
            public int index;
            public string propertyPath;
        
            public UniquePropertyInfo(Component component, SerializedProperty serializedProperty)
            {
                var components = component.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i].Equals(component))
                    {
                        index = i;
                        break;
                    }
                }
                propertyPath = serializedProperty.propertyPath;
            }
        
            public UniquePropertyInfo(int index, string propertyPath)
            {
                this.index = index;
                this.propertyPath = propertyPath;
            }
        }
    
        /// <summary>
        /// Noneなやつ
        /// </summary>
        static List<UniqueProperty> show = new();
    
        /// <summary>
        /// showに登録されたやつを検索するためのやつ
        /// </summary>
        private static HashSet<UniquePropertyInfo> showcomp = new HashSet<UniquePropertyInfo>(new PropertyCompararer());
    
        /// <summary>
        /// Noneだけどそれでいいから無視するやつ。Noneじゃなくなってもそのまま。
        /// </summary>
        private static HashSet<UniquePropertyInfo> ignores = new HashSet<UniquePropertyInfo>(new PropertyCompararer());
    
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            RestoreData();
            RegisterSerializeNone();
            
            if (!IsShowAny()) return;
            AttachHelper window = GetWindow<AttachHelper>();
            window.ShowPopup();
        }

        static bool IsShowAny()
        {
            foreach (UniqueProperty serializedObj in show)
            {
                if (ignores.Contains(serializedObj)) continue;
                return true;
            }
            return false;
        }
    
        [MenuItem("AttachHelper/Check")]
        public static void ShowWindow()
        {
            RestoreData();
            RegisterSerializeNone();
            AttachHelper window = GetWindow<AttachHelper>();
            window.Show();
        }
    
        [MenuItem("AttachHelper/Reset")]
        public static void Clear()
        {
            show.Clear();
            showcomp.Clear();
            ignores.Clear();
        }
    
        private static void RestoreData()
        {
            if (EditorUserSettings.GetConfigValue("ignoreCount") is null)
            {
                EditorUserSettings.SetConfigValue("ignoreCount", "0");
            }
        
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            for (int i = 0; i < ignoreCount; i++)
            {
                string propertyPath = EditorUserSettings.GetConfigValue($"propertyPath{i}");
                int index = int.Parse(EditorUserSettings.GetConfigValue($"index{i}"));
                ignores.Add(new UniquePropertyInfo(index, propertyPath));
            }
        }
        
        private static void AddIgnore(UniquePropertyInfo uniquePropertyInfo)
        {
            ignores.Add(uniquePropertyInfo);
        
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            EditorUserSettings.SetConfigValue($"propertyPath{ignoreCount}", uniquePropertyInfo.propertyPath);
            EditorUserSettings.SetConfigValue($"index{ignoreCount}", uniquePropertyInfo.index.ToString());
            EditorUserSettings.SetConfigValue("ignoreCount", (ignoreCount + 1).ToString());
        }
    
        static void RegisterSerializeNone()
        {
            var objs = new List<GameObject>();
            var scene = SceneManager.GetActiveScene();
            foreach (var obj in scene.GetRootGameObjects())
            {
                FindRecursive(ref objs, obj);
            }
        
            foreach (var obj in objs)
            {
                Component[] components = obj.GetComponents<Component>();
                foreach (Component component in components)
                {
                    if (component == null) continue;
                
                    var serializedObj = new SerializedObject(component);
                
                    var serializedProp = serializedObj.GetIterator();
                    while (serializedProp.NextVisible(true))
                    {
                        var uniqueProperty = new UniqueProperty(component, serializedProp);
                        if (serializedProp.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (serializedProp.objectReferenceValue != null) continue;
                        if (showcomp.Contains(uniqueProperty)) continue;
                        
                        show.Add(uniqueProperty);
                        showcomp.Add(uniqueProperty);
                    }
                }
            }
        }
        
        private static void FindRecursive(ref List<GameObject> list, GameObject root)
        {
            list.Add(root);
            foreach (Transform child in root.transform)
            {
                FindRecursive(ref list, child.gameObject);
            }
        }
    
        void OnGUI()
        {
            EditorGUILayout.BeginScrollView(Vector2.zero, false, false);
            foreach (var serializedObj in show)
            {
                if (ignores.Contains(serializedObj)) continue;
                var serializedProp = serializedObj.SerializedProperty;
            
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Inspect", GUILayout.Width(100)))
                    {
                        Selection.activeGameObject = serializedObj.GameObject;
                    }
                    GUILayout.Label($"{serializedObj.GameObject.name} > {serializedObj.GameObject.GetComponents<Component>()[serializedObj.index].GetType()} > {serializedProp.displayName}", GUILayout.ExpandWidth(true));
                    EditorGUILayout.PropertyField(serializedProp, new GUIContent(GUIContent.none), true, GUILayout.MinWidth(55), GUILayout.ExpandWidth(false));
                    serializedProp.serializedObject.ApplyModifiedProperties();
                    if (GUILayout.Button("Decide", GUILayout.Width(100)))
                    {
                        AddIgnore(serializedObj);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Decide All None"))
                {
                    foreach (var serializedObj in show)
                    {
                        if (ignores.Contains(serializedObj)) continue;
                        var serializedProp = serializedObj.SerializedProperty;
                        if (serializedProp.objectReferenceValue != null) continue;
                        AddIgnore(serializedObj);
                    }
                
                    AssetDatabase.SaveAssets();
                }
            
                if (GUILayout.Button("Decide All Attached"))
                {
                    foreach (var serializedObj in show)
                    {
                        if (ignores.Contains(serializedObj)) continue;
                        var serializedProp = serializedObj.SerializedProperty;
                        if (serializedProp.objectReferenceValue == null) continue;
                        AddIgnore(serializedObj);
                    }
                
                    AssetDatabase.SaveAssets();
                }
            }
        
            if (GUILayout.Button("Decide All"))
            {
                foreach (var serializedObj in show)
                {
                    if (ignores.Contains(serializedObj)) continue;
                
                    AddIgnore(serializedObj);
                }
            
                AssetDatabase.SaveAssets();
            }
        }
    }
}