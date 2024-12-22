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
        public class PropertyCompararer : IEqualityComparer<UniqueProperty>
        {
            public bool Equals(UniqueProperty x, UniqueProperty y)
            {
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return Equals(x.index, y.index) && Equals(x.propertyPath, y.propertyPath);
            }
        
            public int GetHashCode(UniqueProperty obj)
            {
                return HashCode.Combine(obj.index, obj.propertyPath);
            }
        }
    
        public class UniqueProperty
        {
            public int index;
            public SerializedProperty SerializedProperty;
            public string propertyPath;
        
            public UniqueProperty(Component component, SerializedProperty serializedProperty)
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
            
                SerializedProperty = serializedProperty.Copy();
                propertyPath = serializedProperty.propertyPath;
            }
        
            public UniqueProperty(int index, string propertyPath)
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
        private static Dictionary<UniqueProperty, GameObject> showcomp = new Dictionary<UniqueProperty, GameObject>(new PropertyCompararer());
    
        /// <summary>
        /// Noneだけどそれでいいから無視するやつ。Noneじゃなくなってもそのまま。
        /// </summary>
        private static HashSet<UniqueProperty> ignores = new HashSet<UniqueProperty>(new PropertyCompararer());
    
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            RestoreData();
            RegisterSerializeNone();
            if (!show.Any()) return;
        
            AttachHelper window = GetWindow<AttachHelper>();
            window.ShowPopup();
        }
    
        [MenuItem("AttachHelper/Check")]
        public static void ShowWindow()
        {
            RestoreData();
            RegisterSerializeNone();
            if (!show.Any()) return;
        
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
    
        static void RestoreData()
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
                ignores.Add(new UniqueProperty(index, propertyPath));
            }
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
                        if (showcomp.ContainsKey(uniqueProperty)) continue;
                    
                        show.Add(uniqueProperty);
                        showcomp.Add(uniqueProperty, obj);
                    }
                }
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
                        Selection.activeGameObject = showcomp[serializedObj];
                    }
                    GUILayout.Label($"{showcomp[serializedObj].name} > {showcomp[serializedObj].GetComponents<Component>()[serializedObj.index].GetType()} > {serializedProp.displayName}", GUILayout.ExpandWidth(true));
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
    
        private static void AddIgnore(UniqueProperty uniqueProperty)
        {
            ignores.Add(uniqueProperty);
        
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            EditorUserSettings.SetConfigValue($"propertyPath{ignoreCount}", uniqueProperty.propertyPath);
            EditorUserSettings.SetConfigValue($"index{ignoreCount}", uniqueProperty.index.ToString());
            EditorUserSettings.SetConfigValue("ignoreCount", (ignoreCount + 1).ToString());
        }
    
        private static void FindRecursive(ref List<GameObject> list, GameObject root)
        {
            list.Add(root);
            foreach (Transform child in root.transform)
            {
                FindRecursive(ref list, child.gameObject);
            }
        }
    }
}