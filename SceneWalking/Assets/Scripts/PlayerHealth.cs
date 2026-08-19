using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    public event Action OnDeath;
    public event Action<float> OnHealthChanged;

    private PlayerRespawn respawn;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        respawn = GetComponent<PlayerRespawn>();
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        if (respawn != null && respawn.IsInvulnerable) return;

        CurrentHealth = Mathf.Clamp(
            CurrentHealth - damage,
            0,
            maxHealth
        );

        Debug.Log($"Player HP: {CurrentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(CurrentHealth);

        if (IsDead)
        {
            Debug.Log("Player died!");
            OnDeath?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Clamp(
            CurrentHealth + amount,
            0,
            maxHealth
        );

        OnHealthChanged?.Invoke(CurrentHealth);
    }

    public void FullHeal()
    {
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth);
    }
}