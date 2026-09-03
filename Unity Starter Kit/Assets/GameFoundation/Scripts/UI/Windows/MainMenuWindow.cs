using System.Collections;
using System.Collections.Generic;
using GameFoundation.Core;
using GameFoundation.Pro.Animation;
using GameFoundation.Pro.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class MainMenuWindow : BaseWindow
    {
        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button loadGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        private UIService _uiService;
        private ISceneService _sceneService;
        private ISaveService _saveService;
        private ILocalizationService _localization;
        private Coroutine _revealRoutine;
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
            PlayProReveal();
            SelectDefaultButton();
        }

        private void TryInitialize()
        {
            if (_listenersBound) return;

            ResolveButtonReferences();
            CreateExtendedMenuButtons();

            if (!ValidateButtons()) return;

            if (!ServiceLocator.TryGet<UIService>(out _uiService))
            {
                Debug.LogError("[MainMenuWindow] UIService not found in ServiceLocator!", this);
                return;
            }

            _sceneService = ServiceLocator.Get<ISceneService>();
            _saveService = ServiceLocator.Get<ISaveService>();

            BindLocalization();
            BindListeners();
            ApplyFantasyVisualStyle();
            RefreshLocalization();
            RefreshSaveButtons();
            ArrangeButtonsForScreen();
            SelectDefaultButton();


#if UNITY_WEBGL
            if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
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

            continueButton.onClick.AddListener(OnContinueClicked);
            playButton.onClick.AddListener(OnNewGameClicked);
            loadGameButton.onClick.AddListener(OnLoadGameClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);
            quitButton.onClick.AddListener(OnQuitPressed);
            _listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!_listenersBound) return;

            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
            if (playButton != null) playButton.onClick.RemoveListener(OnNewGameClicked);
            if (loadGameButton != null) loadGameButton.onClick.RemoveListener(OnLoadGameClicked);
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

            SetButtonText(continueButton, "main_menu_continue");
            SetButtonText(playButton, "main_menu_new_game");
            SetButtonText(loadGameButton, "main_menu_load_game");
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
            if (continueButton == null || playButton == null || loadGameButton == null ||
                settingsButton == null || creditsButton == null || quitButton == null)
                return;

            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            float availableWidth = GetWindowWidth();
            bool portrait = Screen.height > Screen.width;
            float width = Mathf.Clamp(availableWidth * (portrait ? 0.72f : 0.23f), 330f, 440f);
            float height = portrait ? 62f : 66f;
            float spacing = portrait ? 72f : 76f;
            float x = portrait ? Mathf.Max(30f, (availableWidth - width) * 0.5f) : availableWidth * 0.04f;
            float startY = portrait ? 185f : 125f;

            PositionButton(continueButton, x, width, height, startY);
            PositionButton(playButton, x, width, height, startY - spacing);
            PositionButton(loadGameButton, x, width, height, startY - spacing * 2f);
            PositionButton(settingsButton, x, width, height, startY - spacing * 3f);
            PositionButton(creditsButton, x, width, height, startY - spacing * 4f);
            PositionButton(quitButton, x, width, height, startY - spacing * 5f);
        }

        private float GetWindowWidth()
        {
            if (transform is RectTransform rect && rect.rect.width > 0f)
                return rect.rect.width;

            return Screen.height > Screen.width ? 1080f : 1920f;
        }

        private static void PositionButton(Button button, float x, float width, float height, float y)
        {
            if (button.transform is not RectTransform rect) return;

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private void ApplyFantasyVisualStyle()
        {
            ApplyButtonStyle(continueButton);
            ApplyButtonStyle(playButton);
            ApplyButtonStyle(loadGameButton);
            ApplyButtonStyle(settingsButton);
            ApplyButtonStyle(creditsButton);
            ApplyButtonStyle(quitButton);

            var texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                if (text.gameObject.name != "GameTitle") continue;

                text.gameObject.SetActive(false); // The clean background already contains the DEADBAND logo.

                var outline = text.GetComponent<Outline>();
                if (outline == null)
                    outline = text.gameObject.AddComponent<Outline>();

                outline.effectColor = new Color(0.02f, 0.06f, 0.07f, 0.82f);
                outline.effectDistance = new Vector2(2f, -2f);
                outline.useGraphicAlpha = true;
                break;
            }
        }

        private void ApplyButtonStyle(Button button)
        {
            if (button == null) return;

            var image = button.targetGraphic as Image;
            Outline outline = null;
            if (image != null)
            {
                image.material = null;
                image.raycastTarget = true;

                outline = image.GetComponent<Outline>();
                if (outline == null)
                    outline = image.gameObject.AddComponent<Outline>();

                outline.effectColor = Color.clear;
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;

                DisableThemeApplier(image);
            }

            button.transition = Selectable.Transition.None;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontWeight = FontWeight.SemiBold;
                label.fontSize = Mathf.Max(label.fontSize, 26f);
                label.characterSpacing = Mathf.Max(label.characterSpacing, 2.5f);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                if (label.transform is RectTransform labelRect)
                {
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(30f, 0f);
                    labelRect.offsetMax = new Vector2(-58f, 0f);
                }
                DisableThemeApplier(label);
            }

            var rail = AddAccentRail(button);
            var arrow = AddArrow(button);
            var visual = button.GetComponent<TacticalMenuButtonVisual>();
            if (visual == null)
                visual = button.gameObject.AddComponent<TacticalMenuButtonVisual>();

            visual.Configure(button, image, label, rail, arrow, outline);
        }

        private static Image AddAccentRail(Button button)
        {
            var rail = button.transform.Find("DeadbandAccentRail");
            if (rail == null)
            {
                var railObject = new GameObject("DeadbandAccentRail", typeof(RectTransform), typeof(Image));
                railObject.layer = button.gameObject.layer;
                railObject.transform.SetParent(button.transform, false);
                rail = railObject.transform;
            }

            var rect = rail as RectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(5f, 0f);

            var image = rail.GetComponent<Image>();
            image.raycastTarget = false;
            DisableThemeApplier(image);

            var marker = rail.Find("DeadbandMarker");
            if (marker == null)
            {
                var markerObject = new GameObject("DeadbandMarker", typeof(RectTransform), typeof(Image));
                markerObject.layer = button.gameObject.layer;
                markerObject.transform.SetParent(rail, false);
                marker = markerObject.transform;
            }

            var markerRect = marker as RectTransform;
            markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = Vector2.zero;
            markerRect.sizeDelta = new Vector2(9f, 9f);
            markerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var markerImage = marker.GetComponent<Image>();
            markerImage.raycastTarget = false;
            DisableThemeApplier(markerImage);
            return image;
        }

        private static TMP_Text AddArrow(Button button)
        {
            var arrow = button.transform.Find("DeadbandArrow");
            if (arrow == null)
            {
                var arrowObject = new GameObject("DeadbandArrow", typeof(RectTransform), typeof(TextMeshProUGUI));
                arrowObject.layer = button.gameObject.layer;
                arrowObject.transform.SetParent(button.transform, false);
                arrow = arrowObject.transform;
            }

            var rect = arrow as RectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-18f, 0f);
            rect.sizeDelta = new Vector2(34f, 0f);

            var text = arrow.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = "›";
            text.fontSize = 34f;
            text.fontWeight = FontWeight.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            DisableThemeApplier(text);
            return text;
        }

        private static void DisableThemeApplier(Graphic graphic)
        {
            if (graphic != null && graphic.TryGetComponent<ThemeApplier>(out var themeApplier))
                themeApplier.enabled = false;
        }

        private static void ConfigureTheme(Graphic graphic, ThemeColorRole role, bool headingFont = false)
        {
            var themeApplier = graphic.GetComponent<ThemeApplier>();
            if (themeApplier == null)
                themeApplier = graphic.gameObject.AddComponent<ThemeApplier>();

            themeApplier.Configure(role, headingFont);
        }

        private void PlayProReveal()
        {
            if (!_listenersBound) return;

            if (_revealRoutine != null)
                StopCoroutine(_revealRoutine);

            _revealRoutine = StartCoroutine(RevealElements());
        }

        private IEnumerator RevealElements()
        {
            var elements = new Transform[]
            {
                continueButton.transform,
                playButton.transform,
                loadGameButton.transform,
                settingsButton.transform,
                creditsButton.transform,
                quitButton.transform
            };

            var groups = new CanvasGroup[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                groups[i] = elements[i].GetComponent<CanvasGroup>();
                if (groups[i] == null)
                    groups[i] = elements[i].gameObject.AddComponent<CanvasGroup>();

                groups[i].alpha = 0f;
                elements[i].localScale = Vector3.one * 0.84f;
            }

            yield return null;

            const float duration = 0.28f;
            for (int i = 0; i < elements.Length; i++)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    groups[i].alpha = Easing.Evaluate(EaseType.OutQuad, progress);
                    float scale = Mathf.LerpUnclamped(0.84f, 1f, Easing.Evaluate(EaseType.OutBack, progress));
                    elements[i].localScale = Vector3.one * scale;
                    yield return null;
                }

                groups[i].alpha = 1f;
                elements[i].localScale = Vector3.one;

                float delay = 0f;
                while (delay < 0.035f)
                {
                    delay += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            _revealRoutine = null;
        }

        private void CreateExtendedMenuButtons()
        {
            if (playButton == null) return;

            if (continueButton == null)
            {
                continueButton = Instantiate(playButton, playButton.transform.parent);
                continueButton.name = "ButtonContinue";
            }

            if (loadGameButton == null)
            {
                loadGameButton = Instantiate(playButton, playButton.transform.parent);
                loadGameButton.name = "ButtonLoadGame";
            }
        }

        private void RefreshSaveButtons()
        {
            bool hasSave = _saveService != null && _saveService.HasSave(GameProgressSave.AutosaveKey);
            if (continueButton != null) continueButton.interactable = hasSave;
            if (loadGameButton != null) loadGameButton.interactable = hasSave;
        }

        private void SelectDefaultButton()
        {
            if (EventSystem.current == null) return;

            var defaultButton = continueButton != null && continueButton.interactable
                ? continueButton
                : playButton;

            if (defaultButton != null && defaultButton.isActiveAndEnabled)
                EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
        }

        private void OnNewGameClicked()
        {
            _saveService?.DeleteSave(GameProgressSave.AutosaveKey);
            _saveService?.Save(GameProgressSave.AutosaveKey, GameProgressSave.NewGame());
            RefreshSaveButtons();
            _uiService?.Open<LevelSelectWindow>();
        }

        private void OnContinueClicked() => ContinueFromAutosave();

        private void OnLoadGameClicked()
        {
            if (_uiService == null || _saveService == null || !_saveService.HasSave(GameProgressSave.AutosaveKey))
                return;

            var popup = _uiService.OpenOverlay<ConfirmPopup>();
            popup?.Setup("load_game_title", "load_game_message", ContinueFromAutosave);
        }

        private void ContinueFromAutosave()
        {
            var save = _saveService?.Load<GameProgressSave>(GameProgressSave.AutosaveKey);
            if (save != null && !string.IsNullOrWhiteSpace(save.lastSceneName) &&
                Application.CanStreamedLevelBeLoaded(save.lastSceneName))
            {
                _sceneService?.LoadSceneAsync(save.lastSceneName);
                return;
            }

            _uiService?.Open<LevelSelectWindow>();
        }

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
            if (continueButton == null) { Debug.LogWarning("[MainMenuWindow] Missing continueButton", this); ok = false; }
            if (playButton == null) { Debug.LogWarning("[MainMenuWindow] Missing playButton", this); ok = false; }
            if (loadGameButton == null) { Debug.LogWarning("[MainMenuWindow] Missing loadGameButton", this); ok = false; }
            if (settingsButton == null) { Debug.LogWarning("[MainMenuWindow] Missing settingsButton", this); ok = false; }
            if (creditsButton == null) { Debug.LogWarning("[MainMenuWindow] Missing creditsButton", this); ok = false; }
            if (quitButton == null) { Debug.LogWarning("[MainMenuWindow] Missing quitButton", this); ok = false; }
            return ok;
        }
    }

    internal sealed class TacticalMenuButtonVisual : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private const float AnimationSpeed = 9.5f;

        private Button _button;
        private Image _panel;
        private TMP_Text _label;
        private Image _rail;
        private Image _marker;
        private TMP_Text _arrow;
        private Outline _outline;
        private ThemeService _themeService;
        private ThemeData _appliedTheme;
        private RectTransform _arrowRect;
        private Vector2 _arrowShownPosition;
        private bool _hovered;
        private bool _selected;
        private bool _pressed;
        private float _focus;

        public void Configure(
            Button button,
            Image panel,
            TMP_Text label,
            Image rail,
            TMP_Text arrow,
            Outline outline)
        {
            _button = button;
            _panel = panel;
            _label = label;
            _rail = rail;
            _marker = rail != null ? rail.transform.Find("DeadbandMarker")?.GetComponent<Image>() : null;
            _arrow = arrow;
            _outline = outline;
            _arrowRect = arrow != null ? arrow.rectTransform : null;
            if (_arrowRect != null)
                _arrowShownPosition = _arrowRect.anchoredPosition;

            _themeService = ServiceLocator.Get<ThemeService>();
            ApplyVisuals(true);
        }

        private void OnEnable()
        {
            _themeService = ServiceLocator.Get<ThemeService>();
            if (_themeService != null)
                _themeService.OnThemeChanged += OnThemeChanged;

            // The menu can be inactive while Settings changes the theme, so it may
            // miss OnThemeChanged. Always repaint as soon as the button is shown again.
            ApplyVisuals(true);
        }

        private void OnDisable()
        {
            if (_themeService != null)
                _themeService.OnThemeChanged -= OnThemeChanged;

            _hovered = false;
            _pressed = false;
        }

        private void Update()
        {
            var activeTheme = _themeService != null ? _themeService.ActiveTheme : null;
            if (activeTheme != _appliedTheme)
            {
                ApplyVisuals(true);
                return;
            }

            bool interactable = _button != null && _button.interactable;
            float target = interactable && (_hovered || _selected || _pressed) ? 1f : 0f;
            float previous = _focus;
            _focus = Mathf.MoveTowards(_focus, target, AnimationSpeed * Time.unscaledDeltaTime);

            if (!Mathf.Approximately(previous, _focus) || !interactable)
                ApplyVisuals(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable) return;
            _hovered = true;
            EventSystem.current?.SetSelectedGameObject(gameObject);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
                _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData) => _pressed = false;
        public void OnSelect(BaseEventData eventData) => _selected = true;
        public void OnDeselect(BaseEventData eventData) => _selected = false;

        private void OnThemeChanged(ThemeData _) => ApplyVisuals(true);

        private void ApplyVisuals(bool immediate)
        {
            if (_button == null) return;

            if (immediate)
            {
                bool focused = _button.interactable && (_hovered || _selected || _pressed);
                _focus = focused ? 1f : 0f;
            }

            var theme = _themeService != null ? _themeService.ActiveTheme : null;
            _appliedTheme = theme;
            Color accent = theme != null ? theme.accent : new Color(0.58f, 0.78f, 0.20f, 1f);
            Color panelBase = new Color(0.025f, 0.055f, 0.04f, 1f);
            if (theme != null)
            {
                var themeTint = theme.secondary;
                themeTint.a = 1f;
                panelBase = Color.Lerp(panelBase, themeTint, 0.12f);
            }
            Color textPrimary = theme != null ? theme.textPrimary : new Color(0.95f, 0.97f, 0.91f, 1f);
            Color textSecondary = theme != null ? theme.textSecondary : new Color(0.66f, 0.69f, 0.65f, 1f);
            bool interactable = _button.interactable;

            if (_panel != null)
            {
                var hiddenPanel = panelBase;
                hiddenPanel.a = 0f;
                var shownPanel = panelBase;
                shownPanel.a = _pressed ? 0.90f : 0.80f;
                _panel.color = Color.Lerp(hiddenPanel, shownPanel, _focus);
            }

            if (_label != null)
            {
                var idleText = textSecondary;
                idleText.a = interactable ? 0.84f : 0.34f;
                var activeText = textPrimary;
                activeText.a = 1f;
                _label.color = Color.Lerp(idleText, activeText, _focus);
            }

            var accentVisible = accent;
            accentVisible.a *= _focus;
            if (_rail != null)
            {
                _rail.color = accentVisible;
                if (_rail.rectTransform != null)
                    _rail.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(2f, 6f, _focus), 0f);
            }

            if (_marker != null)
            {
                _marker.color = accentVisible;
                _marker.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, _focus);
            }

            if (_arrow != null)
            {
                _arrow.color = accentVisible;
                if (_arrowRect != null)
                    _arrowRect.anchoredPosition = _arrowShownPosition + Vector2.right * Mathf.Lerp(8f, 0f, _focus);
            }

            if (_outline != null)
            {
                var outlineColor = accent;
                outlineColor.a = 0.64f * _focus;
                _outline.effectColor = outlineColor;
                _outline.effectDistance = new Vector2(1f + _focus, -(1f + _focus));
            }
        }
    }
}
