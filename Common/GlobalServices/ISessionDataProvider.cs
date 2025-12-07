using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if DESSENTIALS_ZEGO_SDK
using Zego;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Common.GlobalServices
{
    public interface ISessionDataProvider : IGlobalService<ISessionDataProvider>
    {
        public int DaysSinceFirstActive { get; }
    }
}
