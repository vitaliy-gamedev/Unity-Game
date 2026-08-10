using System;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.UI
{
    public enum ThemeMode { Light, Dark }

    /// <summary>
    /// Two fixed palettes, one toggle — no ScriptableObjects, no custom color roles.
    /// If you need more than two themes or per-project custom palettes, that's what
    /// the Pro ThemeData/ThemeService system is for; this one is intentionally simple.
    /// </summary>
    [Serializable]
    public class SimpleThemePalette
    {
        public Color background = Color.white;
        public Color panel = new(0.92f, 0.92f, 0.92f);
        public Color textPrimary = Color.black;
        public Color textSecondary = new(0.35f, 0.35f, 0.35f);
        public Color accent = new(0.2f, 0.5f, 1f);
    }

    public class LightDarkThemeService : MonoBehaviour
    {
        private const string PrefsKey = "gf_theme_mode";

        [SerializeField] private SimpleThemePalette lightPalette = new();
        [SerializeField]
        private SimpleThemePalette darkPalette = new()
        {
            background = new Color(0.08f, 0.08f, 0.08f),
            panel = new Color(0.15f, 0.15f, 0.15f),
            textPrimary = Color.white,
            textSecondary = new Color(0.7f, 0.7f, 0.7f),
            accent = new Color(0.3f, 0.6f, 1f)
        };

        public ThemeMode CurrentMode { get; private set; }
        public SimpleThemePalette CurrentPalette => CurrentMode == ThemeMode.Light ? lightPalette : darkPalette;

        public event Action<SimpleThemePalette> OnThemeChanged;

        private void Awake()
        {
            // Реєструємо сервіс у ServiceLocator, щоб інші компоненти (наприклад, тугл) могли його знайти
            ServiceLocator.Register(this);

            CurrentMode = PlayerPrefs.HasKey(PrefsKey)
                ? (ThemeMode)PlayerPrefs.GetInt(PrefsKey)
                : ThemeMode.Light; // no reliable cross-platform system dark-mode query without a native plugin
        }

        public void SetMode(ThemeMode mode)
        {
            if (mode == CurrentMode) return;

            CurrentMode = mode;
            PlayerPrefs.SetInt(PrefsKey, (int)mode);
            PlayerPrefs.Save();
            OnThemeChanged?.Invoke(CurrentPalette);
        }

        public void Toggle() => SetMode(CurrentMode == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light);
    }
}