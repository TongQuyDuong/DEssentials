using System;
#if DESSENTIALS_ZEGO_SDK
using Zego;
#else
using Dessentials.Common.ServiceLocator;
#endif

namespace Dessentials.Common.GlobalServices
{
    public interface IGameInitializer : IGlobalService<IGameInitializer>
    {
        public void RegisterOnPlayerDataInitialized(Action callback);
    }
}
