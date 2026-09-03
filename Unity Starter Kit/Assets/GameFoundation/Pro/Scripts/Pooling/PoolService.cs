using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Pro.Pooling
{
    /// <summary>
    /// Register one of these in your Bootstrap and resolve it anywhere via
    /// ServiceLocator.Get&lt;PoolService&gt;(). Pools are created lazily on first
    /// GetPool&lt;T&gt;() call and keyed by prefab instance, so you never have to
    /// pre-declare every pool up front.
    /// </summary>
    public class PoolService : MonoBehaviour
    {
        [SerializeField] private int defaultPrewarmCount = 10;

        private readonly Dictionary<Object, object> _pools = new();

        public ObjectPool<T> GetPool<T>(T prefab, int? prewarmCount = null, Transform parent = null) where T : MonoBehaviour
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolService] Cannot create a pool for a null prefab.", this);
                return null;
            }

            if (_pools.TryGetValue(prefab, out var existing))
                return (ObjectPool<T>)existing;

            var pool = new ObjectPool<T>(prefab, prewarmCount ?? defaultPrewarmCount, expandable: true, parent);
            _pools[prefab] = pool;
            return pool;
        }

        public T Spawn<T>(T prefab, Transform parent = null) where T : MonoBehaviour
            => GetPool(prefab, parent: parent)?.Spawn();

        public void Despawn<T>(T prefab, T instance) where T : MonoBehaviour
        {
            if (instance == null) return;

            if (prefab == null)
            {
                Debug.LogWarning("[PoolService] Cannot return an instance without its prefab key — destroying it instead.");
                Destroy(instance.gameObject);
                return;
            }

            if (_pools.TryGetValue(prefab, out var existing))
                ((ObjectPool<T>)existing).Despawn(instance);
            else
            {
                Debug.LogWarning($"[PoolService] No pool exists yet for prefab '{prefab.name}' — destroying instance instead.");
                Destroy(instance.gameObject);
            }
        }

        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                if (pool is IObjectPool disposablePool)
                    disposablePool.Dispose();
            }

            _pools.Clear();
        }

        private void OnDestroy()
        {
            ClearAll();
        }
    }
}
