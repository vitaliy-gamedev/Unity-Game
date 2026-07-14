using UnityEngine;
using TMPro;

public class HUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _itemsText;
    [SerializeField] private GameObject _hudPanel;

    private PlayerHealth _playerHealth;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += UpdateHealthDisplay;
                UpdateHealthDisplay(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
            }
        }

        UpdateScoreDisplay();
    }

    private void Update()
    {
        UpdateScoreDisplay();
    }

    private void UpdateHealthDisplay(int current, int max)
    {
        if (_healthText != null)
        {
            _healthText.text = $"HP: {current}/{max}";
        }
    }

    private void UpdateScoreDisplay()
    {
        if (_scoreText != null && GameManager.Instance != null)
        {
            _scoreText.text = $"Score: {GameManager.Instance.CurrentScore}";
        }
    }

    public void UpdateItemsDisplay(int collected, int total)
    {
        if (_itemsText != null)
        {
            _itemsText.text = $"Items: {collected}/{total}";
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= UpdateHealthDisplay;
        }
    }
}
