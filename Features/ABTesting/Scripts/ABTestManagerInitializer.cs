using System;
using System.Collections;
using System.Collections.Generic;
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
