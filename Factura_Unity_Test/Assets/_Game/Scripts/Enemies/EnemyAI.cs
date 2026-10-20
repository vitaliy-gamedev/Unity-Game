// Пряме переслідування автомобіля на відкритій трасі без пошуку шляху навколо перешкод.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Factura
{
    public enum EnemyState { Idle, Chase, Attack, Dead }

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
            body = GetComponent<Rigidbody>();
            health = GetComponent<Health>();
            hitbox = GetComponent<CapsuleCollider>();
        }

        private void OnEnable()
        {
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
            carCollider = target;
            carHealth = targetHealth;
            neighbours = otherEnemies;
            State = EnemyState.Idle;
            attackTimer = attackWindup;
        }

        // Дозвіл бою незалежний від стану AI: після Win/Lose припиняються переслідування й атаки.
        public void SetCombatActive(bool value) => combatActive = value;

        private void OnDamaged(DamageHit hit)
        {
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
                if (toCar.sqrMagnitude > detectionRadius * detectionRadius) return;
                State = EnemyState.Chase;
            }

            Vector3 probe = body.position;
            probe.y = carCollider.bounds.center.y;
            // Дистанція до поверхні кузова, не його центра, не змушує ворога заходити всередину авто.
            Vector3 closest = carCollider.ClosestPoint(probe);
            closest.y = body.position.y;
            Vector3 toSurface = closest - body.position;
            float distance = toSurface.magnitude;
            // Якщо рухома машина накрила ворога, виводимо його до найближчого борту.
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

            // Гістерезис прибирає перемикання Chase/Attack на межі дальності.
            float range = State == EnemyState.Attack ? attackDistance + 0.15f : attackDistance;
            if (distance > range)
            {
                State = EnemyState.Chase;
                attackTimer = attackWindup;
                Vector3 movement = toSurface.normalized * speed + Separation() * 2;
                movement = Vector3.ClampMagnitude(movement, speed) * Time.fixedDeltaTime;
                movement = Vector3.ClampMagnitude(movement, distance - attackDistance * 0.9f);
                body.MovePosition(body.position + movement);
                return;
            }

            State = EnemyState.Attack;
            float correction = Mathf.Clamp(distance - attackDistance * 0.9f, -speed * Time.fixedDeltaTime, speed * Time.fixedDeltaTime);
            body.MovePosition(body.position + toSurface / distance * correction);
            attackTimer -= Time.fixedDeltaTime;
            if (attackTimer > 0) return;
            attackTimer += attackInterval;
            carHealth.ApplyDamage(damage, closest + Vector3.up * 0.8f, toCar);
            Attacked?.Invoke();
        }

        private Vector3 Separation()
        {
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
            State = EnemyState.Dead;
            combatActive = false;
            // Шкоду й хітбокс вимикаємо відразу; візуальна модель лишається для анімації смерті.
            hitbox.enabled = false;
        }
    }
}
