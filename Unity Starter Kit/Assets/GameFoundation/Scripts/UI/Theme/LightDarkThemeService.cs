using System;
using GameFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        public Color background = new(0.96f, 0.95f, 0.92f);
        public Color panel = new(0.88f, 0.86f, 0.8f);
        public Color textPrimary = new(0.08f, 0.07f, 0.06f);
        public Color textSecondary = new(0.34f, 0.31f, 0.27f);
        public Color accent = new(0.8f, 0.04f, 0.03f);
    }

    public class LightDarkThemeService : MonoBehaviour
    {
        private const string PrefsKey = "gf_theme_mode";

        [SerializeField] private SimpleThemePalette lightPalette = new();
        [SerializeField] private bool autoStyleSceneUi = true;

        [SerializeField]
        private SimpleThemePalette darkPalette = new()
        {
            background = new Color(0.16f, 0.15f, 0.15f),
            panel = new Color(0.23f, 0.21f, 0.21f),
            textPrimary = new Color(0.93f, 0.9f, 0.86f),
            textSecondary = new Color(0.72f, 0.68f, 0.62f),
            accent = new Color(0.72f, 0.08f, 0.07f)
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

            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyGlobalUiStyle(CurrentPalette);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyGlobalUiStyle(CurrentPalette);
        }

        public void SetMode(ThemeMode mode)
        {
            if (mode == CurrentMode) return;

            CurrentMode = mode;
            PlayerPrefs.SetInt(PrefsKey, (int)mode);
            PlayerPrefs.Save();
            OnThemeChanged?.Invoke(CurrentPalette);
            ApplyGlobalUiStyle(CurrentPalette);
        }

        public void Toggle() => SetMode(CurrentMode == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light);

        private void ApplyGlobalUiStyle(SimpleThemePalette palette)
        {
            if (!autoStyleSceneUi) return;

            var graphics = FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var graphic in graphics)
            {
                if (graphic == null || graphic.GetComponent<SimpleThemeApplier>() != null)
                    continue;

                if (graphic is TMP_Text text)
                {
                    SetColor(text, palette.textPrimary);
                    continue;
                }

                if (graphic is Image image)
                {
                    if (ShouldSkipImage(image))
                        continue;

                    SetColor(image, ColorForImage(image, palette));
                }
            }
        }

        private static bool ShouldSkipImage(Image image)
        {
            if (image.sprite == null)
                return false;

            string spriteName = image.sprite.name;
            bool builtInUiSprite = spriteName == "Background" || spriteName == "UISprite" || spriteName == "Knob";
            return !builtInUiSprite && image.GetComponent<Button>() == null && image.GetComponent<Slider>() == null;
        }

        private static Color ColorForImage(Image image, SimpleThemePalette palette)
        {
            string objectName = image.gameObject.name.ToLowerInvariant();

            if (image.GetComponent<Button>() != null || objectName.Contains("fill") || objectName.Contains("checkmark"))
                return palette.accent;

            if (objectName.Contains("background") || objectName.Contains("viewport") || objectName.Contains("panel") ||
                objectName.Contains("window") || objectName.Contains("item") || objectName.Contains("dropdown"))
                return palette.panel;

            return palette.panel;
        }

        private static void SetColor(Graphic graphic, Color color)
        {
            color.a = graphic.color.a;
            graphic.color = color;
        }
    }
}
