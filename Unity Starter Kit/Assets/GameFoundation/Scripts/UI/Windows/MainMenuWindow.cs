using GameFoundation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class MainMenuWindow : BaseWindow
    {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        private UIService _uiService;

        // Ініціалізацію сервісів та підписки краще робити в Start, 
        // коли всі ServiceLocator.Register вже точно відпрацювали в Awake!
        private void Start()
        {
            if (!ValidateButtons()) return;

            if (ServiceLocator.TryGet<UIService>(out _uiService))
            {
                playButton.onClick.AddListener(OnPlayClicked);
                settingsButton.onClick.AddListener(OnSettingsClicked);
                creditsButton.onClick.AddListener(OnCreditsClicked);
                quitButton.onClick.AddListener(OnQuitPressed);
            }
            else
            {
                Debug.LogError("[MainMenuWindow] UIService not found in ServiceLocator!", this);
            }

#if UNITY_WEBGL
            if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif
        }

        private void OnDestroy()
        {
            // Завжди знімаємо підписки, щоб не було витоку пам'яті при знищенні об'єкта
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitPressed);
        }

        private void OnPlayClicked() => _uiService?.Open<LevelSelectWindow>();
        private void OnSettingsClicked() => _uiService?.Open<SettingsWindow>();
        private void OnCreditsClicked() => _uiService?.Open<CreditsWindow>();

        private void OnQuitPressed()
        {
            if (_uiService == null) return;

            var popup = _uiService.OpenOverlay<ConfirmPopup>();
            popup.Setup(
                titleKey: "popup_quit_title",
                messageKey: "popup_quit_message",
                onConfirm: () =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
        }

        private bool ValidateButtons()
        {
            bool ok = true;
            if (playButton == null) { Debug.LogWarning("[MainMenuWindow] Missing playButton", this); ok = false; }
            if (settingsButton == null) { Debug.LogWarning("[MainMenuWindow] Missing settingsButton", this); ok = false; }
            if (creditsButton == null) { Debug.LogWarning("[MainMenuWindow] Missing creditsButton", this); ok = false; }
            if (quitButton == null) { Debug.LogWarning("[MainMenuWindow] Missing quitButton", this); ok = false; }
            return ok;
        }
    }
}