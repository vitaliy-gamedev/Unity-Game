using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Pro.Pooling
{
    public interface IObjectPool : IDisposable
    {
    }

    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    /// <summary>
    /// Generic component pool. Compared to a naive Instantiate/Destroy approach,
    /// this avoids GC spikes and instantiation cost for frequently spawned objects
    /// (bullets, particles, UI list items, enemies).
    /// </summary>
    public class ObjectPool<T> : IObjectPool where T : MonoBehaviour
    {
        private readonly Queue<T> _pool = new();
        private readonly HashSet<T> _inactive = new();
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly bool _expandable;
        private readonly bool _prefabIsPoolable;

        public int CountInactive => _pool.Count;

        public ObjectPool(T prefab, int initialSize, bool expandable = true, Transform parent = null)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab), "[ObjectPool] Prefab cannot be null.");

            _prefab = prefab;
            _expandable = expandable;
            _parent = parent;
            _prefabIsPoolable = prefab is IPoolable;

            Prewarm(initialSize);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
                Return(CreateObject());
        }

        public T Spawn()
        {
            RemoveDestroyedEntries();

            if (_pool.Count == 0)
            {
                if (!_expandable)
                {
                    Debug.LogWarning($"[ObjectPool] Pool for '{_prefab.name}' is empty and not expandable.");
                    return null;
                }
                var created = CreateObject();
                Activate(created);
                return created;
            }

            var pooled = _pool.Dequeue();
            _inactive.Remove(pooled);
            Activate(pooled);
            return pooled;
        }

        public T Spawn(Vector3 position)
        {
            var obj = Spawn();
            if (obj != null) obj.transform.position = position;
            return obj;
        }

        public T Spawn(Vector3 position, Quaternion rotation)
        {
            var obj = Spawn();
            if (obj != null) obj.transform.SetPositionAndRotation(position, rotation);
            return obj;
        }

        public void Despawn(T obj)
        {
            if (obj == null) return;
            Return(obj);
        }

        private T CreateObject()
        {
            var obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            return obj;
        }

        private void Activate(T obj)
        {
            obj.gameObject.SetActive(true);
            if (_prefabIsPoolable && obj is IPoolable poolable)
                poolable.OnSpawn();
        }

        private void Return(T obj)
        {
            if (obj == null || !_inactive.Add(obj)) return;

            if (_prefabIsPoolable && obj is IPoolable poolable)
                poolable.OnDespawn();

            obj.gameObject.SetActive(false);
            if (_parent != null)
                obj.transform.SetParent(_parent);

            _pool.Enqueue(obj);
        }

        private void RemoveDestroyedEntries()
        {
            while (_pool.Count > 0 && _pool.Peek() == null)
            {
                var destroyed = _pool.Dequeue();
                _inactive.Remove(destroyed);
            }
        }

        /// <summary>Destroys every pooled instance. Call this from OnDestroy of whatever owns the pool.</summary>
        public void Dispose()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                    UnityEngine.Object.Destroy(obj.gameObject);
            }

            _inactive.Clear();
        }
    }
}
