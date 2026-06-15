using System;
using UnityEngine;

namespace Dessentials.Common.EntityManagement
{
    public class ManagedEntity<TSelf> : MonoBehaviour
    where TSelf : ManagedEntity<TSelf>
    {
        protected virtual void OnDestroy()
        {
        }
    }
}
