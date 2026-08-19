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
        private bool _isSubscribed;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _text = GetComponent<TMP_Text>();
            TryBindTheme();
        }

        private void OnEnable()
        {
            TryBindTheme();
        }

        private void TryBindTheme()
        {
            if (_isSubscribed) return;

            if (!ServiceLocator.TryGet(out _themeService))
                return;

            if (_themeService != null)
            {
                _themeService.OnThemeChanged += Apply;
                _isSubscribed = true;
                Apply(_themeService.CurrentPalette);
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _themeService != null)
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

            if (_text != null)
            {
                color.a = _text.color.a;
                _text.color = color;
            }
            else if (_graphic != null)
            {
                color.a = _graphic.color.a;
                _graphic.color = color;
            }
        }
    }
}
