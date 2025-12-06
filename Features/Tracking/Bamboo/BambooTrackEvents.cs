using System.Collections;
using System.Collections.Generic;
using Dessentials.Common;
using Dessentials.Common.GlobalServices;
using UnityEngine;
#if DESSENTIALS_ZEGO_SDK
using Zego;
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
        public double TotalAdsRevenue { get; }
        public double TotalIAPRevenue { get; }
        
        public double LTV => TotalAdsRevenue + TotalIAPRevenue;
    }
    
    public abstract class BaseBambooTrackEvent
    {
        protected bool m_initialized = false;
        protected bool m_eventTracked = false;
        
        protected virtual string EventName => string.Empty;

        public virtual bool IsTrackable
        {
            get
            {
                var trackedEventsProvider = IAlreadyTrackedEventsProvider.Global;

                return trackedEventsProvider != null && trackedEventsProvider.AlreadyTrackedEvents.Contains(EventName);
            }
        }

        public virtual void InitTrackTask()
        {
            m_initialized = true;
        }

        //Called when all conditions are met for the first time, meant for inheritors to call
        protected void TrackBambooEvent()
        {
            if (!m_initialized
                || m_eventTracked
                || !IsTrackable)
                return;
            
            var revenueDataProvider = IRevenueDataProvider.Global;
            
            if (revenueDataProvider == null)
                return;
            
            var firebaseAnalytics = IFirebaseAnalytics.Global;
            
            if (firebaseAnalytics == null)
                return;
            
            m_eventTracked = true;
            IAlreadyTrackedEventsProvider.Global?.OnNewBambooEventTracked(EventName);
            
            firebaseAnalytics.LogEvent(EventName, "ads_value", revenueDataProvider.LTV);
        }
    }
}
