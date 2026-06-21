using System;
using System.Collections.Generic;
using Dessentials.Serializables;
using UnityEditor;
using UnityEngine;

namespace Dessentials.Features.ConvertJsonToScriptableObjects
{
    public class ConvertJsonToScriptableObjectsWindow : EditorWindow
    {
        private TextAsset[] _textAssets;
        private Type[] _eligibleTypes;
        private string[] _typeDisplayNames;
        private int _selectedTypeIndex;
        private Vector2 _scrollPosition;

        [SerializeField] private FolderReference _outputFolder;

        private SerializedObject _serializedObject;
        private SerializedProperty _outputFolderProperty;

        [MenuItem("Assets/Convert JSON to Scriptable Objects", false, 50)]
        private static void OpenFromContextMenu()
        {
            var window = GetWindow<ConvertJsonToScriptableObjectsWindow>("Convert JSON to SO");
            window._textAssets = GetSelectedTextAssets();
            window.Show();
        }

        [MenuItem("Assets/Convert JSON to Scriptable Objects", true)]
        private static bool ValidateOpenFromContextMenu()
        {
            foreach (var obj in Selection.objects)
                if (obj is TextAsset)
                    return true;
            return false;
        }

        private static TextAsset[] GetSelectedTextAssets()
        {
            var results = new List<TextAsset>();
            foreach (var obj in Selection.objects)
                if (obj is TextAsset textAsset)
                    results.Add(textAsset);
            return results.ToArray();
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _outputFolderProperty = _serializedObject.FindProperty("_outputFolder");

            CacheEligibleTypes();
        }

        private void CacheEligibleTypes()
        {
            var soTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>();
            var importType = typeof(IImportFromJson);

            var eligible = new List<Type>();
            foreach (var type in soTypes)
            {
                if (type.IsAbstract || type.IsGenericType)
                    continue;
                if (importType.IsAssignableFrom(type))
                    eligible.Add(type);
            }

            _eligibleTypes = eligible.ToArray();

            _typeDisplayNames = new string[_eligibleTypes.Length];
            for (int i = 0; i < _eligibleTypes.Length; i++)
                _typeDisplayNames[i] = _eligibleTypes[i].Name;
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            if (_textAssets == null || _textAssets.Length == 0)
            {
                EditorGUILayout.HelpBox("No TextAssets selected.", MessageType.Warning);
                return;
            }

            // Selected TextAssets
            EditorGUILayout.LabelField("Selected TextAssets", EditorStyles.boldLabel);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(200));
            foreach (var textAsset in _textAssets)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(textAsset, typeof(TextAsset), false);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            // Type dropdown
            if (_eligibleTypes.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No ScriptableObject types implementing IImportFromJson found.",
                    MessageType.Error);
            }
            else
            {
                _selectedTypeIndex = EditorGUILayout.Popup("Target Type", _selectedTypeIndex, _typeDisplayNames);
            }

            EditorGUILayout.Space(4);

            // Output folder
            EditorGUILayout.PropertyField(_outputFolderProperty, new GUIContent("Output Folder"));

            _serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            // Convert button
            bool canConvert = _eligibleTypes.Length > 0
                              && _outputFolder != null
                              && _outputFolder.IsValid;

            EditorGUI.BeginDisabledGroup(!canConvert);
            if (GUILayout.Button("Convert", GUILayout.Height(30)))
                Convert();
            EditorGUI.EndDisabledGroup();
        }

        private void Convert()
        {
            var targetType = _eligibleTypes[_selectedTypeIndex];
            string folderPath = _outputFolder.Path;

            try
            {
                for (int i = 0; i < _textAssets.Length; i++)
                {
                    var textAsset = _textAssets[i];
                    if (textAsset == null) continue;

                    EditorUtility.DisplayProgressBar(
                        "Converting JSON to ScriptableObject",
                        textAsset.name,
                        (float)i / _textAssets.Length);

                    var instance = CreateInstance(targetType);
                    ((IImportFromJson)instance).ImportFromJson(textAsset.text);

                    string assetPath = $"{folderPath}/{textAsset.name}.asset";
                    AssetDatabase.CreateAsset(instance, assetPath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Converted {_textAssets.Length} JSON file(s) to {targetType.Name} in {folderPath}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
