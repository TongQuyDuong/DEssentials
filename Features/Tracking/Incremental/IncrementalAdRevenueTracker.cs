#if DESSENTIALS_ZEGO_SDK
using System;
using System.Collections;
using System.Collections.Generic;
using Dessentials.Common.GlobalServices;
using Runtime.Manager.Data;
using UnityEngine;

namespace Dessentials.Features.Tracking
{
    public class IncrementalAdRevenueTracker : MonoBehaviour
    {
        [SerializeField]
        private List<double> _singleBiddingIncrements = new();
        [SerializeField]
        private List<double> _multiBiddingIncrements = new();

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
            var sessionDataProvider = ISessionDataProvider.Global;
            if (sessionDataProvider == null) return;
            
            if (ISessionDataProvider.Global.DaysSinceFirstActive > 7) return;

            var transitionalDataProvider = ITransitionalDataProvider.Global;
            if (transitionalDataProvider == null) return;
            
            var revenue = e.Revenue;
            if (transitionalDataProvider.CheatAdsRevenue > 0)
                revenue = transitionalDataProvider.CheatAdsRevenue;
            
            var adsRevenueDataProvider = IRevenueDataProvider.Global;
            if (adsRevenueDataProvider == null) return;
            
            adsRevenueDataProvider.AdsIncementalValue += revenue;
            // var value = adsRevenueDataProvider.AdsIncementalValue;
            // var config = _adsIncrementalConfig.GetInCrementalData(_totalAdsRevenue.Value);
            // if (value >= config.incrementalRevenue)
            // {
            //     TrackEventAdsIncremental(config.incrementalRevenue);
            //     _AdsIncremental.Value = 0;
            // }
        }
    }
}

#endif