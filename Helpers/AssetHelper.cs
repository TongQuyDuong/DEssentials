#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine; 
#endif

namespace Dessentials.Helpers
{
#if UNITY_EDITOR
    public static class DAssetHelper
    {
        public static List<GameObject> GetPrefabsInDirectory(string directoryPath, List<string> ignoreFolerNames = null)
        {
            var prefabs = new List<GameObject>();

            // Find all prefab files in the directory
            string[] prefabFiles = Directory.GetFiles(
                directoryPath,
                "*.prefab",
                SearchOption.AllDirectories
            );

            foreach (string prefabFile in prefabFiles)
            {
                // Convert backslashes to forward slashes for Unity path compatibility
                string assetPath = prefabFile.Replace("\\", "/");

                if (ignoreFolerNames != null && ignoreFolerNames.Count > 0)
                {
                    // Check if the path contains the folder name to ignore
                    if (ignoreFolerNames.Any(name => assetPath.Contains(name)))
                    {
                        continue; // Skip this prefab
                    }
                }

                // Load the prefab asset
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }

            return prefabs;
        }

        public static List<T> GetPrefabsInDirectory<T>(string directoryPath, List<string> ignoreFolerNames = null)
            where T : MonoBehaviour
        {
            var result = new List<T>();

            var prefabs = GetPrefabsInDirectory(directoryPath, ignoreFolerNames);

            foreach (var prefab in prefabs)
            {
                // Get all components of type T in the prefab
                var comp = prefab.GetComponent<T>();
                if (comp != null)
                {
                    result.Add(comp);
                }
            }

            return result;
        }

        public static List<TextAsset> GetJSONsInDirectory(string directoryPath, List<string> ignoreFolerNames = null)
        {
            var jsons = new List<TextAsset>();

            // Find all prefab files in the directory
            string[] jsonFiles = Directory.GetFiles(
                directoryPath,
                "*.json",
                SearchOption.AllDirectories
            );

            string[] txtFiles = Directory.GetFiles(
                directoryPath,
                "*.txt",
                SearchOption.AllDirectories
            );

            var resultsList = new List<string>(jsonFiles);
            resultsList.AddRange(txtFiles);

            foreach (string jsonFile in resultsList)
            {
                // Convert backslashes to forward slashes for Unity path compatibility
                string assetPath = jsonFile.Replace("\\", "/");

                if (ignoreFolerNames != null && ignoreFolerNames.Count > 0)
                {
                    // Check if the path contains the folder name to ignore
                    if (ignoreFolerNames.Any(name => assetPath.Contains(name)))
                    {
                        continue; // Skip this prefab
                    }
                }

                // Load the prefab asset
                TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                if (json != null)
                {
                    jsons.Add(json);
                }
            }

            return jsons;
        }

        public static List<T> GetScriptableObjectsInDirectory<T>(string directoryPath, List<string> ignoreFolerNames = null)
            where T : ScriptableObject
        {
            var result = new List<T>();

            // Find all prefab files in the directory
            string[] prefabFiles = Directory.GetFiles(
                directoryPath,
                "*.asset",
                SearchOption.AllDirectories
            );

            foreach (string prefabFile in prefabFiles)
            {
                // Convert backslashes to forward slashes for Unity path compatibility
                string assetPath = prefabFile.Replace("\\", "/");

                if (ignoreFolerNames != null && ignoreFolerNames.Count > 0)
                {
                    // Check if the path contains the folder name to ignore
                    if (ignoreFolerNames.Any(name => assetPath.Contains(name)))
                    {
                        continue; // Skip this prefab
                    }
                }

                // Load the prefab asset
                T obj = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (obj != null)
                {
                    result.Add(obj);
                }
            }

            return result;
        }

