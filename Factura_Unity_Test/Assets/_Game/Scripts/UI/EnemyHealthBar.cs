using UnityEngine;
using UnityEngine.UI;

namespace Factura
{
    /// <summary>
    /// Індикатор HP над ворогом. Підписується на Health, а не опитує HP кожного кадру.
    /// Повний або вже мертвий ворог не показує смужку, щоб не захаращувати мобільний екран.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private GameObject barRoot;
        [SerializeField] private Image fill;
        private Health health;
        private Transform view;

        private void Awake()
        {
            // Камера одна на сцену; зберігаємо Transform для billboard-повороту смужки.
            health = GetComponent<Health>();
            if (Camera.main) view = Camera.main.transform;
        }

        private void OnEnable()
        {
            health.Changed += ShowHealth;
            // Початкове відображення потрібне й без нової події пошкодження.
            ShowHealth(health.Current, health.Maximum);
        }

        private void OnDisable() => health.Changed -= ShowHealth;

        private void ShowHealth(int current, int maximum)
        {
            // Змінюємо правий anchor заповнення, а не масштабуємо весь Canvas з рамкою.
            fill.rectTransform.anchorMax = new Vector2((float)current / maximum, 1);
            barRoot.SetActive(current > 0 && current < maximum);
        }

        private void LateUpdate()
        {
            // Після руху камери повертаємо активний world-space UI до її площини.
            if (barRoot.activeSelf && view) barRoot.transform.rotation = view.rotation;
        }
    }
}
