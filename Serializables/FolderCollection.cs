using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEditor;
using UnityEngine;

namespace Dessentials.Serializables
{
#if UNITY_EDITOR
    [System.Serializable]
    public class FolderCollection<T> where T : Object
    {
        [SerializeField]
#if ODIN_INSPECTOR
        [LabelText("List")]
#endif
        private List<FolderReference> folders = new List<FolderReference>();

        public List<FolderReference> Folders => folders;

        /// <summary>
        /// Retrieves all assets of type T from all folders in this collection using Unity's AssetDatabase.
        /// </summary>
        /// <returns>A list of all assets of type T found in the referenced folders.</returns>
        public List<T> GetAllAssets()
        {
            List<T> assets = new List<T>();
            
            if (folders == null || folders.Count == 0)
                return assets;
            
            foreach (var folder in folders)
            {
                if (folder == null)
                    continue;
                
                assets.AddRange(folder.GetAllItemsOfType<T>());
            }

            return assets;
        }

        /// <summary>
        /// Adds a folder reference to the collection.
        /// </summary>
        public void AddFolder(FolderReference folder)
        {
            if (folder != null && !folders.Contains(folder))
                folders.Add(folder);
        }

        /// <summary>
        /// Removes a folder reference from the collection.
        /// </summary>
        public void RemoveFolder(FolderReference folder)
        {
            folders?.Remove(folder);
        }

        /// <summary>
        /// Clears all folder references from the collection.
        /// </summary>
        public void Clear()
        {
            folders?.Clear();
        }
    }
#endif
}
