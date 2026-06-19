using System.Collections.Generic;
using System.Linq;
#if DESSENTIALS_ZEGO_SDK
using Zego;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Features.Tracking
{
    public interface IRevenueDataProvider : IGlobalService<IRevenueDataProvider>
    {
        public double FirstAdRevenue { get; }
        public double TotalAdsRevenue { get; set; }
        public Dictionary<string, double> IntervalAdsRevenueDict { get; }
        public double LastestAdsRevenue { get; set; }
        public double TotalIAPRevenue { get; }
        public double LastIAPRevenue { get; }

        public double LTV => TotalAdsRevenue + TotalIAPRevenue;

        public double AdsIncrementalValueSingleBidding { get; set; }
        public double AdsIncrementalValueMultiBidding { get; set; }

        public void HandleNewAdRevenuePaid(double revenue)
        {
            TotalAdsRevenue += revenue;
            LastestAdsRevenue = revenue;

            if (IntervalAdsRevenueDict != null)
            {
                foreach (var key in IntervalAdsRevenueDict.Keys.ToList())
                {
                    IntervalAdsRevenueDict[key] += revenue;
                }
            }
        }

        public void TryRegisterIntervalEvent(string eventName)
        {
            IntervalAdsRevenueDict?.TryAdd(eventName, 0);
        }

        public void ClearIntervalAdsRevenue(string eventName)
        {
            if (IntervalAdsRevenueDict != null
                && IntervalAdsRevenueDict.ContainsKey(eventName))
            {
                IntervalAdsRevenueDict[eventName] = 0;
            }
        }
    }
}
