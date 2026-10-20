// Повторне використання обмеженого набору ефектів попадання та вибуху.
using UnityEngine;

namespace Factura
{
    public sealed class CombatEffects : MonoBehaviour
    {
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private ParticleSystem impactPrefab;
        [SerializeField, Min(1)] private int capacity = 12;
        private ParticleSystem[] impacts;
        private int cursor;

        private void Awake()
        {
            impacts = new ParticleSystem[capacity];
            for (int index = 0; index < impacts.Length; index++)
                impacts[index] = Instantiate(impactPrefab, transform);
        }

        private void OnEnable() => projectiles.Impact += PlayImpact;

        private void OnDisable()
        {
            projectiles.Impact -= PlayImpact;
            Clear();
        }

        private void PlayImpact(Vector3 point, Vector3 normal)
        {
            var effect = impacts[cursor];
            cursor = (cursor + 1) % impacts.Length;
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up));
            effect.Play();
        }

        public void Clear()
        {
            if (impacts == null) return;
            foreach (var effect in impacts) if (effect) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            cursor = 0;
        }
    }
}
