// Одноразовий вибух із радіальною шкодою та відновленням при повторному заїзді.
using System;
using UnityEngine;

namespace Factura
{
    [RequireComponent(typeof(Health), typeof(CapsuleCollider))]
    public sealed class ExplosiveBarrel : MonoBehaviour
    {
        [SerializeField] private GameObject visual;
        [SerializeField] private ParticleSystem explosion;
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField, Min(0.1f)] private float radius = 4.5f;
        [SerializeField, Min(1)] private int damage = 60;
        private readonly Collider[] hits = new Collider[32];
        private Health health;
        private CapsuleCollider hitbox;
        public event Action<Vector3> Exploded;
        public bool IsAlive => health && health.IsAlive;
        public Vector3 AimPoint => transform.position + Vector3.up * 0.65f;
        public float AimRadius => hitbox.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));

        private void Awake()
        {
            health = GetComponent<Health>();
            hitbox = GetComponent<CapsuleCollider>();
        }

        private void OnEnable() => health.Died += Explode;
        private void OnDisable() => health.Died -= Explode;

        public void ResetAt(Vector3 position)
        {
            transform.position = position;
            health.ResetHealth();
            visual.SetActive(true);
            hitbox.enabled = true;
            explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Explode()
        {
            hitbox.enabled = false;
            visual.SetActive(false);
            explosion.Play();
            Exploded?.Invoke(AimPoint);
            int count = Physics.OverlapSphereNonAlloc(AimPoint, radius, hits, enemyLayers, QueryTriggerInteraction.Collide);
            for (int index = 0; index < count; index++)
            {
                var enemy = hits[index].GetComponent<EnemyAI>();
                if (!enemy || !enemy.IsAlive) continue;
                Vector3 direction = enemy.AimPoint - AimPoint;
                enemy.GetComponent<Health>().ApplyDamage(damage, enemy.AimPoint, direction);
            }
        }
    }
}
