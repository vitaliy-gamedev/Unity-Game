using GameFoundation.Core;
using GameFoundation.Pro.Achievements;
using GameFoundation.Pro.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>
    /// Put on a UI Toggle inside SettingsWindow. Reflects the current theme on
    /// open and flips LightDarkThemeService when the user taps it.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ThemeToggleButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string lightLabelText = "Fantasy Day";
        [SerializeField] private string darkLabelText = "Moonlit Fantasy";
        [SerializeField] private string lightLabelKey = "theme_fantasy_day";
        [SerializeField] private string darkLabelKey = "theme_fantasy_night";

        private Toggle _toggle;
        private LightDarkThemeService _themeService;
        private ThemeService _proThemeService;
        private ILocalizationService _localization;
        private IAchievementService _achievementService;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _themeService = ServiceLocator.Get<LightDarkThemeService>();
            _proThemeService = ServiceLocator.Get<ThemeService>();
            _localization = ServiceLocator.Get<ILocalizationService>();
            _achievementService = ServiceLocator.Get<IAchievementService>();
            if (_localization != null)
                _localization.OnLanguageChanged += UpdateLabel;

            if (_proThemeService != null)
                _proThemeService.OnThemeChanged += OnProThemeChanged;

            ApplyProVisualStyle();
            _toggle.SetIsOnWithoutNotify(IsNightTheme());
            UpdateLabel();

            _toggle.onValueChanged.AddListener(isDark =>
            {
                _proThemeService?.SetThemeByIndex(isDark ? 1 : 0);
                _themeService?.SetMode(isDark ? ThemeMode.Dark : ThemeMode.Light);
                _achievementService?.IncrementProgress("theme_explorer");
                UpdateLabel();
            });
        }

        private void OnDestroy()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= UpdateLabel;
            if (_proThemeService != null)
                _proThemeService.OnThemeChanged -= OnProThemeChanged;
        }

        private void OnProThemeChanged(ThemeData _)
        {
            _toggle.SetIsOnWithoutNotify(IsNightTheme());
            UpdateLabel();
        }

        private bool IsNightTheme()
            => _proThemeService != null && _proThemeService.AvailableThemes.Count >= 2
                ? _proThemeService.ActiveThemeIndex == 1
                : _themeService != null && _themeService.CurrentMode == ThemeMode.Dark;

        private void ApplyProVisualStyle()
        {
            if (transform is RectTransform toggleRect)
                toggleRect.sizeDelta = new Vector2(360f, 54f);

            if (_toggle.targetGraphic is Image background)
            {
                background.material = null;
                var backgroundRect = background.rectTransform;
                backgroundRect.anchorMin = new Vector2(0f, 0.5f);
                backgroundRect.anchorMax = new Vector2(0f, 0.5f);
                backgroundRect.pivot = new Vector2(0f, 0.5f);
                backgroundRect.anchoredPosition = new Vector2(0f, 0f);
                backgroundRect.sizeDelta = new Vector2(58f, 32f);
                AddTheme(background, ThemeColorRole.Secondary);
            }

            if (_toggle.graphic is Image checkmark)
            {
                checkmark.material = null;
                checkmark.rectTransform.sizeDelta = new Vector2(25f, 25f);
                AddTheme(checkmark, ThemeColorRole.Accent);
            }

            if (label != null)
            {
                label.fontSize = 25f;
                label.fontWeight = FontWeight.SemiBold;
                label.enableAutoSizing = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.alignment = TextAlignmentOptions.MidlineLeft;

                if (label.transform is RectTransform labelRect)
                {
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(76f, 0f);
                    labelRect.offsetMax = Vector2.zero;
                }

                AddTheme(label, ThemeColorRole.TextPrimary);
            }

            var colors = _toggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.84f, 0.9f, 1f);
            colors.selectedColor = Color.white;
            _toggle.colors = colors;
        }

        private static void AddTheme(Graphic graphic, ThemeColorRole role)
        {
            var applier = graphic.GetComponent<ThemeApplier>();
            if (applier == null)
                applier = graphic.gameObject.AddComponent<ThemeApplier>();
            applier.Configure(role);
        }

        private void UpdateLabel()
        {
            if (label == null) return;

            bool isDark = IsNightTheme();
            string key = isDark ? darkLabelKey : lightLabelKey;
            string fallback = isDark ? darkLabelText : lightLabelText;
            label.text = _localization != null ? _localization.Get(key) : fallback;
        }
    }
}
