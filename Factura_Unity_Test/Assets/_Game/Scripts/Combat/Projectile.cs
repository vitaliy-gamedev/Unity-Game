using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Один повторно використовуваний снаряд: політ, влучання, строк життя й повернення в пул.
    /// Напрям фіксується при запуску — куля не переслідує ціль після пострілу.
    /// Замість залежності від OnTriggerEnter перевіряємо весь пройдений відрізок SphereCast.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        // Одиниці параметрів: speed — одиниці світу/с, lifetime — секунди,
        // damage — цілі HP, hitRadius — одиниці світу. Сам float не зберігає ці одиниці.
        [SerializeField, Min(1)] private float speed = 35;
        [SerializeField, Min(0.01f)] private float lifetime = 2;
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField, Min(0.01f)] private float hitRadius = 0.1f;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask environmentLayers;
        // Повторно використовуємо буфер NonAlloc. Він має фіксовану місткість;
        // при щільнішій геометрії/кількох хітбоксах її потрібно окремо переглянути.
        private readonly Collider[] overlaps = new Collider[8];
        private ProjectilePool owner;
        private Vector3 direction;
        private float remainingTime;
        private bool inFlight;
        private TrailRenderer trail;
        public float HitRadius => hitRadius;
        public LayerMask TargetLayers => targetLayers;

        private void Awake() => trail = GetComponent<TrailRenderer>();

        public void Launch(ProjectilePool pool, Vector3 position, Vector3 forward, Vector3 barrelOrigin)
        {
            // Кожен запуск повністю відновлює стан об'єкта, який уже міг літати раніше.
            owner = pool;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            direction = forward.normalized;
            // Нормалізація прибирає довжину вектора: швидкість має задавати speed,
            // а не випадковий масштаб вхідного forward. Напрям залишається світовим.
            remainingTime = lifetime;
            inFlight = true;
            gameObject.SetActive(true);
            if (trail) trail.Clear();
            // Спочатку перевіряємо ділянку від основи ствола до дула.
            // Без цього близький ворог міг бути позаду точки появи кулі й стати невразливим.
            Vector3 barrel = position - barrelOrigin;
            float barrelLength = barrel.magnitude;
            if (barrelLength > 0.001f && Physics.SphereCast(barrelOrigin, hitRadius, barrel / barrelLength,
                    out var obstruction, barrelLength, targetLayers | environmentLayers, QueryTriggerInteraction.Collide))
            {
                Hit(obstruction.collider, obstruction.point, obstruction.normal);
                return;
            }
            // Окрема перевірка початкового перекриття потрібна, коли сама точка
            // появи вже всередині колайдера і звичайний sweep не знаходить вхід у нього.
            int count = Physics.OverlapSphereNonAlloc(position, hitRadius, overlaps, targetLayers | environmentLayers, QueryTriggerInteraction.Collide);
            for (int index = 0; index < count; index++)
            {
                var target = overlaps[index].GetComponentInParent<Health>();
                if (target && !target.IsAlive) continue;
                if (!target && (environmentLayers.value & (1 << overlaps[index].gameObject.layer)) == 0) continue;
                Vector3 point = overlaps[index].ClosestPoint(position);
                Hit(overlaps[index], point, -direction);
                break;
            }
        }

        private void FixedUpdate()
        {
            if (!inFlight) return;
            float distance = speed * Mathf.Min(remainingTime, Time.fixedDeltaTime);
            // Швидкість множимо на час і отримуємо шлях за крок.
            // В останньому кроці не пролітаємо довше, ніж лишилося lifetime.
            // Перевірка відрізка до переміщення захищає від прольоту швидкої кулі
            // крізь тонку ціль між фізичними кадрами. Радіус дає невеликий об'єм пострілу.
            // Collide явно враховує trigger-хітбокси ворогів незалежно від глобального прапорця.
            if (Physics.SphereCast(transform.position, hitRadius, direction, out var hit, distance, targetLayers | environmentLayers, QueryTriggerInteraction.Collide))
            {
                Hit(hit.collider, hit.point, hit.normal);
                return;
            }
            transform.position += direction * distance;
            remainingTime -= Time.fixedDeltaTime;
            // Навіть промах гарантовано повернеться в пул після завершення lifetime.
            if (remainingTime <= 0) ReturnToPool();
        }

        private void Hit(Collider collider, Vector3 point, Vector3 normal)
        {
            // Health може бути на батьківському об'єкті, а геометрія попадання — на дочірньому.
            // В оточення без Health куля теж влучає та показує ефект, але HP не змінює.
            var target = collider.GetComponentInParent<Health>();
            if (target) target.ApplyDamage(damage, point, direction);
            owner.ReportImpact(point, normal);
            transform.position = point;
            ReturnToPool();
        }

        public void ReturnToPool()
        {
            // Прапорець не дозволяє повернути ту саму кулю двічі з різних шляхів завершення.
            if (!inFlight) return;
            inFlight = false;
            if (trail) trail.Clear();
            owner.Release(this);
        }
    }
}
