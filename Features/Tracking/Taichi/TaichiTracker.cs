using System;
using Dessentials.Common.GlobalServices;
using UnityEngine;

#if DESSENTIALS_ZEGO_SDK
using Zego;
using Firebase.Analytics;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Features.Tracking
{
    public interface ITaichiTracker : IGlobalService<ITaichiTracker>
    {
        public void HandleOnNewAdRevenuePaid();
    }
    
    public class TaichiTracker : MonoBehaviour, ITaichiTracker
    {
        private void Awake()
        {
            ServiceLocator.Global.Register<ITaichiTracker>(this);
        }

        public void HandleOnNewAdRevenuePaid()
        {
            var taichiConfig = ITaichiTrackingConfigProvider.Global?.TaichiTrackingConfig;

            if (taichiConfig != null)
            {
                var revenueDataProvider = IRevenueDataProvider.Global;

                if (revenueDataProvider == null)
                {
                    Debug.LogError("[BambooTracker] RevenueDataProvider null");
                    return;
                }

                if (revenueDataProvider.LastestAdsRevenue >= taichiConfig.RoundedThreshold)
                {
                    var eventName = $"taichi_top50_{taichiConfig.country}";
                    
                    Parameter[] _param = 
                    { 
                        new("value", taichiConfig.RoundedThreshold),
                        new("currency", "USD")
                    };
                    
                    Debug.Log($"[BambooTracker] Firing {eventName}");

                    IFirebaseAnalytics.Global?.LogEvent(eventName, _param);
                }
            }
        }
    }

    [Serializable]
    public class TaichiTrackingConfig
    {
        public string country = "Not Fetched";
        public double threshold = 999999999;

        public double RoundedThreshold
            => Math.Round(threshold, 3);
    }
}
