using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Factura
{
    /// <summary>
    /// Власник пулу куль Unity ObjectPool та обліку активних снарядів.
    /// Часті постріли повторно використовують об'єкти замість пари Instantiate/Destroy.
    /// Вороги створюються рідко між заїздами, тому їхній життєвий цикл залишено у Spawner.
    /// </summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile prefab;
        [SerializeField, Min(1)] private int prewarmCount = 24;
        private ObjectPool<Projectile> pool;
        // Набір не містить дублікатів і дозволяє прибирати активну кулю без пошуку по списку.
        private readonly HashSet<Projectile> active = new();
        public int ActiveCount => active.Count;
        public int CreatedCount => pool?.CountAll ?? 0;
        public float HitRadius => prefab.HitRadius;
        // Властивості читають спільні параметри префаба. Турелі потрібна та сама
        // геометрія пострілу, що й реальній кулі, а не окремий довільний допуск.
        public LayerMask TargetLayers => prefab.TargetLayers;
        public event Action<Vector3, Vector3> Impact;

        public void ReportImpact(Vector3 point, Vector3 normal) => Impact?.Invoke(point, normal);

        private void Awake()
        {
            // defaultCapacity резервує внутрішнє сховище, але сам об'єкт кулі не створює.
            // 64 — межа збережених НЕактивних об'єктів, не жорсткий ліміт активних пострілів.
            pool = new ObjectPool<Projectile>(Create, null, Return, DestroyProjectile, true, prewarmCount, 64);
            // Спершу беремо всі кулі одночасно, потім повертаємо: послідовне Get/Release
            // в одному циклі лише багато разів використало б той самий єдиний об'єкт.
            var reserve = new Projectile[prewarmCount];
            for (int i = 0; i < reserve.Length; i++) reserve[i] = pool.Get();
            foreach (var projectile in reserve) pool.Release(projectile);
        }

        private Projectile Create()
        {
            // Instantiate потрібен лише для першого наповнення або зростання пулу.
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
            // Облік додаємо ДО Launch: близьке попадання може повернути кулю відразу в Launch.
            active.Add(projectile);
            projectile.Launch(this, position, direction, barrelOrigin);
        }

        public void Release(Projectile projectile)
        {
            // Лише куля, яка ще була в active, може бути передана назад у ObjectPool.
            if (active.Remove(projectile)) pool.Release(projectile);
        }

        public void ClearActive()
        {
            // ReturnToPool змінює active. Тому не використовуємо один foreach по всьому
            // набору, а кожної ітерації заново беремо одну кулю до його спорожнення.
            while (active.Count > 0)
            {
                using var iterator = active.GetEnumerator();
                iterator.MoveNext();
                iterator.Current.ReturnToPool();
            }
        }

        // Dispose звільняє об'єкти, що зберігаються в пулі, при знищенні його власника.
        private void OnDestroy() => pool?.Dispose();
    }
}