        /// <summary>
        /// Normalizes a directory path by removing any trailing directory separators.
        /// </summary>
        private static string NormalizeDirectoryPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Remove trailing slashes (both forward and backward)
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static async UniTask<bool> EnsureDirectoryAndMoveAssetAsync(
            string oldAssetPath,
            string newAssetPath,
            int pollingTimeoutMilliseconds = 5000,
            int pollingIntervalMilliseconds = 100)
        {
            if (string.IsNullOrEmpty(oldAssetPath) || string.IsNullOrEmpty(newAssetPath))
            {
                Debug.LogError("Old asset path or new asset path cannot be null or empty.");
                return false;
            }

            // Get the target directory from the new asset path AND NORMALIZE IT
            string newDirectoryRaw = Path.GetDirectoryName(newAssetPath);
            if (string.IsNullOrEmpty(newDirectoryRaw))
            {
                Debug.LogError($"Could not determine directory from new asset path: {newAssetPath}");
                return false;
            }
            string newDirectory = NormalizeDirectoryPath(newDirectoryRaw); // IMPORTANT: Normalize

            // --- Step 1: Ensure the target directory exists and is recognized ---
            if (!AssetDatabase.IsValidFolder(newDirectory))
            {
                string parentDirectory = NormalizeDirectoryPath(Path.GetDirectoryName(newDirectory)); // Normalize parent too
                string newFolderName = Path.GetFileName(newDirectory); // GetFileName on a normalized path

                // This check should now be more reliable
                if (string.IsNullOrEmpty(parentDirectory) || string.IsNullOrEmpty(newFolderName))
                {
                    Debug.LogError($"Could not determine parent directory ('{parentDirectory}') or folder name ('{newFolderName}') for: {newDirectory}. Original target: {newAssetPath}");
                    return false;
                }

                // Ensure parent directories exist (recursive check up to "Assets")
                Stack<string> foldersToCreate = new Stack<string>();
                string currentPathToEnsure = newDirectory; // Start with the full target directory

                // Build stack of directories to create, from deepest to shallowest parent
                while (!string.IsNullOrEmpty(currentPathToEnsure) &&
                       !AssetDatabase.IsValidFolder(currentPathToEnsure) &&
                       currentPathToEnsure.ToLowerInvariant().StartsWith("assets")) // Ensure we stay within Assets
                {
                    foldersToCreate.Push(currentPathToEnsure);
                    string parentOfCurrent = Path.GetDirectoryName(currentPathToEnsure);
                    if (string.IsNullOrEmpty(parentOfCurrent)) break; // Should not happen if starts with "Assets"
                    currentPathToEnsure = NormalizeDirectoryPath(parentOfCurrent);
                }

                // Create directories from shallowest parent down to the target directory
                while (foldersToCreate.Count > 0)
                {
                    string dirToMake = foldersToCreate.Pop(); // This will be Assets/A, then Assets/A/B, etc.
                    string pDir = NormalizeDirectoryPath(Path.GetDirectoryName(dirToMake));
                    string fName = Path.GetFileName(dirToMake); // Safe due to prior normalization

                    if (string.IsNullOrEmpty(pDir) || string.IsNullOrEmpty(fName))
                    {
                        Debug.LogError($"Invalid path components for creating folder: Parent='{pDir}', Name='{fName}' from '{dirToMake}'");
                        continue; // Skip this problematic one
                    }

                    // Only try to create if parent is valid (or is "Assets")
                    if (AssetDatabase.IsValidFolder(pDir) || pDir.Equals("Assets", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"Attempting to create folder: Name='{fName}' in Parent='{pDir}'");
                        string guid = AssetDatabase.CreateFolder(pDir, fName);
                        if (string.IsNullOrEmpty(guid) && !AssetDatabase.IsValidFolder(dirToMake))
                        {
                            Debug.LogWarning($"AssetDatabase.CreateFolder for '{dirToMake}' failed or returned empty GUID. Will poll.");
                        }
                        // Poll for its validity regardless of GUID, as IsValidFolder is the ultimate check
                        if (!await PollForValidFolder(dirToMake, pollingTimeoutMilliseconds, pollingIntervalMilliseconds))
                        {
                            Debug.LogError($"Directory {dirToMake} did not become valid after creation attempt.");
                            return false;
                        }
                        await UniTask.Yield(); // Small yield after each creation + poll
                    }
                    else
                    {
                        Debug.LogError($"Parent directory '{pDir}' for '{dirToMake}' is not valid or not 'Assets'. Skipping creation. This might indicate an issue with path construction.");
                        return false; // Critical path issue
                    }
                }
            }
            else
            {
                Debug.Log($"Target directory '{newDirectory}' already exists and is valid.");
            }

            // --- Step 2: Move the asset ---
            string moveError = AssetDatabase.MoveAsset(oldAssetPath, newAssetPath);
            if (string.IsNullOrEmpty(moveError))
            {
                Debug.Log($"Asset successfully moved from '{oldAssetPath}' to '{newAssetPath}'.");
                return true;
            }
            else
            {
                Debug.LogError($"Failed to move asset. Error: {moveError}. Old: '{oldAssetPath}', New: '{newAssetPath}'");
                return false;
            }
        }

