using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Pro.Theme
{
    /// <summary>
    /// Register in Bootstrap with an initial ThemeData. Call SetTheme() at runtime
    /// (e.g. from a "dark mode" toggle in Settings) and every ThemeApplier in the
    /// scene repaints itself automatically via OnThemeChanged.
    /// </summary>
    public class ThemeService : MonoBehaviour
    {
        private const string ThemePrefsKey = "gf_pro_active_theme";

        [SerializeField] private ThemeData activeTheme;
        [SerializeField] private string resourcesPath = "Themes";

        private ThemeData[] _availableThemes = Array.Empty<ThemeData>();

        public ThemeData ActiveTheme => activeTheme;
        public IReadOnlyList<ThemeData> AvailableThemes => _availableThemes;
        public int ActiveThemeIndex => Array.IndexOf(_availableThemes, activeTheme);
        public event Action<ThemeData> OnThemeChanged;

        private void Awake()
        {
            RefreshAvailableThemes();

            string savedTheme = PlayerPrefs.GetString(ThemePrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(savedTheme))
            {
                foreach (var theme in _availableThemes)
                {
                    if (theme != null && theme.name == savedTheme)
                    {
                        activeTheme = theme;
                        break;
                    }
                }
            }

            if (activeTheme == null && _availableThemes.Length > 0)
                activeTheme = _availableThemes[0];
        }

        public void SetTheme(ThemeData theme)
        {
            if (theme == null || theme == activeTheme) return;
            activeTheme = theme;
            PlayerPrefs.SetString(ThemePrefsKey, activeTheme.name);
            PlayerPrefs.Save();
            OnThemeChanged?.Invoke(activeTheme);
        }

        public void SetThemeByIndex(int index)
        {
            if (index < 0 || index >= _availableThemes.Length) return;
            SetTheme(_availableThemes[index]);
        }

        public void CycleTheme()
        {
            if (_availableThemes.Length == 0) return;
            int next = (Mathf.Max(ActiveThemeIndex, 0) + 1) % _availableThemes.Length;
            SetTheme(_availableThemes[next]);
        }

        public void RefreshAvailableThemes()
        {
            _availableThemes = Resources.LoadAll<ThemeData>(resourcesPath);
            Array.Sort(_availableThemes, (left, right) => string.CompareOrdinal(left.name, right.name));
        }
    }
}
