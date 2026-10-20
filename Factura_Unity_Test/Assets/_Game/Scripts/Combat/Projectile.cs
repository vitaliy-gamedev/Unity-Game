// Снаряд із незмінним напрямом, перевіркою відрізка польоту та поверненням у пул.
using UnityEngine;

namespace Factura
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(1)] private float speed = 35;
        [SerializeField, Min(0.01f)] private float lifetime = 2;
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField, Min(0.01f)] private float hitRadius = 0.1f;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask environmentLayers;
        // Фіксований NonAlloc-буфер розрахований на цю сцену; щільніша геометрія потребує перегляду місткості.
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
            owner = pool;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            direction = forward.normalized;
            remainingTime = lifetime;
            inFlight = true;
            gameObject.SetActive(true);
            if (trail) trail.Clear();
            // Перевіряємо й довжину ствола: ціль між його основою та дулом не повинна ставати невразливою.
            Vector3 barrel = position - barrelOrigin;
            float barrelLength = barrel.magnitude;
            if (barrelLength > 0.001f && Physics.SphereCast(barrelOrigin, hitRadius, barrel / barrelLength,
                    out var obstruction, barrelLength, targetLayers | environmentLayers, QueryTriggerInteraction.Collide))
            {
                Hit(obstruction.collider, obstruction.point, obstruction.normal);
                return;
            }
            // Окремо враховуємо появу кулі вже всередині хітбокса.
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
            // Sweep відсікає проліт крізь тонку ціль між кроками; Collide враховує trigger-хітбокси.
            if (Physics.SphereCast(transform.position, hitRadius, direction, out var hit, distance, targetLayers | environmentLayers, QueryTriggerInteraction.Collide))
            {
                Hit(hit.collider, hit.point, hit.normal);
                return;
            }
            transform.position += direction * distance;
            remainingTime -= Time.fixedDeltaTime;
            if (remainingTime <= 0) ReturnToPool();
        }

        private void Hit(Collider collider, Vector3 point, Vector3 normal)
        {
            var target = collider.GetComponentInParent<Health>();
            if (target) target.ApplyDamage(damage, point, direction);
            owner.ReportImpact(point, normal);
            transform.position = point;
            ReturnToPool();
        }

        public void ReturnToPool()
        {
            // Прапорець захищає від подвійного повернення в пул.
            if (!inFlight) return;
            inFlight = false;
            if (trail) trail.Clear();
            owner.Release(this);
        }
    }
}
