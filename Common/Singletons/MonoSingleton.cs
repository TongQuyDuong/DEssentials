using System;
using UnityEngine;

namespace Dessentials.Common
{
    public class MonoSingleton<T> : MonoBehaviour, IDisposable where T : Component
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
                            return s_instance;
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
                    s_instance = this as T;
                else if (this != s_instance)
                    Destroy(gameObject);
            }
        }

        #endregion API Methods

        #region Class Methods

        public virtual void Dispose() { }

        #endregion Class Methods
    }
}