        public static async UniTask MoveToLocalUsedFolder(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab is null, cannot move to Used folder.");
                return;
            }

            var newPrefabCurrentPath = AssetDatabase.GetAssetPath(prefab);
            if (!newPrefabCurrentPath.Contains("Used"))
            {
                var newPrefabNewDirectory = string.Empty;

                var newPrefabFileName = Path.GetFileName(newPrefabCurrentPath);

                var newPrefabCurrentDirectory = newPrefabCurrentPath.Replace(newPrefabFileName, "");

                if (newPrefabCurrentPath.Contains("Unused"))
                {
                    newPrefabNewDirectory = newPrefabCurrentDirectory.Replace("Unused", "Used");
                }
                else
                {
                    newPrefabNewDirectory = newPrefabCurrentDirectory + "Used/";
                }

                await EnsureDirectoryAndMoveAssetAsync(newPrefabCurrentPath, newPrefabNewDirectory + newPrefabFileName);
            }
        }

        public static async UniTask MoveToLocalUnusedFolder(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Prefab is null, cannot move to Unused folder.");
                return;
            }

            var oldAssetPath = AssetDatabase.GetAssetPath(prefab);

            if (oldAssetPath.Contains("Used"))
            {
                var newAssetPath = oldAssetPath.Replace("Used", "Unused");

                var newDirectory = newAssetPath.Replace(Path.GetFileName(newAssetPath), "");

                await EnsureDirectoryAndMoveAssetAsync(oldAssetPath, newAssetPath);
            }
        }

        private static async UniTask<bool> PollForValidFolder(
            string folderPath, // Should be normalized before calling
            int timeoutMilliseconds,
            int intervalMilliseconds,
            CancellationToken cancellationToken = default)
        {
            // Ensure folderPath is normalized for consistent checks
            string normalizedFolderPath = NormalizeDirectoryPath(folderPath);
            if (AssetDatabase.IsValidFolder(normalizedFolderPath)) return true;

            Debug.Log($"Polling for directory validity: {normalizedFolderPath}");
            var timeoutCts = new CancellationTokenSource(timeoutMilliseconds);
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            try
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); // More direct refresh
                    await UniTask.Delay(intervalMilliseconds, cancellationToken: linkedCts.Token);

                    if (AssetDatabase.IsValidFolder(normalizedFolderPath))
                    {
                        Debug.Log($"Polling success: Directory '{normalizedFolderPath}' is now valid.");
                        return true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    Debug.LogWarning($"Polling for directory '{normalizedFolderPath}' timed out after {timeoutMilliseconds}ms.");
                }
            }
            finally
            {
                if (!timeoutCts.IsCancellationRequested) timeoutCts.Cancel();
                timeoutCts.Dispose();
                linkedCts.Dispose();
            }
            // Final check after loop/timeout
            return AssetDatabase.IsValidFolder(normalizedFolderPath);
        }
    }
#endif
}

