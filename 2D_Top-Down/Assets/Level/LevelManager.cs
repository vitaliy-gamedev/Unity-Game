using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Level Config")]
    [SerializeField] private LevelConfigSO _levelConfig;

    [Header("Player Spawn")]
    [SerializeField] private Transform _playerSpawnPoint;

    [Header("Win Settings")]
    [SerializeField] private int _totalItemsOnLevel = 0;
    [SerializeField] private int _totalEnemiesOnLevel = 0;

    [Header("Lose Settings")]
    [SerializeField] private bool _hasTimeLimit = false;
    [SerializeField] private float _timeLimitSeconds = 120f;

    private int _itemsCollected;
    private int _enemiesKilled;
    private bool _isLevelComplete;
    private bool _isGameOver;
    private float _elapsedTime;

    private PlayerHealth _playerHealth;

    private void Start()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetPlaying();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameplayMusic();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            if (_playerHealth != null)
                _playerHealth.ResetHealth();

            if (_playerSpawnPoint != null)
                player.transform.position = _playerSpawnPoint.position;

            _playerHealth.OnDied.AddListener(OnPlayerDied);
        }

    }

    private void Update()
    {
        if (_isLevelComplete || _isGameOver) return;

        if (_hasTimeLimit)
        {
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= _timeLimitSeconds)
            {
                TimeUp();
            }
        }
    }

    public void OnItemCollected()
    {
        _itemsCollected++;

        CheckWinCondition();
    }

    public void OnEnemyKilled()
    {
        _enemiesKilled++;

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (_isLevelComplete) return;
        if (_levelConfig == null) return;

        bool itemsOk = _itemsCollected >= _levelConfig.RequiredItemCount;
        bool enemiesOk = !_levelConfig.RequiresAllEnemiesKilled || _enemiesKilled >= _totalEnemiesOnLevel;

        if (itemsOk && enemiesOk)
        {
            WinLevel();
        }
    }

    private void WinLevel()
    {
        if (_isLevelComplete) return;
        _isLevelComplete = true;

        GameManager.Instance?.CompleteCurrentLevel();

        AudioManager.Instance?.PlayLevelComplete();

        VFXManager.Instance?.PlayLevelComplete();

        GameStateManager.Instance?.SetWin();
    }

    private void OnPlayerDied()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        AudioManager.Instance?.PlayGameOver();

        StartCoroutine(ShowGameOverDelayed());
    }

    private IEnumerator ShowGameOverDelayed()
    {
        yield return new WaitForSeconds(1f);
        GameStateManager.Instance?.SetLose();
    }

    private void TimeUp()
    {
        _isGameOver = true;
        AudioManager.Instance?.PlayGameOver();
        GameStateManager.Instance?.SetLose();
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDied.RemoveListener(OnPlayerDied);
        }
    }
}
