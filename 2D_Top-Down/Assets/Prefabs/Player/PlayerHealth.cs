using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO _config;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnDamaged;
    public UnityEngine.Events.UnityEvent OnDied;
    public UnityEngine.Events.UnityEvent OnHealed;

    public event Action<int, int> OnHealthChanged;

    private int _currentHealth;
    private bool _isInvulnerable;
    private bool _isDead;

    private SpriteRenderer _spriteRenderer;

    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _config != null ? _config.MaxHealth : 3;
    public bool IsDead => _isDead;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        _currentHealth = _config != null ? _config.MaxHealth : 3;
        _isDead = false;
        _isInvulnerable = false;
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (_isDead || _isInvulnerable || damage <= 0) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0, _currentHealth);

        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        OnDamaged?.Invoke();

        AudioManager.Instance?.PlayPlayerHit();

        if (_currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartInvulnerability();
        }
    }

    public void Heal(int amount)
    {
        if (_isDead || amount <= 0) return;

        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, MaxHealth);

        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        OnHealed?.Invoke();
    }

    private void Die()
    {
        _isDead = true;
        OnDied?.Invoke();

        GameStateManager.Instance?.SetLose();
    }

    private void StartInvulnerability()
    {
        if (_config == null) return;
        StartCoroutine(InvulnerabilityCoroutine());
    }

    private IEnumerator InvulnerabilityCoroutine()
    {
        _isInvulnerable = true;

        float elapsed = 0f;
        float duration = _config != null ? _config.InvulnerabilityDuration : 1f;

        while (elapsed < duration)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = !_spriteRenderer.enabled;

            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        if (_spriteRenderer != null)
            _spriteRenderer.enabled = true;

        _isInvulnerable = false;
    }
}
