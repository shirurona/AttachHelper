using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AttachHelper.Editor
{
    public class AttachHelperSettingsProvider : SettingsProvider
    {
        private static SortedSet<string> sortedSet = new SortedSet<string>();
        private static Dictionary<string, string> dictionary = new Dictionary<string, string>();
        private static HashSet<string> set = new HashSet<string>();

        private static HashSet<string> Categories
        {
            get
            {
                if (_categories is null)
                {
                    _categories = new HashSet<string>();
                    foreach (var title in sortedSet)
                    {
                        string[] labels = title.Split('/');
                        if (!_categories.Contains(labels[0]))
                        {
                            _categories.Add(labels[0]);
                        }
                        if (labels.Length == 3)
                        {
                            if (!_categories.Contains(labels[1]))
                            {
                                _categories.Add(labels[1]);
                            }
                        }
                    }
                }

                return _categories;
            }
        }
        private static HashSet<string> _categories = null;
        private const string SettingPath = "AttachHelper";
        
        private string categoryLarge = string.Empty;
        private string categoryMedium = string.Empty;
        
        private AttachHelperSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }
        
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            CreateDictionary();
            return new AttachHelperSettingsProvider(SettingPath, SettingsScope.Project);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel = 0;
            foreach (string title in sortedSet)
            {
                string[] labels = title.Split('/');
                if (categoryLarge != labels[0])
                {
                    if (!string.IsNullOrEmpty(categoryLarge))
                    {
                        EditorGUILayout.EndFoldoutHeaderGroup();
                    }
                    
                    if (EditorGUILayout.BeginFoldoutHeaderGroup(set.Contains(labels[0]), labels[0]))
                    {
                        set.Add(labels[0]);
                    }
                    else
                    {
                        set.Remove(labels[0]);
                    }
                    categoryLarge = labels[0];
                }

                if (labels.Length == 3)
                {
                    if (categoryMedium != labels[1])
                    {
                        if (set.Contains(labels[0]))
                        {
                            EditorGUI.indentLevel++;
                            ShowFoldout(labels[1]);
                            EditorGUI.indentLevel--;
                        }

                        categoryMedium = labels[1];
                    }

                    if (set.Contains(labels[0]) && set.Contains(labels[1]))
                    {
                        EditorGUI.indentLevel+=2;
                        ShowToggle(labels[^1]);
                        EditorGUI.indentLevel-=2;
                    }
                }
                else if (labels.Length == 2 && set.Contains(labels[0]))
                {
                    EditorGUI.indentLevel++;
                    ShowToggle(labels[^1]);
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            ShowToggle(AttachHelperEditorSettings.UserScriptLabel);
            if (GUILayout.Button("Set All On"))
            {
                OpenAllFoldout();
                AttachHelperEditorSettings.instance.ChangeValueAll(true);
            }
            if (GUILayout.Button("Set All Off"))
            {
                OpenAllFoldout();
                AttachHelperEditorSettings.instance.ChangeValueAll(false);
            }
            if (GUILayout.Button("Set Default"))
            {
                OpenAllFoldout();
                AttachHelperEditorSettings.instance.ChangeValueAll(false);
                AttachHelperEditorSettings.instance.ChangeValue(AttachHelperEditorSettings.UserScriptLabel, true);
            }
            if (EditorGUI.EndChangeCheck())
            {
                AttachHelperEditorSettings.instance.Save();
            }
        }

        private static void OpenAllFoldout()
        {
            foreach (string category in Categories)
            {
                if (set.Contains(category)) continue;
                set.Add(category);
            }
        } 

        private static void ShowToggle(string label)
        {
            bool isOn = EditorGUILayout.Toggle(label, AttachHelperEditorSettings.instance.GetValue(dictionary[label]));
            AttachHelperEditorSettings.instance.ChangeValue(dictionary[label], isOn);
        }
        
        private static void ShowFoldout(string label)
        {
            if (EditorGUILayout.Foldout(set.Contains(label), label))
            {
                set.Add(label);
            }
            else
            {
                set.Remove(label);
            }
        }

        private static void CreateDictionary()
        {
            sortedSet.Clear();
            dictionary.Clear();
            var addComponentMenuTypes = TypeCache.GetTypesWithAttribute<AddComponentMenu>();
            foreach (Type componentType in addComponentMenuTypes)
            {
                if (!HasSerializedObjectReferenceField(componentType))
                {
                    continue;
                }
                var attr = componentType.GetCustomAttributes(typeof(AddComponentMenu), false).FirstOrDefault() as AddComponentMenu;
                if (attr == null)
                {
                    continue;
                }
                var title = attr.componentMenu?.Trim();
                if (string.IsNullOrEmpty(title))
                    continue;

                sortedSet.Add(title);
                string[] split = title.Split('/');
                dictionary.Add(split[^1], componentType.ToString());
            }
            dictionary.Add(AttachHelperEditorSettings.UserScriptLabel, AttachHelperEditorSettings.UserScriptLabel);
        }
        
        private static bool HasSerializedObjectReferenceField(Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Any(field =>
                        (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null) &&
                        typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)))
                {
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }
    }
}