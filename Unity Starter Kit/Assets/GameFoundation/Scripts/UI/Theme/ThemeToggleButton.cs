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

        private Toggle _toggle;
        private LightDarkThemeService _themeService;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _themeService = ServiceLocator.Get<LightDarkThemeService>();
            if (_themeService == null) return;

            _toggle.SetIsOnWithoutNotify(_themeService.CurrentMode == ThemeMode.Dark);
            UpdateLabel();

            _toggle.onValueChanged.AddListener(isDark =>
            {
                _themeService.SetMode(isDark ? ThemeMode.Dark : ThemeMode.Light);
                UpdateLabel();
            });
        }

        private void UpdateLabel()
        {
            if (label == null) return;
            label.text = _themeService.CurrentMode == ThemeMode.Dark ? darkLabelText : lightLabelText;
        }
    }
}
