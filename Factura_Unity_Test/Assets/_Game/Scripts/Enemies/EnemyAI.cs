using System;
using System.Collections.Generic;
using UnityEngine;

namespace Factura
{
    // Невеликий автомат станів зрозуміліший за набір незалежних прапорців поведінки.
    public enum EnemyState { Idle, Chase, Attack, Dead }

    /// <summary>
    /// Проста поведінка зомбі на відкритій прямій трасі: виявити авто, наздогнати та бити.
    /// Рух і дальність атаки визначаються відносно колайдера машини, а не лише її центра.
    /// NavMesh тут не потрібен, бо немає складної навігації навколо перешкод;
    /// якщо така геометрія з'явиться, пряме переслідування доведеться замінити.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Health), typeof(CapsuleCollider))]
    public sealed class EnemyAI : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 5.5f;
        [SerializeField, Min(1)] private float detectionRadius = 14;
        [SerializeField, Min(0.1f)] private float attackDistance = 0.8f;
        [SerializeField, Min(0.1f)] private float attackInterval = 0.7f;
        [SerializeField, Min(0)] private float attackWindup = 0.25f;
        [SerializeField, Min(1)] private int damage = 10;
        private Rigidbody body;
        private Health health;
        private CapsuleCollider hitbox;
        private Collider carCollider;
        private Health carHealth;
        private bool combatActive;
        private float attackTimer;
        private float staggerTime;
        private Vector3 pushDirection;
        private IReadOnlyList<EnemyAI> neighbours;
        public event Action Attacked;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsAlive => health && health.IsAlive;
        public bool IsCombatActive => combatActive;
        public float AttackWindupProgress => State == EnemyState.Attack
            ? Mathf.Clamp01(1 - attackTimer / Mathf.Max(0.01f, attackWindup)) : 0;
        public Vector3 AimPoint => hitbox.bounds.center;
        public float AimRadius => hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));

        private void Awake()
        {
            // RequireComponent фіксує необхідний склад префаба; компоненти кешуємо один раз.
            body = GetComponent<Rigidbody>();
            health = GetComponent<Health>();
            hitbox = GetComponent<CapsuleCollider>();
        }

        private void OnEnable()
        {
            // Підписки живуть стільки ж, скільки активний компонент; OnDisable їх знімає.
            health.Died += Die;
            health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            health.Died -= Die;
            health.Damaged -= OnDamaged;
        }

        public void Initialize(Collider target, Health targetHealth, IReadOnlyList<EnemyAI> otherEnemies)
        {
            // Динамічні вороги отримують конкретну ціль від Spawner, а не шукають авто
            // за тегом чи через глобальний Singleton. Список сусідів використовується лише для читання.
            carCollider = target;
            carHealth = targetHealth;
            neighbours = otherEnemies;
            State = EnemyState.Idle;
            attackTimer = attackWindup;
        }

        public void SetCombatActive(bool value) => combatActive = value;

        private void OnDamaged(DamageHit hit)
        {
            // Коротко перериваємо рух/атаку й штовхаємо в площині дороги.
            // Це керований відгук кінематичного тіла, а не фізична симуляція AddForce.
            staggerTime = 0.1f;
            pushDirection = hit.Direction;
            pushDirection.y = 0;
            pushDirection.Normalize();
        }

        private void FixedUpdate()
        {
            if (!combatActive || !IsAlive || !carHealth || !carHealth.IsAlive) return;
            if (staggerTime > 0)
            {
                staggerTime -= Time.fixedDeltaTime;
                body.MovePosition(body.position + pushDirection * (1.4f * Time.fixedDeltaTime));
                return;
            }
            Vector3 toCar = carCollider.bounds.center - body.position;
            toCar.y = 0;
            if (State == EnemyState.Idle)
            {
                // Порівняння квадратів відстаней не потребує квадратного кореня.
                // Після виявлення ворог продовжує переслідування, навіть якщо авто віддалилось.
                if (toCar.sqrMagnitude > detectionRadius * detectionRadius) return;
                State = EnemyState.Chase;
            }

            // ClosestPoint дає найближчу поверхню великого авто. Дистанція лише
            // до його центра змушувала б зомбі заходити всередину моделі заради атаки.
            Vector3 probe = body.position;
            probe.y = carCollider.bounds.center.y;
            Vector3 closest = carCollider.ClosestPoint(probe);
            closest.y = body.position.y;
            Vector3 toSurface = closest - body.position;
            float distance = toSurface.magnitude;
            // Якщо рухоме авто вже накрило ворога, виводимо його за найближчий
            // бік bounds. Так ворог не залишається всередині машини без можливості влучити в нього.
            if (distance < 0.01f)
            {
                var bounds = carCollider.bounds;
                Vector3 away = body.position - bounds.center;
                Vector3 outside = body.position;
                if (Mathf.Abs(away.x) / bounds.extents.x > Mathf.Abs(away.z) / bounds.extents.z)
                    outside.x = bounds.center.x + Mathf.Sign(away.x == 0 ? 1 : away.x) * (bounds.extents.x + attackDistance);
                else
                    outside.z = bounds.center.z + Mathf.Sign(away.z == 0 ? -1 : away.z) * (bounds.extents.z + attackDistance);
                body.MovePosition(Vector3.MoveTowards(body.position, outside, speed * Time.fixedDeltaTime));
                State = EnemyState.Chase;
                attackTimer = attackWindup;
                return;
            }
            if (toCar.sqrMagnitude > 0.01f)
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(toCar), 540 * Time.fixedDeltaTime));

            // Невеликий запас при вже активній атаці прибирає тремтіння Chase/Attack на межі дальності.
            float range = State == EnemyState.Attack ? attackDistance + 0.15f : attackDistance;
            if (distance > range)
            {
                State = EnemyState.Chase;
                attackTimer = attackWindup;
                // Рух до поверхні доповнюємо розведенням сусідів, але обмежуємо
                // загальну швидкість і довжину кроку, щоб не перестрибнути точку зупинки.
                Vector3 movement = toSurface.normalized * speed + Separation() * 2;
                movement = Vector3.ClampMagnitude(movement, speed) * Time.fixedDeltaTime;
                movement = Vector3.ClampMagnitude(movement, distance - attackDistance * 0.9f);
                body.MovePosition(body.position + movement);
                return;
            }

            State = EnemyState.Attack;
            // Маленька корекція дозволяє зберігати дистанцію до авто, яке продовжує їхати.
            float correction = Mathf.Clamp(distance - attackDistance * 0.9f, -speed * Time.fixedDeltaTime, speed * Time.fixedDeltaTime);
            body.MovePosition(body.position + toSurface / distance * correction);
            attackTimer -= Time.fixedDeltaTime;
            if (attackTimer > 0) return;
            // Перший удар має підготовку attackWindup, наступні — інтервал attackInterval.
            // Подія Attacked синхронізує видимий удар зі справжнім завданням шкоди.
            attackTimer += attackInterval;
            carHealth.ApplyDamage(damage, closest + Vector3.up * 0.8f, toCar);
            Attacked?.Invoke();
        }

        private Vector3 Separation()
        {
            // Локальне розведення не замінює пошук шляху. Для невеликої групи
            // простого перебору достатньо; при великих натовпах потрібен просторовий індекс.
            Vector3 steering = Vector3.zero;
            if (neighbours == null) return steering;
            for (int index = 0; index < neighbours.Count; index++)
            {
                var other = neighbours[index];
                if (!other || other == this || !other.IsAlive) continue;
                Vector3 delta = body.position - other.body.position;
                delta.y = 0;
                float distance = delta.magnitude;
                if (distance > 0.001f && distance < 0.85f) steering += delta / distance * (1 - distance / 0.85f);
            }
            return steering;
        }

        private void Die()
        {
            // Вимикаємо бойову поведінку й хітбокс негайно, але не знищуємо модель:
            // EnemyFeedback ще має показати падіння та поступове зникнення.
            State = EnemyState.Dead;
            combatActive = false;
            hitbox.enabled = false;
        }
    }
}
