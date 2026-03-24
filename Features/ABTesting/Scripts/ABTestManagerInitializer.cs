using System;
using System.Collections;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Search;
#endif
using UnityEngine;

namespace Dessentials.Features.ABTesting
{
    public class ABTestManagerInitializer : MonoBehaviour
    {
        private void Awake()
        {
            ABTestManager.Instance.Initialize();
        }

#if UNITY_EDITOR && ODIN_INSPECTOR
        [Button]
        public void PingAbTestManager()
        {
            var assetPath = AssetDatabase.GetAssetPath(ABTestManager.Instance);
            
            SearchUtils.PingAsset(assetPath);
        }
#endif
    }
}
