using System;
using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Незмінний опис уже завданої шкоди для візуальних/звукових реакцій.
    /// Передаємо значення, а не посилання на кулю: кулю можуть одразу повернути в пул.
    /// </summary>
    public readonly struct DamageHit
    {
        public readonly int Amount;
        public readonly Vector3 Point;
        public readonly Vector3 Direction;

        public DamageHit(int amount, Vector3 point, Vector3 direction)
        {
            Amount = amount;
            Point = point;
            // Нормалізований напрям робить силу візуального поштовху незалежною
            // від довжини вектора пострілу. Нульовий напрям залишається допустимим.
            Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
        }
    }

    /// <summary>
    /// Спільна модель HP для авто, ворога й бочки.
    /// Знає лише про здоров'я та події; не вирішує, як показати смерть або завершити гру.
    /// Це дає змогу повторно використати компонент без залежностей від UI та конкретного AI.
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximum = 100;
        public int Current { get; private set; }
        public int Maximum => maximum;
        public bool IsAlive => Current > 0;
        public event Action<int, int> Changed;
        public event Action<DamageHit> Damaged;
        public event Action Died;

        // Початкове HP задаємо при ініціалізації компонента, а не на першому Update.
        private void Awake() => Current = maximum;

        public void ResetHealth()
        {
            // Відновлення оновлює індикатори, але не запускає ефекти попадання.
            Current = maximum;
            Changed?.Invoke(Current, maximum);
        }

        // Скорочений виклик підходить для шкоди без відомої точки попадання.
        public void ApplyDamage(int damage) => ApplyDamage(damage, transform.position + Vector3.up * 0.8f, Vector3.zero);

        public void ApplyDamage(int damage, Vector3 point, Vector3 direction)
        {
            // Захист від повторної смерті та некоректної від'ємної шкоди.
            if (!IsAlive || damage <= 0) return;
            // Подія повідомляє фактично зняте HP, навіть якщо постріл сильніший за залишок.
            int applied = Mathf.Min(Current, damage);
            Current = Mathf.Max(0, Current - damage);
            // Спочатку нове HP і реакція на удар, потім смерть: підписники вже бачать
            // актуальний стан і можуть показати останнє попадання перед анімацією падіння.
            Changed?.Invoke(Current, maximum);
            Damaged?.Invoke(new DamageHit(applied, point, direction));
            if (!IsAlive) Died?.Invoke();
        }
    }
}
