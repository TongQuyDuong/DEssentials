using System.Collections.Generic;
using System.Linq;
using Dessentials.Serializables;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Dessentials.Features.LevelDatabase
{
    /// <summary>
    /// Level name -> Addressable reference, so a remote list of names can pick levels without the
    /// database loading every level. Abstract because a generic ScriptableObject cannot be an asset:
    /// a game subclasses it with its own level type. An unknown name is the caller's to handle.
    /// </summary>
    public abstract class LevelDatabase<TLevel> : ScriptableObject
        where TLevel : ScriptableObject
    {
        [SerializeField]
        protected SerializableDictionary<string, AssetReferenceT<TLevel>> _levelsDictionary = new();

        public bool Contains(string levelName) =>
            levelName != null && _levelsDictionary.ContainsKey(levelName);

        public bool TryGetLevel(string levelName, out AssetReferenceT<TLevel> level)
        {
            level = null;
            return levelName != null && _levelsDictionary.TryGetValue(levelName, out level) && level != null;
        }

#if UNITY_EDITOR
#if ODIN_INSPECTOR
        [TitleGroup("Import")]
        [ShowInInspector]
#endif
        private TLevel[] ImportLevels;

#if ODIN_INSPECTOR
        [TitleGroup("Import by Name")]
#endif
        [SerializeField]
        private FolderReference _levelsRootFolder;

#if ODIN_INSPECTOR
        [TitleGroup("Import by Name")]
        [ShowInInspector]
#endif
        private List<string> _levelNamesToImport = new();

        /// <summary>Clears the database and fills it with <paramref name="levels"/>, keyed by asset name.</summary>
        protected void SetLevels(IEnumerable<TLevel> levels)
        {
            _levelsDictionary.Clear();
            foreach (var level in levels)
            {
                if (level == null) continue;
                if (!_levelsDictionary.TryAdd(level.name, ReferenceTo(level)))
                    Debug.LogError($"Level {level.name} is listed twice.");
            }

            EditorUtility.SetDirty(this);
        }

        private static AssetReferenceT<TLevel> ReferenceTo(TLevel level) =>
            new(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(level)));

#if ODIN_INSPECTOR
        [TitleGroup("Import by Name")]
        [Button]
#endif
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

            var allLevels = _levelsRootFolder.LoadAssets<TLevel>();
            var levelsByName = new Dictionary<string, TLevel>(allLevels.Length);
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

                if (!_levelsDictionary.TryAdd(trimmed, ReferenceTo(level)))
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

#if ODIN_INSPECTOR
        [TitleGroup("Import")]
        [Button]
#endif
        public void RemoveNullEntries()
        {
            var nullKeys = _levelsDictionary
                .Where(kvp => kvp.Value == null || !kvp.Value.RuntimeKeyIsValid())
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in nullKeys)
                _levelsDictionary.Remove(key);
            Debug.Log($"Removed {nullKeys.Count} null entries from _levelsDictionary.");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

#if ODIN_INSPECTOR
        [TitleGroup("Import")]
        [Button]
#endif
        public void ImportLevelsFromList()
        {
            foreach (var level in ImportLevels)
            {
                if (!_levelsDictionary.TryAdd(level.name, ReferenceTo(level)))
                {
                    Debug.LogError($"Level {level.name} already exists!");
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
