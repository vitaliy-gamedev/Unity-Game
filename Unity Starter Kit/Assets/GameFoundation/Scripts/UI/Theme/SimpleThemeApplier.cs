using GameFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public enum SimpleThemeRole { Background, Panel, TextPrimary, TextSecondary, Accent }

    /// <summary>
    /// Drop on any Image / TMP_Text that should follow the Light/Dark toggle.
    /// Repaints on Awake and again every time LightDarkThemeService.SetMode()/Toggle() fires.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleThemeApplier : MonoBehaviour
    {
        [SerializeField] private SimpleThemeRole role;

        private Graphic _graphic;
        private TMP_Text _text;
        private LightDarkThemeService _themeService;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _text = GetComponent<TMP_Text>();
            _themeService = ServiceLocator.Get<LightDarkThemeService>();

            if (_themeService != null)
            {
                _themeService.OnThemeChanged += Apply;
                Apply(_themeService.CurrentPalette);
            }
        }

        private void OnDestroy()
        {
            if (_themeService != null)
                _themeService.OnThemeChanged -= Apply;
        }

        private void Apply(SimpleThemePalette palette)
        {
            Color color = role switch
            {
                SimpleThemeRole.Background => palette.background,
                SimpleThemeRole.Panel => palette.panel,
                SimpleThemeRole.TextPrimary => palette.textPrimary,
                SimpleThemeRole.TextSecondary => palette.textSecondary,
                SimpleThemeRole.Accent => palette.accent,
                _ => Color.white
            };

            if (_text != null) _text.color = color;
            else if (_graphic != null) _graphic.color = color;
        }
    }
}
