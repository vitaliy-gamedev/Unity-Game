using GameFoundation.Core;
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
        [SerializeField] private string lightLabelText = "Light Mode";
        [SerializeField] private string darkLabelText = "Dark Mode";
        [SerializeField] private string lightLabelKey = "theme_light";
        [SerializeField] private string darkLabelKey = "theme_dark";

        private Toggle _toggle;
        private LightDarkThemeService _themeService;
        private ILocalizationService _localization;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _themeService = ServiceLocator.Get<LightDarkThemeService>();
            _localization = ServiceLocator.Get<ILocalizationService>();
            if (_localization != null)
                _localization.OnLanguageChanged += UpdateLabel;

            if (_themeService == null) return;

            _toggle.SetIsOnWithoutNotify(_themeService.CurrentMode == ThemeMode.Dark);
            UpdateLabel();

            _toggle.onValueChanged.AddListener(isDark =>
            {
                _themeService.SetMode(isDark ? ThemeMode.Dark : ThemeMode.Light);
                UpdateLabel();
            });
        }

        private void OnDestroy()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= UpdateLabel;
        }

        private void UpdateLabel()
        {
            if (label == null || _themeService == null) return;

            bool isDark = _themeService.CurrentMode == ThemeMode.Dark;
            string key = isDark ? darkLabelKey : lightLabelKey;
            string fallback = isDark ? darkLabelText : lightLabelText;
            label.text = _localization != null ? _localization.Get(key) : fallback;
        }
    }
}
