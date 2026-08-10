using System;
using System.Collections.Generic;

namespace GameFoundation.Core
{
    /// <summary>
    /// Simple static service registry used across all GameFoundation systems.
    /// Register services in your Bootstrap/GameInstaller, resolve them anywhere with Get&lt;T&gt;().
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        public static void Register<T>(T instance) where T : class
        {
            Services[typeof(T)] = instance;
        }

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out var service))
                return service as T;

            UnityEngine.Debug.LogError($"[ServiceLocator] Service of type {typeof(T)} is not registered.");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var raw))
            {
                service = raw as T;
                return service != null;
            }
            service = null;
            return false;
        }

        public static void Unregister<T>() where T : class
        {
            Services.Remove(typeof(T));
        }

        /// <summary>
        /// Call this when returning to Bootstrap or restarting the app in the editor,
        /// otherwise services survive domain reloads via static state and go stale.
        /// </summary>
        public static void Clear()
        {
            Services.Clear();
        }
    }
}
