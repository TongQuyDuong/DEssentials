#if DESSENTIALS_ZEGO_SDK
using Zego;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Features.Tracking
{
    public interface IWatchAdsAmountProvider : IGlobalService<IWatchAdsAmountProvider>
    {
        public int TotalAdsWatched { get; set; }
        public int TotalRewardAdsWatched { get; set; }
        public int TotalInterAdsWatched { get; set; }
    }
}
