using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zego;

namespace Dessentials
{
    public interface IRemoteConfigValueProvider : IGlobalService<IRemoteConfigValueProvider>
    {
        public Action OnFetched { get; set; }
        
        public bool TryGetStringValue(string key, out string value);
    }
}
