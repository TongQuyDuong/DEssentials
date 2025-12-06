using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dessentials.Common.ServiceLocator
{
    public class ServiceLocator : PersistentMonoSingleton<ServiceLocator>
    {
        readonly Dictionary<Type, object> services = new();
        public IEnumerable<object> RegisteredServices => services.Values;

        public static ServiceLocator Global
            => Instance;
        
        public bool TryGet<T>(out T service) where T : class {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object obj)) {
                service = obj as T;
                return true;
            }

            service = null;
            return false;
        }

        public T Get<T>() where T : class {
            Type type = typeof(T);
            if (services.TryGetValue(type, out object obj)) {
                return obj as T;
            }
            
            throw new ArgumentException($"ServiceManager.Get: Service of type {type.FullName} not registered");
        }

        public void Register<T>(T service) {
            Type type = typeof(T);
            
            if (!services.TryAdd(type, service)) {
                Debug.LogError($"ServiceManager.Register: Service of type {type.FullName} already registered");
            }
        }

        public void Register(Type type, object service) {
            if (!type.IsInstanceOfType(service)) {
                throw new ArgumentException("Type of service does not match type of service interface", nameof(service));
            }
            
            if (!services.TryAdd(type, service)) {
                Debug.LogError($"ServiceManager.Register: Service of type {type.FullName} already registered");
            }
        }
    }
}
