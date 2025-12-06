using UnityEngine;

namespace Dessentials.Common.ServiceLocator
{
    public interface IGlobalService<out T> where T : class
    {
        public static T Global
        {
            get
            {
                if (!ServiceLocator.Global.TryGet<T>(out var result))
                {
                    
#if DESSENTIALS_DEBUG_LOG_IN_BUILD || UNITY_EDITOR
                    Debug.LogError($"{typeof(T).Name} has not been registered!");
#endif
                    
                    return null;
                }

                return result;
            }
        }

        // public static void TryGet(out T service) => ServiceLocator.Global.TryGet(out service);

        public static bool Exist => ServiceLocator.Global.TryGet<T>(out _);

        public static void Register(T service) => ServiceLocator.Global.Register(service);
    }
}