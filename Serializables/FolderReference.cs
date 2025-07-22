using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dessentials.Serializables
{
#if UNITY_EDITOR
    [System.Serializable]
    public class FolderReference
    {
        [SerializeField] private string name;

        public string GUID;

        public string Path
        {
            get => AssetDatabase.GUIDToAssetPath(GUID);
            set => GUID = AssetDatabase.AssetPathToGUID(value);
        }
        public FolderReference(string path) => Path = path;
    } 
#endif
}
