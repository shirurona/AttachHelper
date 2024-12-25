using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AttachHelper.Editor
{
    public class AttachHelper : EditorWindow
    {
        public class UniqueProperty
        {
            public string GlobalObjectIdString;
            public SerializedProperty Property;
        
            public UniqueProperty(string globalObjectIdString, SerializedProperty property)
            {
                GlobalObjectIdString = globalObjectIdString;
                Property = property.Copy();
            }
        }
    
        /// <summary>
        /// Noneなやつ
        /// </summary>
        static List<UniqueProperty> show = new();
    
        /// <summary>
        /// showに登録されたやつを検索するためのやつ
        /// </summary>
        private static HashSet<string> showcomp = new HashSet<string>();
    
        /// <summary>
        /// Noneだけどそれでいいから無視するやつ。Noneじゃなくなってもそのまま。
        /// </summary>
        private static HashSet<string> ignores = new HashSet<string>();

        private Vector2 scrollPosition = Vector2.zero;
    
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorSceneManager.sceneOpened += (_, _) =>
            {
                show.Clear();
                showcomp.Clear();
                RestoreData();
                RegisterSerializeNone();
            };
            
            RestoreData();
            RegisterSerializeNone();
            
            if (HasOpenInstances<AttachHelper>()) {
                FocusWindowIfItsOpen<AttachHelper>();
            }
            else
            {
                if (!IsShowAny()) return;
                AttachHelper window = GetWindow<AttachHelper>();
                window.Show();
            }
        }

        static bool IsShowAny()
        {
            foreach (UniqueProperty serializedObj in show)
            {
                if (ignores.Contains(serializedObj.GlobalObjectIdString)) continue;
                return true;
            }
            return false;
        }
    
        [MenuItem("AttachHelper/Check")]
        public static void ShowWindow()
        {
            RestoreData();
            RegisterSerializeNone();
            if (HasOpenInstances<AttachHelper>()) {
                FocusWindowIfItsOpen<AttachHelper>();
            }
            else
            {
                AttachHelper window = GetWindow<AttachHelper>();
                window.Show();
            }
        }
    
        [MenuItem("AttachHelper/Reset")]
        public static void Clear()
        {
            show.Clear();
            showcomp.Clear();
            ignores.Clear();
            ClearData();
        }

        private static void ClearData()
        {
            if (string.IsNullOrEmpty(EditorUserSettings.GetConfigValue("ignoreCount")))
            {
                return;
            }
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            for (int i = 0; i < ignoreCount; i++)
            {
                EditorUserSettings.SetConfigValue($"globalObjectIdString{i}", null);
            }
            EditorUserSettings.SetConfigValue("ignoreCount", null);
        }
    
        private static void RestoreData()
        {
            if (string.IsNullOrEmpty(EditorUserSettings.GetConfigValue("ignoreCount")))
            {
                EditorUserSettings.SetConfigValue("ignoreCount", "0");
            }
        
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            for (int i = 0; i < ignoreCount; i++)
            {
                string globalObjectId = EditorUserSettings.GetConfigValue($"globalObjectIdString{i}");
                ignores.Add(globalObjectId);
            }
        }
        
        private static void AddIgnore(string uniquePropertyInfo)
        {
            ignores.Add(uniquePropertyInfo);
        
            int ignoreCount = int.Parse(EditorUserSettings.GetConfigValue("ignoreCount"));
            EditorUserSettings.SetConfigValue($"globalObjectIdString{ignoreCount}", uniquePropertyInfo);
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
                        GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(component);
                        string globalObjectIdString = globalObjectId.ToString();
                        if (serializedProp.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (serializedProp.objectReferenceValue != null) continue;
                        if (showcomp.Contains(globalObjectIdString)) continue;

                        show.Add(new UniqueProperty(globalObjectIdString, serializedProp));
                        showcomp.Add(globalObjectIdString);
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
            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, false, false))
            {
                scrollPosition = scrollViewScope.scrollPosition;
                foreach (var serializedObj in show)
                {
                    if (ignores.Contains(serializedObj.GlobalObjectIdString)) continue;
                    if (GlobalObjectId.TryParse(serializedObj.GlobalObjectIdString, out GlobalObjectId globalObjectId))
                    {
                        var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
                        if (obj == null) continue;
                        var serializedProp = serializedObj.Property;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            Component component = obj as Component;
                            if (component is null)
                            {
                                throw new NullReferenceException();
                            }
                            GameObject gameObj = component.gameObject;
                            if (GUILayout.Button("Inspect", GUILayout.Width(100)))
                            {
                                Selection.activeGameObject = gameObj;
                            }

                            GUILayout.Label($"{gameObj.name} > {component.GetType()} > {serializedProp.displayName}", GUILayout.MinWidth(200));

                            GUILayout.FlexibleSpace();
                            EditorGUILayout.PropertyField(serializedProp, new GUIContent(GUIContent.none), true,
                                GUILayout.MinWidth(150), GUILayout.MaxWidth(200), GUILayout.ExpandWidth(false));
                            serializedProp.serializedObject.ApplyModifiedProperties();
                            if (GUILayout.Button("Decide", GUILayout.Width(100)))
                            {
                                AddIgnore(serializedObj.GlobalObjectIdString);
                                AssetDatabase.SaveAssets();
                            }
                        }
                    }
                }
            }
        
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Decide All None"))
                {
                    foreach (var serializedObj in show)
                    {
                        if (ignores.Contains(serializedObj.GlobalObjectIdString)) continue;
                        var serializedProp = serializedObj.Property;
                        if (serializedProp.objectReferenceValue != null) continue;
                        AddIgnore(serializedObj.GlobalObjectIdString);
                    }
                
                    AssetDatabase.SaveAssets();
                }
            
                if (GUILayout.Button("Decide All Attached"))
                {
                    foreach (var serializedObj in show)
                    {
                        if (ignores.Contains(serializedObj.GlobalObjectIdString)) continue;
                        var serializedProp = serializedObj.Property;
                        if (serializedProp.objectReferenceValue == null) continue;
                        AddIgnore(serializedObj.GlobalObjectIdString);
                    }
                
                    AssetDatabase.SaveAssets();
                }
            }
        
            if (GUILayout.Button("Decide All", GUILayout.Height(40)))
            {
                foreach (var serializedObj in show)
                {
                    if (ignores.Contains(serializedObj.GlobalObjectIdString)) continue;
                
                    AddIgnore(serializedObj.GlobalObjectIdString);
                }
            
                AssetDatabase.SaveAssets();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close"))
            {
                Close();
            }
        }
    }
}