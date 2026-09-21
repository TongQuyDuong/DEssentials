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
        /// <summary>
        /// The registered provider, or null when there is none: a build with no remote config SDK,
        /// a scene played without the service bootstrapped, or the Editor outside play mode (where
        /// ServiceLocator itself does not exist). Unlike <see cref="IGlobalService{T}.Global"/> this
        /// stays quiet - a missing provider is a normal state callers fall back from, not an error.
        /// </summary>
        public static IRemoteConfigValueProvider Current
            => Application.isPlaying && IGlobalService<IRemoteConfigValueProvider>.Exist
                ? IGlobalService<IRemoteConfigValueProvider>.Global
                : null;

        public Action OnFetched { get; set; }
        
        public bool TryGetStringValue(string key, out string value);
    }
}
