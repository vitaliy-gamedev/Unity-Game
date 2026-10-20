using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Готує ворогів перед заїздом, задає їм ціль, розставляє бочку й рахує вбивства.
    /// Один генератор SpawnBatches обслуговує і async-запуск гри, і синхронні перевірки.
    /// Async тут розподіляє роботу між кадрами головного потоку, а не запускає Unity API у Task.Run.
    /// </summary>
    public sealed class Spawner : MonoBehaviour
    {
        [SerializeField] private EnemyAI prefab;
        [SerializeField, Min(1)] private int enemyCount = 18;
        [SerializeField, Min(1)] private float routeLength = 100;
        [SerializeField, Min(0)] private float safeStartDistance = 18;
        [SerializeField, Min(0)] private float halfWidth = 4;
        [SerializeField, Min(0.5f)] private float minimumSpacing = 2.5f;
        [SerializeField] private bool useFixedSeed;
        [SerializeField] private int seed = 41;
        [SerializeField] private ExplosiveBarrel barrel;
        // Сам список зберігається між рестартами. Турель і вороги отримують його
        // як IReadOnlyList, тому бачать актуальне наповнення без повторного зв'язування.
        private readonly List<EnemyAI> enemies = new();
        public IReadOnlyList<EnemyAI> Enemies => enemies;
        public int KilledCount { get; private set; }
        public event Action<int> KillsChanged;
        public ExplosiveBarrel Barrel => barrel;
        public bool IsPreparing { get; private set; }

        // Синхронний шлях призначений для контрольованих Editor-перевірок.
        // У звичайній грі координатор використовує PrepareAsync, щоб не створювати все в одному кадрі.
        public void Prepare(Collider car, Health carHealth)
        {
            if (IsPreparing) throw new InvalidOperationException("Enemy preparation is already running.");
            foreach (int batch in SpawnBatches(car, carHealth)) { }
        }

        public async Awaitable PrepareAsync(Collider car, Health carHealth, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (IsPreparing) throw new InvalidOperationException("Enemy preparation is already running.");
            IsPreparing = true;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                // Кожен yield генератора означає готову пачку; NextFrameAsync
                // віддає керування кадру і враховує скасування життєвого циклу сцени.
                foreach (int batch in SpawnBatches(car, carHealth))
                    await Awaitable.NextFrameAsync(cancellation);
                cancellation.ThrowIfCancellationRequested();
            }
            catch
            {
                // Частково створений рівень не залишаємо ні при помилці, ні при скасуванні.
                ClearEnemies();
                throw;
            }
            finally { IsPreparing = false; }
        }

        private void ClearEnemies()
        {
            foreach (var enemy in enemies)
            {
                if (!enemy) continue;
                enemy.GetComponent<Health>().Died -= OnEnemyDied;
                // Destroy виконається відкладено, тому відразу вимикаємо старих ворогів,
                // щоб вони не били авто й не потрапляли у фізичні запити нового заїзду.
                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
            enemies.Clear();
        }

        private IEnumerable<int> SpawnBatches(Collider car, Health carHealth)
        {
            ClearEnemies();
            KilledCount = 0;
            KillsChanged?.Invoke(0);
            // Фіксований seed дає відтворювані перевірки; звичайний режим змінює розстановку.
            // Окремий System.Random не змінює глобальну послідовність UnityEngine.Random.
            var random = useFixedSeed ? new System.Random(seed) : new System.Random();
            float start = car.transform.position.z + safeStartDistance;
            float end = car.transform.position.z + routeLength - 5;
            for (int index = 0; index < enemyCount; index++)
            {
                Vector3 position = default;
                bool found = false;
                // Обмежуємо спроби розміщення: надмірні enemyCount/minimumSpacing
                // не повинні спричинити нескінченний цикл у маленькій області.
                for (int attempt = 0; attempt < 64; attempt++)
                {
                    position = new Vector3(car.transform.position.x + Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble()),
                        0, Mathf.Lerp(start, end, (float)random.NextDouble()));
                    found = true;
                    foreach (var other in enemies)
                    {
                        if ((other.transform.position - position).sqrMagnitude >= minimumSpacing * minimumSpacing) continue;
                        found = false;
                        break;
                    }
                    if (found) break;
                }
                if (!found)
                {
                    Debug.LogWarning($"Could place only {enemies.Count} of {enemyCount} enemies within the configured bounds.", this);
                    break;
                }
                var enemy = Instantiate(prefab, position, Quaternion.Euler(0, 180, 0), transform);
                enemy.name = $"Enemy {index + 1:00}";
                enemy.Initialize(car, carHealth, enemies);
                enemy.GetComponent<Health>().Died += OnEnemyDied;
                enemies.Add(enemy);
                // Не більше чотирьох створень між точками повернення керування.
                // Остання неповна пачка завершує генератор без зайвого очікування кадру.
                if ((index + 1) % 4 == 0 && index + 1 < enemyCount) yield return index + 1;
            }
            PlaceBarrelNearGroup(car.transform.position.x);
        }

        private void PlaceBarrelNearGroup(float centerX)
        {
            // Вибираємо місце біля щільнішої групи, щоб додатковий об'єкт мав
            // тактичний сенс, а не випадково стояв далеко від усіх ворогів.
            if (!barrel || enemies.Count == 0) return;
            Vector3 best = enemies[0].transform.position;
            int most = -1;
            for (int index = 0; index < enemies.Count; index++)
            {
                Vector3 candidate = enemies[index].transform.position + new Vector3(1.2f, 0, 1.2f);
                candidate.x = Mathf.Clamp(candidate.x, centerX - halfWidth, centerX + halfWidth);
                int nearby = 0;
                for (int other = 0; other < enemies.Count; other++)
                    if ((enemies[other].transform.position - candidate).sqrMagnitude <= 16) nearby++;
                if (nearby <= most) continue;
                most = nearby;
                best = candidate;
            }
            barrel.ResetAt(best);
        }

        // Health повідомляє про смерть один раз; бочка не входить у цей лічильник.
        private void OnEnemyDied() => KillsChanged?.Invoke(++KilledCount);

        public void SetCombatActive(bool value)
        {
            // У Ready/Win/Lose вороги лишаються в сцені, але не переслідують і не атакують авто.
            foreach (var enemy in enemies) if (enemy) enemy.SetCombatActive(value);
        }
    }
}
