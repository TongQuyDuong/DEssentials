#if DESSENTIALS_ZEGO_SDK
using System;
using System.Collections;
using System.Collections.Generic;
using Dessentials.Common.GlobalServices;
using Firebase.Analytics;
using Runtime.Manager.Data;
using UnityEngine;

namespace Dessentials.Features.Tracking
{
    public class TotalAndIncrementalAdRevenueTracker : MonoBehaviour
    {
        [SerializeField]
        private AdsIncrementalConfig _adsIncrementalConfig = new();

        Zego.EventBinding<Zego.AdPaidEvent> _adsPaidEventBinding;
        
        private void Awake()
        {
            _adsPaidEventBinding = new Zego.EventBinding<Zego.AdPaidEvent>(OnAdRevenuePaid);
            Zego.EventBus<Zego.AdPaidEvent>.Register(_adsPaidEventBinding);
        }

        private void OnDestroy()
        {
            Zego.EventBus<Zego.AdPaidEvent>.Deregister(_adsPaidEventBinding);
        }

        private void OnAdRevenuePaid(Zego.AdPaidEvent e)
        {
            TrackTotalAdsRevenue(e);
            TrackIncrementalAdsRevenue(e);
        }

        private void TrackTotalAdsRevenue(Zego.AdPaidEvent e)
        {
            var revenue = e.Revenue;
            
            if (ITransitionalDataProvider.Exist)
            {
                if (ITransitionalDataProvider.Global.CheatIncrementalAdsRevenue > 0)
                    revenue = ITransitionalDataProvider.Global.CheatIncrementalAdsRevenue;
            }
            
            var adsRevenueDataProvider = IRevenueDataProvider.Global;
            if (adsRevenueDataProvider == null) return;
            
            adsRevenueDataProvider.TotalAdsRevenue += revenue;
            
            if (IFirebaseAnalytics.Exist)
            {
                IFirebaseAnalytics.Global
                    .SetUserProperty("monet_ads_value",
                        adsRevenueDataProvider.TotalAdsRevenue.ToString("0.000"));
            }
        }

        private void TrackIncrementalAdsRevenue(Zego.AdPaidEvent e)
        {
            var sessionDataProvider = ISessionDataProvider.Global;
            
            if (sessionDataProvider == null) return;
            
            if (ISessionDataProvider.Global.DaysSinceFirstActive > 7) return;
            
            var revenue = e.Revenue;
            
            if (ITransitionalDataProvider.Exist)
            {
                if (ITransitionalDataProvider.Global.CheatIncrementalAdsRevenue > 0)
                    revenue = ITransitionalDataProvider.Global.CheatIncrementalAdsRevenue;
            }
            
            var adsRevenueDataProvider = IRevenueDataProvider.Global;
            if (adsRevenueDataProvider == null) return;
            
            adsRevenueDataProvider.AdsIncrementalValueSingleBidding += revenue;
            adsRevenueDataProvider.AdsIncrementalValueMultiBidding += revenue;
            
            var valueSingle = adsRevenueDataProvider.AdsIncrementalValueSingleBidding;
            var configSingle
                = _adsIncrementalConfig.GetSingleBiddingIncrementalData(adsRevenueDataProvider.TotalAdsRevenue);
            if (valueSingle >= configSingle.incrementalRevenue)
            {
                TrackEventAdsIncremental("ad_incremental", configSingle.incrementalRevenue);
                adsRevenueDataProvider.AdsIncrementalValueSingleBidding = 0;
            }
            
            var valueMulti = adsRevenueDataProvider.AdsIncrementalValueMultiBidding;
            var configMulti 
                = _adsIncrementalConfig
                    .GetMultiBiddingIncrementalData(adsRevenueDataProvider.TotalAdsRevenue,
                        out var eventName);
            if (valueMulti >= configMulti.incrementalRevenue)
            {
                TrackEventAdsIncremental(eventName, configMulti.incrementalRevenue);
                adsRevenueDataProvider.AdsIncrementalValueMultiBidding = 0;
            }
        }
        
        private void TrackEventAdsIncremental(string eventName, double value)
        {
            Parameter[] adParameters =
            {
                new("currency", "USD"),
                new("value", value),
            };

            if (IFirebaseAnalytics.Exist)
            {
                IFirebaseAnalytics.Global.LogEvent(eventName, adParameters);
            }
        }
    }
    
    [Serializable]
    public class AdsIncrementalConfig
    {
        [SerializeField] private AdsIncrementalData[] singleBidding;
        [SerializeField] private AdsIncrementalData[] multiBidding;
        public AdsIncrementalData GetSingleBiddingIncrementalData(double ltv)
        {
            AdsIncrementalData value = singleBidding[0];
            foreach (var data in singleBidding)
            {
                if (ltv < data.ltv) continue;
                value = data;
            }
            return value;
        }
        
        public AdsIncrementalData GetMultiBiddingIncrementalData(double ltv, out string eventName)
        {
            var incrementalCount = 0;

            eventName = "ad_incremental";
            
            AdsIncrementalData value = multiBidding[0];
            foreach (var data in multiBidding)
            {
                if (ltv < data.ltv) continue;
                incrementalCount++;
                value = data;
            }

            eventName += $"_{incrementalCount}";
            
            return value;
        }
    }
    [Serializable]
    public struct AdsIncrementalData
    {
        public float ltv;
        public float incrementalRevenue;
    }
}

#endif