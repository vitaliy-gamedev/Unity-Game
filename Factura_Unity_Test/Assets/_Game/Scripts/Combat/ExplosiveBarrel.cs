using System;
using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Додаткова тактична ціль: отримує звичайну шкоду через Health і вибухає після смерті.
    /// Вибух уражає лише ворогів із заданої маски, не авто й не саму бочку повторно.
    /// Візуал, колайдер і HP відновлюються на початку наступного заїзду.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(CapsuleCollider))]
    public sealed class ExplosiveBarrel : MonoBehaviour
    {
        [SerializeField] private GameObject visual;
        [SerializeField] private ParticleSystem explosion;
        [SerializeField] private LayerMask enemyLayers;
        [SerializeField, Min(0.1f)] private float radius = 4.5f;
        [SerializeField, Min(1)] private int damage = 60;
        // Фіксований буфер уникає створення масиву на кожен вибух. Для сцени з
        // більшою кількістю хітбоксів цю місткість треба перевірити знову.
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
            // Повторно використовуємо одну бочку між заїздами разом з її ParticleSystem.
            transform.position = position;
            health.ResetHealth();
            visual.SetActive(true);
            hitbox.enabled = true;
            explosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Explode()
        {
            // Спершу прибираємо хітбокс, щоб наступні кулі не влучали в знищену бочку.
            // Health вже забороняє другу подію смерті, тому вибух виконується один раз.
            hitbox.enabled = false;
            visual.SetActive(false);
            explosion.Play();
            Exploded?.Invoke(AimPoint);
            // NonAlloc наповнює наш буфер; Collide враховує trigger-хітбокси ворогів.
            int count = Physics.OverlapSphereNonAlloc(AimPoint, radius, hits, enemyLayers, QueryTriggerInteraction.Collide);
            // count — скільки слотів масиву реально заповнено цим запитом.
            // Перебираємо лише їх, а не старі значення решти буфера від попереднього вибуху.
            for (int index = 0; index < count; index++)
            {
                // У поточному префабі один бойовий колайдер на корені ворога.
                // Якщо додати дочірні/декілька хітбоксів, знадобляться GetComponentInParent
                // та усунення дублікатів, інакше зміниться підрахунок шкоди.
                var enemy = hits[index].GetComponent<EnemyAI>();
                if (!enemy || !enemy.IsAlive) continue;
                Vector3 direction = enemy.AimPoint - AimPoint;
                enemy.GetComponent<Health>().ApplyDamage(damage, enemy.AimPoint, direction);
            }
        }
    }
}
