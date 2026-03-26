#if DESSENTIALS_ZEGO_SDK
using Zego;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Features.Tracking
{
    public interface ITaichiTrackingConfigProvider : IGlobalService<ITaichiTrackingConfigProvider>
    {
        public TaichiTrackingConfig TaichiTrackingConfig { get; }
    }
}
