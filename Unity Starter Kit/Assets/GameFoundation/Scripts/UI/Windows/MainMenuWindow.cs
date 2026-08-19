using System.Collections.Generic;
using GameFoundation.Core;
using TMPro;
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
        private ILocalizationService _localization;
        private bool _listenersBound;
        private bool _localizationBound;
        private Vector2Int _lastScreenSize;

        private void OnEnable()
        {
            TryInitialize();
        }

        private void Start()
        {
            TryInitialize();
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            TryInitialize();
        }

        private void TryInitialize()
        {
            if (_listenersBound) return;

            ResolveButtonReferences();

            if (!ValidateButtons()) return;

            if (!ServiceLocator.TryGet<UIService>(out _uiService))
            {
                Debug.LogError("[MainMenuWindow] UIService not found in ServiceLocator!", this);
                return;
            }

            BindLocalization();
            BindListeners();
            RefreshLocalization();
            ArrangeButtonsForScreen();


#if UNITY_WEBGL
            if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif
        }

        private void OnDestroy()
        {
            UnbindListeners();
            UnbindLocalization();
        }

        private void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize == _lastScreenSize) return;

            ArrangeButtonsForScreen();
        }

        private void ResolveButtonReferences()
        {
            if (playButton != null && settingsButton != null && creditsButton != null && quitButton != null)
                return;

            var buttons = new List<Button>(GetComponentsInChildren<Button>(true));
            buttons.Sort((left, right) =>
            {
                float leftY = GetAnchoredY(left);
                float rightY = GetAnchoredY(right);
                return rightY.CompareTo(leftY);
            });

            if (buttons.Count >= 4)
            {
                if (playButton == null) playButton = buttons[0];
                if (settingsButton == null) settingsButton = buttons[1];
                if (creditsButton == null) creditsButton = buttons[2];
                if (quitButton == null) quitButton = buttons[3];
            }
        }

        private static float GetAnchoredY(Button button)
        {
            return button.transform is RectTransform rect ? rect.anchoredPosition.y : button.transform.position.y;
        }

        private void BindListeners()
        {
            if (_listenersBound) return;

            playButton.onClick.AddListener(OnPlayClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);
            quitButton.onClick.AddListener(OnQuitPressed);
            _listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!_listenersBound) return;

            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitPressed);
            _listenersBound = false;
        }

        private void BindLocalization()
        {
            if (_localizationBound) return;

            _localization = ServiceLocator.Get<ILocalizationService>();
            if (_localization == null) return;

            _localization.OnLanguageChanged += RefreshLocalization;
            _localizationBound = true;
        }

        private void UnbindLocalization()
        {
            if (!_localizationBound || _localization == null) return;

            _localization.OnLanguageChanged -= RefreshLocalization;
            _localizationBound = false;
        }

        private void RefreshLocalization()
        {
            if (_localization == null) return;

            SetButtonText(playButton, "main_menu_play");
            SetButtonText(settingsButton, "main_menu_settings");
            SetButtonText(creditsButton, "main_menu_credits");
            SetButtonText(quitButton, "main_menu_quit");
            ArrangeButtonsForScreen();
        }

        private void SetButtonText(Button button, string key)
        {
            if (button == null) return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = _localization.Get(key);
        }

        private void ArrangeButtonsForScreen()
        {
            if (playButton == null || settingsButton == null || creditsButton == null || quitButton == null)
                return;

            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            bool portrait = Screen.height > Screen.width;
            float availableWidth = GetWindowWidth();
            float width = Mathf.Clamp(availableWidth * (portrait ? 0.72f : 0.32f), 320f, 500f);
            float height = portrait ? 68f : 80f;
            float spacing = portrait ? 88f : 116f;
            float startY = portrait ? 150f : 185f;

            PositionButton(playButton, width, height, startY);
            PositionButton(settingsButton, width, height, startY - spacing);
            PositionButton(creditsButton, width, height, startY - spacing * 2f);
            PositionButton(quitButton, width, height, startY - spacing * 3f);
        }

        private float GetWindowWidth()
        {
            if (transform is RectTransform rect && rect.rect.width > 0f)
                return rect.rect.width;

            return Screen.height > Screen.width ? 1080f : 1920f;
        }

        private static void PositionButton(Button button, float width, float height, float y)
        {
            if (button.transform is not RectTransform rect) return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private void OnPlayClicked() => _uiService?.Open<LevelSelectWindow>();
        private void OnSettingsClicked() => _uiService?.Open<SettingsWindow>();
        private void OnCreditsClicked() => _uiService?.Open<CreditsWindow>();

        private void OnQuitPressed()
        {
            if (_uiService == null) return;

            var popup = _uiService.OpenOverlay<ConfirmPopup>();
            if (popup == null) return;

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
