using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dessentials.Features.Tracking
{
    public abstract class HighValueUserBambooEvent : BaseBambooTrackEvent
    {
        protected virtual double EcpmThreshold => 0;
        
        protected HighValueUserBambooEvent(string eventName) : base(eventName) {}

        protected override bool IsTrackable
        {
            get
            {
                var revenueProvider = IRevenueDataProvider.Global;
                
                return base.IsTrackable
                    && revenueProvider != null
                    && revenueProvider.FirstAdRevenue >= EcpmThreshold;
            }
        }
    }
}
