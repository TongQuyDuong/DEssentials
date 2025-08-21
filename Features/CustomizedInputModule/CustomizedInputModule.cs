using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dessentials.Features
{
    public partial class CustomizedInputModule : StandaloneInputModule
    {
        private static CustomizedInputModule _cachedInput;
        
        public static CustomizedInputModule Instance
        {
            get
            {
                if (_cachedInput == null)
                {
                    _cachedInput = EventSystem.current.currentInputModule as CustomizedInputModule;
                    if (_cachedInput == null)
                    {
                        Debug.LogError("Missing CustomizedInputModule.");
                        // some error handling
                    }
                }

                return _cachedInput;
            }
        }
        
        public GameObject GameObjectUnderPointer(int pointerId = kMouseLeftId)
        {
            var lastPointer = GetLastPointerEventData(pointerId);
            return lastPointer?.pointerCurrentRaycast.gameObject;
        }
    }
}
