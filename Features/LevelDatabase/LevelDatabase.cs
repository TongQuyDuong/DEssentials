using System;
using System.Collections.Generic;
using System.Linq;
using Dessentials.Common;
using Dessentials.Common.GlobalServices;
using Dessentials.Extensions;
using Dessentials.Serializables;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Dessentials.Features.LevelDatabase
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "Dessentials/LevelDatabase")]
    public class LevelDatabase<TLeveScriptableObject> : ScriptableObject
        where TLeveScriptableObject : ScriptableObject
    {
        [SerializeField]
        private SerializableDictionary<string, TLeveScriptableObject> _levelsDictionary = new();

        [SerializeField]
        private TLeveScriptableObject[] _backupLevels;

        public TLeveScriptableObject GetLevelByName(string levelName)
        {
            if (_levelsDictionary.ContainsKey(levelName))
            {
                return _levelsDictionary[levelName];
            }
            else
            {
                DFirebaseCrashlytics.Log($"Requesting non-existent level name : {levelName}");

                return GetFallbackLevel();
            }
        }

        public List<TLeveScriptableObject> GetLevelList(List<string> levelNames)
        {
            return (from levelName in levelNames select GetLevelByName(levelName)).ToList();
        }

        public TLeveScriptableObject GetFallbackLevel()
        {
            return _backupLevels.GetRandomFromCollection();
        }

#if UNITY_EDITOR
        [TitleGroup("Import")]
        [ShowInInspector]
        private TLeveScriptableObject[] ImportLevels;

        [TitleGroup("Import by Name")]
        [SerializeField]
        private FolderReference _levelsRootFolder;

        [TitleGroup("Import by Name")]
        [ShowInInspector]
        private List<string> _levelNamesToImport = new();

        [TitleGroup("Import by Name")]
        [ShowInInspector]
        private List<string> _backupLevelNamesToImport = new();

        [TitleGroup("Import by Name")]
        [Button]
        public void ImportLevelsByName()
        {
            if (_levelsRootFolder == null || !_levelsRootFolder.IsValid)
            {
                Debug.LogError("Levels root folder is not set or invalid.");
                return;
            }

            if (_levelNamesToImport == null || _levelNamesToImport.Count == 0)
            {
                Debug.LogError("Level names list is empty.");
                return;
            }

            var allLevels = _levelsRootFolder.LoadAssets<TLeveScriptableObject>();
            var levelsByName = new Dictionary<string, TLeveScriptableObject>(allLevels.Length);
            foreach (var level in allLevels)
                levelsByName[level.name] = level;

            int added = 0;
            int skipped = 0;
            int notFound = 0;

            foreach (var levelName in _levelNamesToImport)
            {
                if (string.IsNullOrWhiteSpace(levelName))
                    continue;

                string trimmed = levelName.Trim();

                if (!levelsByName.TryGetValue(trimmed, out var level))
                {
                    Debug.LogWarning($"SOLevel not found in folder: {trimmed}");
                    notFound++;
                    continue;
                }

                if (!_levelsDictionary.TryAdd(trimmed, level))
                {
                    Debug.LogWarning($"Level already exists in dictionary: {trimmed}");
                    skipped++;
                    continue;
                }

                added++;
            }

            Debug.Log($"Import complete — Added: {added}, Skipped (duplicate): {skipped}, Not found: {notFound}");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [TitleGroup("Import by Name")]
        [Button]
        public void ImportBackupLevelsByName()
        {
            if (_levelsRootFolder == null || !_levelsRootFolder.IsValid)
            {
                Debug.LogError("Levels root folder is not set or invalid.");
                return;
            }

            if (_backupLevelNamesToImport == null || _backupLevelNamesToImport.Count == 0)
            {
                Debug.LogError("Backup level names list is empty.");
                return;
            }

            var allLevels = _levelsRootFolder.LoadAssets<TLeveScriptableObject>();
            var levelsByName = new Dictionary<string, TLeveScriptableObject>(allLevels.Length);
            foreach (var level in allLevels)
                levelsByName[level.name] = level;

            var existingNames = new HashSet<string>(
                _backupLevels?.Select(l => l != null ? l.name : null).Where(n => n != null)
                ?? Array.Empty<string>());

            var result = new List<TLeveScriptableObject>(_backupLevels ?? Array.Empty<TLeveScriptableObject>());
            int added = 0;
            int skipped = 0;
            int notFound = 0;

            foreach (var levelName in _backupLevelNamesToImport)
            {
                if (string.IsNullOrWhiteSpace(levelName))
                    continue;

                string trimmed = levelName.Trim();

                if (!levelsByName.TryGetValue(trimmed, out var level))
                {
                    Debug.LogWarning($"SOLevel not found in folder: {trimmed}");
                    notFound++;
                    continue;
                }

                if (!existingNames.Add(trimmed))
                {
                    Debug.LogWarning($"Backup level already exists: {trimmed}");
                    skipped++;
                    continue;
                }

                result.Add(level);
                added++;
            }

            _backupLevels = result.ToArray();
            Debug.Log(
                $"Backup import complete — Added: {added}, Skipped (duplicate): {skipped}, Not found: {notFound}");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [TitleGroup("Import")]
        [Button]
        public void RemoveNullEntries()
        {
            var nullKeys = _levelsDictionary
                .Where(kvp => kvp.Value == null)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in nullKeys)
                _levelsDictionary.Remove(key);
            Debug.Log($"Removed {nullKeys.Count} null entries from _levelsDictionary.");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [TitleGroup("Import")]
        [Button]
        public void ImportLevelsFromList()
        {
            foreach (var level in ImportLevels)
            {
                if (!_levelsDictionary.TryAdd(level.name, level))
                {
                    Debug.LogError($"Level {level.name} already exists!");
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [TitleGroup("Export")]
        [ShowInInspector]
        private List<string> _fallbackLevelIDs = new();

        [TitleGroup("Export")]
        [Button]
        public void GenerateFallbackLevelIDs()
        {
            var entries = _fallbackLevelIDs
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => $"        \"{n.Trim()}\"");
            var body = string.Join(",\n", entries);
            var code = $"public static readonly List<string> FallbackLevelIDs = new List<string>\n{{\n{body}\n}};";
            GUIUtility.systemCopyBuffer = code;
            Debug.Log($"Copied to clipboard:\n{code}");
        }
#endif
    }
}