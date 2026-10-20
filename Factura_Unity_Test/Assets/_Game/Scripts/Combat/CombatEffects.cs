using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Показує ефекти влучань, підписуючись на пул куль, а не на кожну окрему кулю.
    /// Невеликий кільцевий буфер ParticleSystem обмежує кількість створених ефектів.
    /// Якщо всі місця зайняті, нове влучання перезапускає найстаріше — це свідомий
    /// компроміс для коротких іскор, а не необмежене Instantiate на кожен постріл.
    /// </summary>
    public sealed class CombatEffects : MonoBehaviour
    {
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private ParticleSystem impactPrefab;
        [SerializeField, Min(1)] private int capacity = 12;
        private ParticleSystem[] impacts;
        private int cursor;

        private void Awake()
        {
            // Всі ефекти створюємо один раз до активного бою.
            impacts = new ParticleSystem[capacity];
            for (int index = 0; index < impacts.Length; index++)
                impacts[index] = Instantiate(impactPrefab, transform);
        }

        private void OnEnable() => projectiles.Impact += PlayImpact;

        private void OnDisable()
        {
            // Відписка не залишає обробник у пулі після вимкнення компонента.
            projectiles.Impact -= PlayImpact;
            Clear();
        }

        private void PlayImpact(Vector3 point, Vector3 normal)
        {
            // Обидва аргументи — Vector3, але сенс різний: point є світовою точкою,
            // normal є напрямом від поверхні. Їх не можна взаємозамінити.
            var effect = impacts[cursor];
            cursor = (cursor + 1) % impacts.Length;
            // При повторному використанні прибираємо старі частинки й ставимо ефект
            // у нову точку; нормаль поверхні визначає напрям розльоту іскор.
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal.sqrMagnitude > 0.001f ? normal : Vector3.up));
            effect.Play();
        }

        public void Clear()
        {
            // Очищення під час рестарту не створює нових об'єктів і не лишає старих іскор.
            if (impacts == null) return;
            foreach (var effect in impacts) if (effect) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            cursor = 0;
        }
    }
}
