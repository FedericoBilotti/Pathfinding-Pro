using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utilities
{
    [DefaultExecutionOrder(-1000)]
    public class ServiceLocator : Singleton<ServiceLocator>
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void RegisterService<T>(T service)
        {
            Type type = typeof(T);

            _services.TryAdd(type, service);
        }

        public bool TryGetService<T>(out T serviceOut)
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                serviceOut = (T)service;
                return true;
            }

            serviceOut = (T)(object)null;
            return false;
        }

        public T GetService<T>()
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            Debug.LogError($"The actual service doesn't exist {typeof(T)}");
            return (T)(object)null;
        }

        public void RemoveService<T>()
        {
            Type type = typeof(T);

            if (!_services.TryGetValue(type, out var service))
                Debug.LogError($"The actual service doesn't exist {type} + {service}");

            _services.Remove(type);
        }
    }
}