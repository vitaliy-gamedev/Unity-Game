using Unity.Cinemachine;
using UnityEngine;

namespace Factura
{
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
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparks.transform.position = hit.Point;
            sparks.Play();
            if (Time.time < nextImpulse) return;
            nextImpulse = Time.time + 0.16f;
            impulse.GenerateImpulseWithVelocity(new Vector3(0.035f, 0.1f, 0));
        }

        private void OnHealthChanged(int current, int maximum)
        {
            bool low = current > 0 && (float)current / maximum <= 0.3f;
            if (low && !smoke.isPlaying) smoke.Play();
            else if (!low) smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (current == maximum)
            {
                sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                nextImpulse = 0;
            }
        }
    }
}
