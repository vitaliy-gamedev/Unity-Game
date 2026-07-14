using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _gameOverPanel;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("Buttons")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _nextLevelButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Messages")]
    [SerializeField] private string _winTitle = "Level Complete!";
    [SerializeField] private string _loseTitle = "Game Over";

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged += HandleStateChange;
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnStateChanged -= HandleStateChange;
    }

    private void Start()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);

        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestartClicked);

        if (_nextLevelButton != null)
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void HandleStateChange(GameState state)
    {
        bool show = state == GameState.Win || state == GameState.Lose;

        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(show);

        if (!show) return;

        if (_titleText != null)
        {
            _titleText.text = state == GameState.Win ? _winTitle : _loseTitle;
        }

        if (_scoreText != null && GameManager.Instance != null)
        {
            _scoreText.text = $"Score: {GameManager.Instance.CurrentScore}";
        }

        if (_nextLevelButton != null)
        {
            bool hasNext = GameManager.Instance != null &&
                GameManager.Instance.GetLevelConfig(GameManager.Instance.CurrentLevelNumber + 1) != null;

            _nextLevelButton.gameObject.SetActive(state == GameState.Win && hasNext);
        }
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameStateManager.Instance?.SetPlaying();
        GameManager.Instance?.RestartCurrentLevel();
    }

    private void OnNextLevelClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameStateManager.Instance?.SetPlaying();
        GameManager.Instance?.LoadNextLevel();
    }

    private void OnMainMenuClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameStateManager.Instance?.SetMenu();
        GameManager.Instance?.LoadMainMenu();
    }
}
