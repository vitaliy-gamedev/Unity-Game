using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Візуальні реакції ворога: спалах, легка деформація, поштовх моделі та падіння після смерті.
    /// Працює з дочірнім Visual, тому ефект не змінює розміри бойового хітбокса.
    /// Рух кореня при попаданні належить EnemyAI, рух кісток — EnemyAnimation.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(EnemyAI))]
    public sealed class EnemyFeedback : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Renderer[] renderers;
        // ID шейдерної властивості кешуємо; MaterialPropertyBlock змінює вигляд
        // окремого ворога без копії спільного матеріалу через renderer.material.
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock properties;
        private Health health;
        private EnemyAI enemy;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 restScale;
        private Vector3 deathAxis = Vector3.left;
        private float flash;
        private float strike;
        private float deathTime = -1;
        private Vector3 hitOffset;

        private void Awake()
        {
            health = GetComponent<Health>();
            enemy = GetComponent<EnemyAI>();
            properties = new MaterialPropertyBlock();
            // Базова поза потрібна для обчислення ефекту від початкового стану,
            // а не для накопичення похибок масштабу чи повороту кожного кадру.
            restPosition = visual.localPosition;
            restRotation = visual.localRotation;
            restScale = visual.localScale;
        }

        private void OnEnable()
        {
            health.Damaged += OnHit;
            health.Died += OnDeath;
            enemy.Attacked += OnAttack;
        }

        private void OnDisable()
        {
            health.Damaged -= OnHit;
            health.Died -= OnDeath;
            enemy.Attacked -= OnAttack;
        }

        private void OnHit(DamageHit hit)
        {
            flash = 0.14f;
            Vector3 direction = transform.InverseTransformDirection(hit.Direction);
            // Напрям кулі переводимо у простір ворога. Це задає напрям падіння
            // з урахуванням того, куди ворог зараз повернутий.
            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.back;
            deathAxis = Vector3.Cross(Vector3.up, direction.normalized);
            hitOffset = direction.normalized * 0.1f;
        }

        private void OnDeath() => deathTime = 0;
        private void OnAttack() => strike = 0.12f;

        private void Update()
        {
            flash = Mathf.Max(0, flash - Time.deltaTime);
            float hitPulse = flash / 0.14f;
            properties.SetColor(Emission, new Color(2.4f, 2f, 1.5f) * hitPulse);
            // Матеріал має заздалегідь увімкнений варіант _EMISSION;
            // сам запис кольору у property block не вмикає shader keyword.
            foreach (var renderer in renderers) renderer.SetPropertyBlock(properties);

            if (deathTime >= 0)
            {
                // Падіння, коротке утримання тіла та зменшення перед приховуванням.
                // Не прибираємо модель миттєво: гравець має побачити результат пострілу.
                deathTime += Time.deltaTime;
                float fall = Mathf.SmoothStep(0, 1, deathTime / 0.42f);
                visual.localRotation = Quaternion.AngleAxis(88 * fall, deathAxis) * restRotation;
                visual.localPosition = restPosition + Vector3.up * (0.3f * fall);
                visual.localScale = restScale * (1 - Mathf.Clamp01((deathTime - 1.4f) / 0.3f));
                if (deathTime >= 1.7f)
                {
                    // Вимикаємо Visual та цей ефект; корінь прибере Spawner при новому заїзді.
                    visual.gameObject.SetActive(false);
                    enabled = false;
                }
                return;
            }

            strike = Mathf.Max(0, strike - Time.deltaTime);
            // Усі зміщення/деформації затухають до збереженої базової пози.
            visual.localPosition = restPosition + hitOffset * hitPulse
                + Vector3.forward * (Mathf.Sin(strike / 0.12f * Mathf.PI) * 0.12f);
            visual.localRotation = Quaternion.AngleAxis(7 * hitPulse, deathAxis) * restRotation;
            visual.localScale = Vector3.Scale(restScale, new Vector3(1 + 0.07f * hitPulse, 1 - 0.06f * hitPulse, 1 + 0.07f * hitPulse));
        }
    }
}
