using System;
using UnityEngine;

namespace Factura
{
    public readonly struct DamageHit
    {
        public readonly int Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;

        public DamageHit(int amount, Vector3 point, Vector3 direction)
        {
            Amount = amount;
            Point = point;
            Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
        }
    }

    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximum = 100;
        public int Current { get; private set; }
        public int Maximum => maximum;
        public bool IsAlive => Current > 0;
        public event Action<int, int> Changed;
        public event Action<DamageHit> Damaged;
        public event Action Died;

        private void Awake() => Current = maximum;

        public void ResetHealth()
        {
            Current = maximum;
            Changed?.Invoke(Current, maximum);
        }

        public void ApplyDamage(int damage) => ApplyDamage(damage, transform.position + Vector3.up * 0.8f, Vector3.zero);

        public void ApplyDamage(int damage, Vector3 point, Vector3 direction)
        {
            if (!IsAlive || damage <= 0) return;
            int applied = Mathf.Min(Current, damage);
            Current = Mathf.Max(0, Current - damage);
            Changed?.Invoke(Current, maximum);
            Damaged?.Invoke(new DamageHit(applied, point, direction));
            if (!IsAlive) Died?.Invoke();
        }
    }
}
