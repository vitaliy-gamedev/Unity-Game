using Unity.Cinemachine;
using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Показує пошкодження авто через іскри, слабкий імпульс камери й дим при низькому HP.
    /// Не змінює саме здоров'я: лише реагує на події Health.
    /// CinemachineImpulseSource додає тряску до налаштованої камери без власного CameraFollow.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class CarDamageFeedback : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulse;
        [SerializeField] private ParticleSystem sparks;
        [SerializeField] private ParticleSystem smoke;
        private Health health;
        private float nextImpulse;

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable()
        {
            // Damaged означає новий удар, Changed — будь-яке оновлення HP, включно з рестартом.
            health.Damaged += OnHit;
            health.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            health.Damaged -= OnHit;
            health.Changed -= OnHealthChanged;
        }

        private void OnHit(DamageHit hit)
        {
            // Точка пошкодження приходить із Health, тому іскри з'являються біля удару,
            // а не завжди в центрі машини.
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparks.transform.position = hit.Point;
            sparks.Play();
            // Обмежуємо частоту саме тряски: кілька одночасних зомбі не повинні
            // складати надмірний імпульс, але кожне попадання все ще показує іскри.
            if (Time.time < nextImpulse) return;
            nextImpulse = Time.time + 0.16f;
            impulse.GenerateImpulseWithVelocity(new Vector3(0.035f, 0.1f, 0));
        }

        private void OnHealthChanged(int current, int maximum)
        {
            // Дим — індикатор живого авто з HP до 30%, а не ще одна подія завдання шкоди.
            bool low = current > 0 && (float)current / maximum <= 0.3f;
            if (low && !smoke.isPlaying) smoke.Play();
            else if (!low) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (current == maximum)
            {
                // Повне відновлення означає чистий заїзд без залишкових ефектів і cooldown тряски.
                sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                nextImpulse = 0;
            }
        }
    }
}
