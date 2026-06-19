using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dessentials.Features.Tracking
{
    public abstract class ActionOccurenceAmountIntervalBambooEvent : ActionOccurenceAmountBambooEvent
    {
        protected ActionOccurenceAmountIntervalBambooEvent(string eventName, int withinAmountOfDays, int actionOccurenceAmountReportThreshold, bool trackOnIAP = true) 
            : base(eventName, withinAmountOfDays, actionOccurenceAmountReportThreshold, trackOnIAP)
        {
        }

        protected override void TrackBambooEventFirstTime()
        {
            base.TrackBambooEventFirstTime();
            
            IRevenueDataProvider.Global?.TryRegisterIntervalEvent(_eventName);
        }

        public override void TrackBambooEventAdditionalTimes()
        {
            var currentValue = ActionOccurenceAmountCurrentValue;
            
            if (currentValue % _actionOccurenceAmountReportThreshold == 0)
            {
                var alreadyTracked = IAlreadyTrackedEventsProvider.Global;
                var revenueProvider = IRevenueDataProvider.Global;

                if (alreadyTracked != null
                    && revenueProvider != null
                    && alreadyTracked.AlreadyTrackedIntervals.ContainsKey(_eventName)
                    && !alreadyTracked.AlreadyTrackedIntervals[_eventName].Contains(currentValue))
                {
                    alreadyTracked.OnIntervalBambooEventTracked(_eventName, currentValue);

                    revenueProvider.IntervalAdsRevenueDict.TryGetValue(_eventName, out var revenue);
                    
                    FireBambooEvent(revenue);
                    
                    revenueProvider.ClearIntervalAdsRevenue(_eventName);
                }
            }
        }
    }
}
