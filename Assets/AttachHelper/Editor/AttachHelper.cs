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
        /// <summary>
        /// Noneなやつ
        /// </summary>
        static List<UniqueProperty> show = new();
    
        /// <summary>
        /// showに登録されたやつを検索するためのやつ
        /// </summary>
        private static HashSet<UniquePropertyInfo> showcomp = new HashSet<UniquePropertyInfo>(new PropertyComparer());
    
        /// <summary>
        /// Noneだけどそれでいいから無視するやつ。Noneじゃなくなってもそのまま。
        /// </summary>
        private static HashSet<UniquePropertyInfo> ignores;

        private Vector2 _scrollPosition = Vector2.zero;
    
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
                if (ignores.Contains(serializedObj)) continue;
                if (GlobalObjectId.TryParse(serializedObj.GlobalObjectIdString, out GlobalObjectId parsedId))
                {
                    UnityEngine.Object obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(parsedId);
                    Component component = obj as Component;
                    if (component is null)
                    {
                        throw new NullReferenceException();
                    }
                        
                    // UnityでAddComponentできるものはSettingsを見て判定
                    // ユーザー作成スクリプト（UnityEngine名前空間以外）は勝手にdictionaryを参照されて勝手にfalseをもらっては困るので上と下で分けて判定
                    if (component.GetType().ToString().StartsWith("UnityEngine") && !AttachHelperEditorSettings.instance.GetValue(component.GetType().ToString())) continue;
                    if (!component.GetType().ToString().StartsWith("UnityEngine") && !AttachHelperEditorSettings.instance.GetValue(AttachHelperEditorSettings.UserScriptLabel)) continue;
                }
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

        static void RestoreData()
        {
            ignores = UserSettingsDataStore.RestoreData();
        }
    
        [MenuItem("AttachHelper/Reset")]
        public static void Clear()
        {
            show.Clear();
            showcomp.Clear();
            ignores.Clear();
            UserSettingsDataStore.ClearData();
        }
    
        static void RegisterSerializeNone()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var obj in scene.GetRootGameObjects())
            {
                FindRecursive(obj);
            }
        }
        
        private static void FindRecursive(GameObject root)
        {
            AddShowSerializeNone(root);
            
            foreach (Transform child in root.transform)
            {
                FindRecursive(child.gameObject);
            }
        }

        static void AddShowSerializeNone(GameObject obj)
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
                    UniqueProperty uniqueProperty = new UniqueProperty(globalObjectIdString, serializedProp);
                    if (serializedProp.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (serializedProp.objectReferenceValue != null) continue;
                    if (showcomp.Contains(uniqueProperty)) continue;

                    show.Add(uniqueProperty);
                    showcomp.Add(uniqueProperty);
                }
            }
        }

        void OnGUI()
        {
            int scrollContentCount = 0;
            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_scrollPosition, false, false))
            {
                _scrollPosition = scrollViewScope.scrollPosition;
                List<UniqueProperty> uniqueProperties = new List<UniqueProperty>();
                List<GlobalObjectId> globalObjectIdList = new List<GlobalObjectId>();
                foreach (var serializedObj in show)
                {
                    if (ignores.Contains(serializedObj)) continue;

                    uniqueProperties.Add(serializedObj);
                    if (GlobalObjectId.TryParse(serializedObj.GlobalObjectIdString, out GlobalObjectId globalObjectId))
                    {
                        globalObjectIdList.Add(globalObjectId);
                    }
                }

                var objs = new UnityEngine.Object[globalObjectIdList.Count];
                GlobalObjectId.GlobalObjectIdentifiersToObjectsSlow(globalObjectIdList.ToArray(), objs);

                for (int i = 0; i < uniqueProperties.Count; i++)
                {
                    var serializedObj = uniqueProperties[i];
                    if (objs[i] == null)
                    {
                        ignores.Remove(serializedObj);
                        continue;
                    }

                    var serializedProp = serializedObj.SerializedProperty;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        Component component = objs[i] as Component;
                        if (component is null)
                        {
                            throw new NullReferenceException();
                        }
                        
                        // UnityでAddComponentできるものはSettingsを見て判定
                        // ユーザー作成スクリプト（UnityEngine名前空間以外）は勝手にdictionaryを参照されて勝手にfalseをもらっては困るので上と下で分けて判定
                        if (!IsUserCreated(component) && !AttachHelperEditorSettings.instance.GetValue(component.GetType().ToString())) continue;
                        if (IsUserCreated(component) && !AttachHelperEditorSettings.instance.GetValue(AttachHelperEditorSettings.UserScriptLabel)) continue;
                        
                        GameObject gameObj = component.gameObject;
                        if (GUILayout.Button("Inspect", GUILayout.Width(100)))
                        {
                            Selection.activeGameObject = gameObj;
                        }

                        GUILayout.Label($"{gameObj.name} > {component.GetType()} > {serializedProp.displayName}",
                            GUILayout.MinWidth(200));

                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(serializedProp, new GUIContent(GUIContent.none), true,
                            GUILayout.MinWidth(150), GUILayout.MaxWidth(200), GUILayout.ExpandWidth(false));
                        serializedProp.serializedObject.ApplyModifiedProperties();
                        if (GUILayout.Button("Decide", GUILayout.Width(100)))
                        {
                            AddIgnore(serializedObj);
                            AssetDatabase.SaveAssets();
                        }
                        scrollContentCount++;
                    }
                }
            }

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

            if (GUILayout.Button("Decide All", GUILayout.Height(40)))
            {
                foreach (var serializedObj in show)
                {
                    if (ignores.Contains(serializedObj)) continue;

                    AddIgnore(serializedObj);
                }

                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("Close"))
            {
                Close();
            }
            minSize = new Vector2(minSize.x, 87.5f);
            maxSize = new Vector2(maxSize.x, scrollContentCount * 21.1f + 87.5f);
            GUILayout.FlexibleSpace();
        }

        private static void AddIgnore(UniquePropertyInfo uniquePropertyInfo)
        {
            ignores.Add(uniquePropertyInfo);
            UserSettingsDataStore.AddIgnore(uniquePropertyInfo);
        }

        private static bool IsUserCreated(Component component)
        {
            if (component == null) return false;

            MonoScript script = null;
            if (component is MonoBehaviour mono)
            {
                script = MonoScript.FromMonoBehaviour(mono);
            }
            if (script == null) return false;
            
            string assetPath = AssetDatabase.GetAssetPath(script);
            if (string.IsNullOrEmpty(assetPath)) return false;
            
            return assetPath.StartsWith("Assets/");
        }
    }
}