using UnityEngine;

namespace Dessentials.Common
{
    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
    {
        static T instance;

        public static T gInstance
        {
            get
            {
                if (instance == null)
                {
                    //Debug.Log("Load: " + typeof(T).Name);
                    instance = Resources.Load<T>(typeof(T).Name);
                    (instance as SingletonScriptableObject<T>)?.OnInitialize();
                }

                return instance;
            }
        }

        // Optional overridable method for initializing the instance.
        protected virtual void OnInitialize()
        {
        }
    }
}