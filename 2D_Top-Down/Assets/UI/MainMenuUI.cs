using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _quitButton;

    [Header("Panels")]
    [SerializeField] private GameObject _settingsPanel;

    private void Start()
    {
        GameStateManager.Instance?.SetMenu();
        AudioManager.Instance?.PlayMainMenuMusic();

        if (_playButton != null)
            _playButton.onClick.AddListener(OnPlayClicked);

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OnSettingsClicked);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnPlayClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.LoadLevelSelect();
    }

    private void OnSettingsClicked()
    {
        AudioManager.Instance?.PlayButtonClick();

        if (_settingsPanel != null)
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
    }

    private void OnQuitClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
