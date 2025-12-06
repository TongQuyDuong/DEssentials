using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dessentials.Features.Tracking
{
    public class HighValueUserBambooEvent : BaseBambooTrackEvent
    {
        protected virtual double EcpmThreshold => 0;

        public override bool IsTrackable
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
