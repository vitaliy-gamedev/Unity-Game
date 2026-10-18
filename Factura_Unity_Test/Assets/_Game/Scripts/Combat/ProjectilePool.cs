using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Factura
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile prefab;
        [SerializeField, Min(1)] private int prewarmCount = 24;
        private ObjectPool<Projectile> pool;
        private readonly HashSet<Projectile> active = new();
        public int ActiveCount => active.Count;
        public int CreatedCount => pool?.CountAll ?? 0;
        public float HitRadius => prefab.HitRadius;
        public LayerMask TargetLayers => prefab.TargetLayers;
        public event Action<Vector3, Vector3> Impact;

        public void ReportImpact(Vector3 point, Vector3 normal) => Impact?.Invoke(point, normal);

        private void Awake()
        {
            pool = new ObjectPool<Projectile>(Create, null, Return, DestroyProjectile, true, prewarmCount, 64);
            var reserve = new Projectile[prewarmCount];
            for (int i = 0; i < reserve.Length; i++) reserve[i] = pool.Get();
            foreach (var projectile in reserve) pool.Release(projectile);
        }

        private Projectile Create()
        {
            var projectile = Instantiate(prefab, transform);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private void Return(Projectile projectile) => projectile.gameObject.SetActive(false);
        private void DestroyProjectile(Projectile projectile) => Destroy(projectile.gameObject);

        public void Fire(Vector3 position, Vector3 direction) => Fire(position, direction, position);

        public void Fire(Vector3 position, Vector3 direction, Vector3 barrelOrigin)
        {
            var projectile = pool.Get();
            active.Add(projectile);
            projectile.Launch(this, position, direction, barrelOrigin);
        }

        public void Release(Projectile projectile)
        {
            if (active.Remove(projectile)) pool.Release(projectile);
        }

        public void ClearActive()
        {
            while (active.Count > 0)
            {
                using var iterator = active.GetEnumerator();
                iterator.MoveNext();
                iterator.Current.ReturnToPool();
            }
        }

        private void OnDestroy() => pool?.Dispose();
    }
}
