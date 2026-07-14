using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _pausePanel;

    [Header("Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Settings Panel")]
    [SerializeField] private GameObject _settingsPanel;

    private void OnEnable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += HandleStateChange;
        }
    }

    private void OnDisable()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChange;
        }
    }

    private void Start()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);

        if (_resumeButton != null)
            _resumeButton.onClick.AddListener(OnResumeClicked);

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettingsClicked);

        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestartClicked);

        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void HandleStateChange(GameState state)
    {
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(state == GameState.Pause);
        }
    }

    private void OnResumeClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameStateManager.Instance?.SetPlaying();
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (_settingsPanel != null)
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
    }

    private void OnRestartClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameStateManager.Instance?.SetPlaying();
        GameManager.Instance?.RestartCurrentLevel();
    }

    private void OnMainMenuClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameStateManager.Instance?.SetMenu();
        GameManager.Instance?.LoadMainMenu();
    }
}
