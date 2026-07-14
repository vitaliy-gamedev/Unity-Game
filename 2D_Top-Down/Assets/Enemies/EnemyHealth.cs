using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyBase))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private EnemyConfigSO _config;

    [Header("Events")]
    [SerializeField] private UnityEngine.Events.UnityEvent OnEnemyDied;

    public event Action OnDeath;

    private int _currentHealth;
    private bool _isDead;

    private SpriteRenderer _spriteRenderer;

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
        _currentHealth = _config != null ? _config.MaxHealth : 1;
        _isDead = false;
    }

    public void TakeDamage(int damage)
    {
        if (_isDead || damage <= 0) return;

        _currentHealth -= damage;

        if (_spriteRenderer != null)
        {
            StartCoroutine(FlashRed());
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRed()
    {
        if (_spriteRenderer == null) yield break;

        Color original = _spriteRenderer.color;
        _spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        _spriteRenderer.color = original;
    }

    private void Die()
    {
        _isDead = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        AudioManager.Instance?.PlayEnemyDeath();

        if (_config != null && GameManager.Instance != null)
        {
            GameManager.Instance.CurrentScore += _config.ScoreReward;
        }

        OnDeath?.Invoke();
        OnEnemyDied?.Invoke();

        VFXManager.Instance?.PlayEnemyDeath(transform.position);

        Destroy(gameObject, 0.3f);
    }
}
