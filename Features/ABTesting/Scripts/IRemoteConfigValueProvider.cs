using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if DESSENTIALS_ZEGO_SDK
using Zego;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Features.ABTesting
{
    public interface IRemoteConfigValueProvider : IGlobalService<IRemoteConfigValueProvider>
    {
        public Action OnFetched { get; set; }
        
        public bool TryGetStringValue(string key, out string value);
    }
}
