using System.Collections.Generic;
using System.Linq;
using Dessentials.Common.Utility;
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
        public Dictionary<string, List<int>> AlreadyTrackedIntervals { get; }

        public void OnNewBambooEventTracked(string eventName)
        {
            if (AlreadyTrackedEvents == null)
            {
                DBug.LogError("AlreadyTrackedEvents null");
                return;
            }

            if (!AlreadyTrackedEvents.Contains(eventName))
            {
                AlreadyTrackedEvents.Add(eventName);
            }
        }
        
        public void OnIntervalBambooEventTracked(string eventName, int trackValue)
        {
            if (AlreadyTrackedIntervals == null)
            {

                DBug.LogError("AlreadyTrackedIntervals null");
                return;
            }

            if (AlreadyTrackedIntervals.Keys.All(name => name != eventName))
            {
                AlreadyTrackedIntervals.Add(eventName, new List<int> { trackValue });
            }
            else
            {
                AlreadyTrackedIntervals[eventName].Add(trackValue);
            }
        }
    }
}
