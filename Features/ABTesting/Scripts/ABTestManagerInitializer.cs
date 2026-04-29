#if ODIN_INSPECTOR && UNITY_EDITOR
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
    }
}
