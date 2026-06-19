#if DESSENTIALS_ZEGO_SDK && DESSENTIALS_SRDEBUGGER
using System.ComponentModel;
using Zego;

namespace Dessentials.Features.Tracking
{
	public partial class BambooTrackOptions
	{
		//===========================================================================================

		private EAdFormat _adsRevenuePaidEventFormat = EAdFormat.RewardedVideo;
		private double _adsPaidEventRevenue = 0.5f;
		
		[Category(SROptions.BAMBOO_TRACK_ADS_PAID_EVENT_CATEGORY)]
		public EAdFormat CheatAdsPaidEventFormat
		{
			get => _adsRevenuePaidEventFormat;
			set => _adsRevenuePaidEventFormat = value;
		}
	
		[Category(SROptions.BAMBOO_TRACK_ADS_PAID_EVENT_CATEGORY)]
		public double CheatAdsPaidEventRevenue
		{
			get => _adsPaidEventRevenue;
			set => _adsPaidEventRevenue = value;
		}

		[Category(SROptions.BAMBOO_TRACK_ADS_PAID_EVENT_CATEGORY)]
		public void TriggerAdsRevenuePaidEvent()
		{
			EventBus<AdPaidEvent>.Raise(new AdPaidEvent()
			{
				Format = _adsRevenuePaidEventFormat,
				Revenue = _adsPaidEventRevenue
			});
		}
	
		[Category(SROptions.BAMBOO_TRACK_ADS_PAID_EVENT_CATEGORY)]
		public int TotalAdsWatched
		{
			get => IWatchAdsAmountProvider.Exist ? IWatchAdsAmountProvider.Global.TotalAdsWatched : -1;
			set
			{
				if (IWatchAdsAmountProvider.Exist)
				{
					IWatchAdsAmountProvider.Global.TotalAdsWatched = value;
				}
			}
		}
	
		[Category(SROptions.BAMBOO_TRACK_ADS_PAID_EVENT_CATEGORY)]
		public int TotalInterWatched
		{
			get => IWatchAdsAmountProvider.Exist ? IWatchAdsAmountProvider.Global.TotalInterAdsWatched : -1;
			set
			{
				if (IWatchAdsAmountProvider.Exist)
				{
					IWatchAdsAmountProvider.Global.TotalInterAdsWatched = value;
				}
			}
		}
	
		[Category(SROptions.BAMBOO_TRACK_ADS_PAID_EVENT_CATEGORY)]
		public int TotalRewardWatched
		{
			get => IWatchAdsAmountProvider.Exist ? IWatchAdsAmountProvider.Global.TotalRewardAdsWatched : -1;
			set
			{
				if (IWatchAdsAmountProvider.Exist)
				{
					IWatchAdsAmountProvider.Global.TotalRewardAdsWatched = value;
				}
			}
		}
	}
}

#endif