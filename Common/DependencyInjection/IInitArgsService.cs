using UnityEngine;

namespace Dessentials.Common.DependencyInjection
{
#if DESSENTIALS_INIT_ARGS
    public interface IInitArgsService<out T> where T : class
    {
        public static T Service
        {
            get
            {
                if (!Sisus.Init.Service.TryGet(out T service))
                {
                    
#if DESSENTIALS_DEBUG_LOG_IN_BUILD || UNITY_EDITOR
                    Debug.LogError($"{typeof(T).Name} has not been registered!");
#endif
                    
                    return null;
                }

                return service;
            }
        }
    }
#endif
}
