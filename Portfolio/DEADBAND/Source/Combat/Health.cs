using System.Collections;
using System;
using UnityEngine;

namespace Deadband.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField] private bool disableOnDeath = true;
        [SerializeField, Min(0f)] private float deathDelay = 0.12f;

        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private float currentHealth;
        private Color baseEmission = Color.black;

        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;
        public float MaximumHealth => maximumHealth;
        public event Action<float, float> HealthChanged;
        public event Action Died;

        public bool Heal(float amount)
        {
            if (!IsAlive || amount <= 0f || currentHealth >= maximumHealth) return false;
            currentHealth = Mathf.Min(maximumHealth, currentHealth + amount);
            HealthChanged?.Invoke(currentHealth, maximumHealth);
            return true;
        }

        public void SetBaseEmission(Color color)
        {
            baseEmission = color;
            if (IsAlive) SetEmission(baseEmission);
        }

        public void Configure(float health, bool shouldDisableOnDeath = true)
        {
            maximumHealth = Mathf.Max(1f, health);
            disableOnDeath = shouldDisableOnDeath;
            currentHealth = maximumHealth;
        }

        private void Awake()
        {
            currentHealth = maximumHealth;
            renderers = GetComponentsInChildren<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
        }

        public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (!IsAlive || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth, maximumHealth);
            if (!IsAlive)
            {
                Died?.Invoke();
            }
            StopAllCoroutines();
            StartCoroutine(IsAlive ? FlashDamage() : Die());
        }

        private IEnumerator FlashDamage()
        {
            SetEmission(new Color(1f, 0.12f, 0.05f) * 4f);
            yield return new WaitForSeconds(0.08f);
            SetEmission(baseEmission);
        }

        private IEnumerator Die()
        {
            SetEmission(Color.white * 5f);
            yield return new WaitForSeconds(deathDelay);
            if (disableOnDeath)
            {
                gameObject.SetActive(false);
            }
            else
            {
                SetEmission(Color.black);
            }
        }

        private void SetEmission(Color color)
        {
            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
