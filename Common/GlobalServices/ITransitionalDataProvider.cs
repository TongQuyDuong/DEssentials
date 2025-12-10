using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if DESSENTIALS_ZEGO_SDK
using Zego;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Common.GlobalServices
{
    /// <summary>
    /// Transitional data exists within a singular session, do not get stored and only meant to temporarily carry data between scenes
    /// </summary>
    public interface ITransitionalDataProvider : IGlobalService<ITransitionalDataProvider>
    {
        public double CheatIncrementalAdsRevenue { get; }
    }
}
