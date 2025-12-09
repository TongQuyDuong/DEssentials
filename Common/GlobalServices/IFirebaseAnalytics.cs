using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if DESSENTIALS_ZEGO_SDK
using Zego;
using Firebase.Analytics;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Common.GlobalServices
{
    public interface IFirebaseAnalytics : IGlobalService<IFirebaseAnalytics>
    {
        public void LogEvent(object eventType);
        
        public void LogEvent(string name, string parameterName, double parameterValue);

        public void SetUserProperty(string propertyName, string propertyValue);
        
#if DESSENTIALS_ZEGO_SDK
        public void LogEvent(string name, params Parameter[] parameters);
#endif
    }
}
