using System.Collections;
using System.Collections.Generic;
using Dessentials.Common;
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
    public interface IAlreadyTrackedEventsProvider : IGlobalService<IAlreadyTrackedEventsProvider>
    {
        public List<string> AlreadyTrackedEvents { get; }
        
        public void OnNewBambooEventTracked(string eventName)
        {
            if (AlreadyTrackedEvents == null)
            {
                
#if DESSENTIALS_DEBUG_LOG_IN_BUILD || UNITY_EDITOR
                Debug.LogError("AlreadyTrackedEvents null");
#endif
                return;
            }
            
            if (!AlreadyTrackedEvents.Contains(eventName))
            {
                AlreadyTrackedEvents.Add(eventName);
                
#if DESSENTIALS_DEBUG_LOG_IN_BUILD || UNITY_EDITOR
                Debug.Log($"[BambooTracker] {eventName} has been fired, total bamboo tracked events: {AlreadyTrackedEvents.Count}");
#endif
            }
        }
    }
    
    public interface IRevenueDataProvider : IGlobalService<IRevenueDataProvider>
    {
        public double FirstAdRevenue { get; }
        public double TotalAdsRevenue { get; set; }
        public double LastestAdsRevenue { get; set; }
        public double TotalIAPRevenue { get; }
        public double LastIAPRevenue { get; }
        
        public double LTV => TotalAdsRevenue + TotalIAPRevenue;
        
        public double AdsIncrementalValueSingleBidding { get; set; }
        public double AdsIncrementalValueMultiBidding { get; set; }
    }
    
    public abstract class BaseBambooTrackEvent
    {
        protected bool m_initialized = false;
        protected bool m_eventTracked = false;
        
        protected readonly string _eventName;
        
        public string EventName => _eventName;
        public bool IsActive => m_initialized;

        protected virtual bool IsTrackable
        {
            get
            {
                var trackedEventsProvider = IAlreadyTrackedEventsProvider.Global;

                return trackedEventsProvider != null && !trackedEventsProvider.AlreadyTrackedEvents.Contains(_eventName);
            }
        }

        protected BaseBambooTrackEvent(string eventName)
        {
            _eventName = eventName;
        }

        public virtual bool InitTrackTask()
        {
            if (m_initialized)
            {
                if (IsTrackable)
                {
                    Debug.Log($"[BambooTracker] Still Tracking {_eventName}");
                }
                else
                {
                    m_initialized = false;
                    Debug.Log($"[BambooTracker] Stop Tracking {_eventName}");
                }
                
                return false;
            }
            
            if (!IsTrackable)
                return false;
            
            m_initialized = true;
            
            Debug.Log($"[BambooTracker] Start Tracking {_eventName}");
            
            return true;
        }

        //Called when all conditions are met for the first time, meant for inheritors to call
        protected void TrackBambooEventFirstTime()
        {
            if (!m_initialized
                || m_eventTracked)
                return;
            
            m_eventTracked = true;
            IAlreadyTrackedEventsProvider.Global?.OnNewBambooEventTracked(_eventName);

            FireBambooEvent(FireBambooEventType.FirstTime);
        }

        public void TrackBambooEventAdditionalTimes()
        {
            var trackedEventsProvider = IAlreadyTrackedEventsProvider.Global;
            
            if (trackedEventsProvider == null
                || !trackedEventsProvider.AlreadyTrackedEvents.Contains(_eventName))
                return;
            
            FireBambooEvent(FireBambooEventType.AdditionalTimes);
        }
        
        public void TrackBambooEventIAP()
        {
            var trackedEventsProvider = IAlreadyTrackedEventsProvider.Global;
            
            if (trackedEventsProvider == null
                || !trackedEventsProvider.AlreadyTrackedEvents.Contains(_eventName))
                return;
            
            FireBambooEvent(FireBambooEventType.IAP);
        }

        protected void FireBambooEvent(FireBambooEventType type)
        {
            var revenueDataProvider = IRevenueDataProvider.Global;

            if (revenueDataProvider == null)
            {
                Debug.LogError("[BambooTracker] RevenueDataProvider null");
                return;
            }

#if DESSENTIALS_ZEGO_SDK

            var revenueValue = type switch
            {
                FireBambooEventType.FirstTime => revenueDataProvider.TotalAdsRevenue,
                FireBambooEventType.AdditionalTimes => revenueDataProvider.LastestAdsRevenue,
                FireBambooEventType.IAP => revenueDataProvider.LastIAPRevenue,
            };
            
            Parameter[] _paramTotal = 
            { 
                new("value", revenueValue),
                new("currency", "USD")
            };
                
            Debug.Log($"[BambooTracker] Firing {_eventName}");

            IFirebaseAnalytics.Global?.LogEvent(_eventName, _paramTotal);
#else
            Debug.LogError($"[BambooTracker] No Zego SDK");            
#endif
        }

        protected enum FireBambooEventType
        {
            FirstTime,
            AdditionalTimes,
            IAP
        }
    }
}
