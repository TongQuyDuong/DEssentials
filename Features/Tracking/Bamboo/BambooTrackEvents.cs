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
    public abstract class BaseBambooTrackEvent
    {
        protected bool m_initialized = false;
        protected bool m_eventTracked = false;
        protected readonly bool _doesTrackOnIAP = false;

        protected readonly string _eventName;

        public string EventName => _eventName;
        public bool IsActive => m_initialized;

        protected virtual bool IsTrackable
        {
            get
            {
                var trackedEventsProvider = IAlreadyTrackedEventsProvider.Global;

                return trackedEventsProvider != null &&
                       !trackedEventsProvider.AlreadyTrackedEvents.Contains(_eventName);
            }
        }

        protected BaseBambooTrackEvent(string eventName, bool trackOnIAP = true)
        {
            _eventName = eventName;
            _doesTrackOnIAP = trackOnIAP;
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
        protected virtual void TrackBambooEventFirstTime()
        {
            if (!m_initialized
                || m_eventTracked)
                return;

            m_eventTracked = true;
            IAlreadyTrackedEventsProvider.Global?.OnNewBambooEventTracked(_eventName);

            FireBambooEvent(FireBambooEventType.FirstTime);
        }

        public virtual void TrackBambooEventAdditionalTimes()
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

            if (!_doesTrackOnIAP)
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

            var revenueValue = type switch
            {
                FireBambooEventType.FirstTime => revenueDataProvider.TotalAdsRevenue,
                FireBambooEventType.AdditionalTimes => revenueDataProvider.LastestAdsRevenue,
                FireBambooEventType.IAP => revenueDataProvider.LastIAPRevenue,
                _ => revenueDataProvider.LastestAdsRevenue
            };
            
            FireBambooEvent(revenueValue);
        }

        protected void FireBambooEvent(double revenueValue)
        {
#if DESSENTIALS_ZEGO_SDK
            Parameter[] _paramTotal =
            {
                new("value", revenueValue),
                new("currency", "USD")
            };

            Debug.Log($"[BambooTracker] Firing {_eventName} with revenue: {revenueValue}");

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