using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach to the MainMenu Canvas root. Wire up buttons in Inspector.
/// Handles: Play → DroneSelect, Options panel toggle, Quit.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;

    [Header("Main Buttons")]
    public Button playButton;
    public Button optionsButton;
    public Button quitButton;

    [Header("Options Controls")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Button langUAButton;
    public Button langENButton;
    public Button optionsBackButton;

    [Header("Options Highlight")]
    public Image langUAHighlight;
    public Image langENHighlight;
    public Color selectedLangColor   = new Color(0.2f, 0.8f, 0.4f, 1f);
    public Color unselectedLangColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    void Start()
    {
        // Ensure singletons exist
        EnsureSingletons();

        // Main buttons
        playButton   .onClick.AddListener(OnPlay);
        optionsButton.onClick.AddListener(OnOptions);
        quitButton   .onClick.AddListener(OnQuit);

        // Options
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolume);
        musicVolumeSlider .onValueChanged.AddListener(OnMusicVolume);
        sfxVolumeSlider   .onValueChanged.AddListener(OnSFXVolume);
        langUAButton      .onClick.AddListener(() => OnSetLanguage(GameManager.Language.Ukrainian));
        langENButton      .onClick.AddListener(() => OnSetLanguage(GameManager.Language.English));
        optionsBackButton .onClick.AddListener(OnOptionsBack);

        // Load current values
        if (GameManager.Instance != null)
        {
            masterVolumeSlider.value = GameManager.Instance.masterVolume;
            musicVolumeSlider .value = GameManager.Instance.musicVolume;
            sfxVolumeSlider   .value = GameManager.Instance.sfxVolume;
            UpdateLangHighlight(GameManager.Instance.currentLanguage);
        }

        ShowMain();
    }

    // ── Panel Switching ──────────────────────────────────────────
    void ShowMain()    { mainPanel.SetActive(true);  optionsPanel.SetActive(false); }
    void ShowOptions() { mainPanel.SetActive(false); optionsPanel.SetActive(true);  }

    // ── Button Handlers ──────────────────────────────────────────
    void OnPlay()    => GameManager.Instance?.LoadDroneSelect();
    void OnOptions() => ShowOptions();
    void OnQuit()    => GameManager.Instance?.QuitGame();
    void OnOptionsBack() => ShowMain();

    void OnMasterVolume(float v)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.masterVolume = v;
        GameManager.Instance.ApplyVolume();
    }

    void OnMusicVolume(float v)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.musicVolume = v;
        GameManager.Instance.ApplyVolume();
    }

    void OnSFXVolume(float v)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.sfxVolume = v;
        GameManager.Instance.ApplyVolume();
    }

    void OnSetLanguage(GameManager.Language lang)
    {
        GameManager.Instance?.SetLanguage(lang);
        UpdateLangHighlight(lang);
    }

    void UpdateLangHighlight(GameManager.Language lang)
    {
        if (langUAHighlight != null)
            langUAHighlight.color = lang == GameManager.Language.Ukrainian ? selectedLangColor : unselectedLangColor;
        if (langENHighlight != null)
            langENHighlight.color = lang == GameManager.Language.English   ? selectedLangColor : unselectedLangColor;
    }

    // ── Ensure singletons for first scene ────────────────────────
    void EnsureSingletons()
    {
        if (GameManager.Instance == null)
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }
        if (LocalizationManager.Instance == null)
        {
            var lm = new GameObject("LocalizationManager");
            lm.AddComponent<LocalizationManager>();
        }
    }
}
