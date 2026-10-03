using Deadband.AI;
using UnityEngine;

namespace Deadband.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private string displayName = "HOSTILE";
        [SerializeField] private bool boss;
        [SerializeField, Min(0.5f)] private float worldHeight = 2.55f;
        [SerializeField, Min(5f)] private float visibleRange = 32f;

        private float damagedVisibleUntil;
        private MeleeEnemyController bossController;
        private HumanoidEnemyController humanoidController;

        public bool IsBoss => boss;

        public void Configure(Health targetHealth, string label, bool isBoss, float height)
        {
            health = targetHealth;
            displayName = label;
            boss = isBoss;
            worldHeight = height;
            visibleRange = isBoss ? 72f : 32f;
        }

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            bossController = GetComponent<MeleeEnemyController>();
            humanoidController = GetComponent<HumanoidEnemyController>();
        }

        private void OnEnable()
        {
            if (health != null) health.HealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            if (health != null) health.HealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float _, float __)
        {
            damagedVisibleUntil = Time.unscaledTime + 5f;
        }

        private void OnGUI()
        {
            if (health == null || !health.IsAlive || Camera.main == null) return;
            if (boss && bossController != null && !bossController.IsEngaged && Time.unscaledTime > damagedVisibleUntil)
            {
                return;
            }
            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            if (distance > visibleRange && Time.unscaledTime > damagedVisibleUntil) return;

            float normalized = Mathf.Clamp01(health.CurrentHealth / health.MaximumHealth);
            bool berserk = (bossController != null && bossController.IsBerserk) ||
                           (humanoidController != null && humanoidController.IsBerserk);
            if (boss)
            {
                DrawBossBar(normalized, berserk);
                return;
            }

            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * worldHeight);
            if (screen.z <= 0f) return;
            float width = Mathf.Lerp(72f, 48f, Mathf.Clamp01(distance / visibleRange));
            float left = screen.x - width * 0.5f;
            float top = Screen.height - screen.y;
            DrawBar(
                new Rect(left, top, width, berserk ? 8f : 6f),
                normalized,
                berserk ? new Color(1f, 0.05f, 0.015f) : new Color(0.92f, 0.18f, 0.1f));
        }

        private void DrawBossBar(float normalized, bool berserk)
        {
            float width = Mathf.Min(520f, Screen.width * 0.46f);
            float left = Screen.width * 0.5f - width * 0.5f;
            float top = 18f;
            GUIStyle nameStyle = new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.68f, 0.22f) }
            };
            GUI.Label(new Rect(left, top, width, 22f), berserk ? displayName + " // BERSERK" : displayName, nameStyle);
            DrawBar(
                new Rect(left, top + 23f, width, berserk ? 16f : 12f),
                normalized,
                berserk ? new Color(1f, 0.035f, 0.01f) : new Color(0.95f, 0.2f, 0.08f));
            GUI.Label(new Rect(left, top + 35f, width, 18f), $"{health.CurrentHealth:0} / {health.MaximumHealth:0}", nameStyle);
        }

        private static void DrawBar(Rect rect, float normalized, Color fill)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.015f, 0.018f, 0.016f, 0.92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, (rect.width - 2f) * normalized, rect.height - 2f), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
