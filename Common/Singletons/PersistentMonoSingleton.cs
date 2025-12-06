using UnityEngine;

namespace Dessentials.Common
{
    public class PersistentMonoSingleton<T> : MonoBehaviour where T : Component
    {
        #region Members

        private static T s_instance;
        private static object s_locker = new object();

        #endregion Members

        #region Properties

        public static T Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_locker)
                    {
                        s_instance = FindObjectOfType<T>();
                        if (s_instance == null)
                        {
                            if (!Application.isPlaying)
                                return null;
                            
                            GameObject instanceGameObject = new GameObject();
                            s_instance = instanceGameObject.AddComponent<T>();
                            instanceGameObject.name = typeof(T).Name;
                            DontDestroyOnLoad(instanceGameObject);
                        }
                    }
                }

                return s_instance;
            }
        }

        #endregion Properties

        #region API Methods

        protected virtual void Awake()
        {
            lock (s_locker)
            {
                if (s_instance == null)
                {
                    s_instance = this as T;
                    DontDestroyOnLoad(gameObject);
                    gameObject.name = "[PersistentSingleton] " + typeof(T).Name;
                }
                else if (this != s_instance)
                {
                    Destroy(gameObject);
                }
            }
        }

        #endregion API Methods
    }
}