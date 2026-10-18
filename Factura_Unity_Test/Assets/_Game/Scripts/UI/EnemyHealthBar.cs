using UnityEngine;
using UnityEngine.UI;

namespace Factura
{
    [RequireComponent(typeof(Health))]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private GameObject barRoot;
        [SerializeField] private Image fill;
        private Health health;
        private Transform view;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (Camera.main) view = Camera.main.transform;
        }

        private void OnEnable()
        {
            health.Changed += ShowHealth;
            ShowHealth(health.Current, health.Maximum);
        }

        private void OnDisable() => health.Changed -= ShowHealth;

        private void ShowHealth(int current, int maximum)
        {
            fill.rectTransform.anchorMax = new Vector2((float)current / maximum, 1);
            barRoot.SetActive(current > 0 && current < maximum);
        }

        private void LateUpdate()
        {
            if (barRoot.activeSelf && view) barRoot.transform.rotation = view.rotation;
        }
    }
}
