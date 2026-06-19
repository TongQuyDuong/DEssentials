#if DESSENTIALS_SRDEBUGGER
using System.ComponentModel;
using Dessentials.Common.Utility;
using Dessentials.Features.Tracking;

public partial class SROptions
{
    public const string DESSENTIAL_CATEGORY = "Dessential";
    public const string BAMBOO_TRACK_CATEGORY = DESSENTIAL_CATEGORY + "/Bamboo Track";
    public const string BAMBOO_TRACK_REVENUE_CATEGORY = BAMBOO_TRACK_CATEGORY + "/Revenue";
    public const string BAMBOO_TRACK_ADS_PAID_EVENT_CATEGORY = BAMBOO_TRACK_CATEGORY + "/AdsRevenuePaidEvent";
    
    [Category(DESSENTIAL_CATEGORY), DisplayName("Enable Debugging")]
    public bool EnableDebugging { get => DBug.Enabled; set => DBug.Enabled = value; }
    
    private BambooTrackOptions m_bambooTrackOptions;
    
    [Category(DESSENTIAL_CATEGORY), DisplayName("Bamboo Track"), Sort(2)]
    public bool ToggleAdsTrack
    {
        get => m_bambooTrackOptions != null;
        set
        {
            if (value && m_bambooTrackOptions == null)
            {
                m_bambooTrackOptions = new BambooTrackOptions();
                SRDebug.Instance.AddOptionContainer(m_bambooTrackOptions);
            }
            else if (!value && m_bambooTrackOptions != null)
            {
                SRDebug.Instance.RemoveOptionContainer(m_bambooTrackOptions);
                m_bambooTrackOptions = null;
            }
        }
    }
}

#endif