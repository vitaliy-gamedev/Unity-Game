using System;
using UnityEngine;

namespace Deadband.Combat
{
    public readonly struct CombatNoiseEvent
    {
        public CombatNoiseEvent(Vector3 position, float radius, GameObject source)
        {
            Position = position;
            Radius = radius;
            Source = source;
        }

        public Vector3 Position { get; }
        public float Radius { get; }
        public GameObject Source { get; }
    }

    public static class CombatNoiseSystem
    {
        public static event Action<CombatNoiseEvent> NoiseEmitted;

        public static void Emit(Vector3 position, float radius, GameObject source)
        {
            NoiseEmitted?.Invoke(new CombatNoiseEvent(position, Mathf.Max(0f, radius), source));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvents()
        {
            NoiseEmitted = null;
        }
    }
}
