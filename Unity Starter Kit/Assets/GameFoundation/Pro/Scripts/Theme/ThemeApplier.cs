using GameFoundation.Core;
using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Pro.Theme
{
    public enum ThemeColorRole { Primary, Secondary, Accent, Background, TextPrimary, TextSecondary }
    public enum ThemeSpriteRole { None, Button, Panel }

    /// <summary>
    /// Drop on any Image / TMP_Text / Button and pick a color role — it repaints
    /// itself on Awake and again whenever ThemeService.SetTheme() fires. No manual
    /// wiring needed per-element beyond choosing the role in the Inspector.
    /// </summary>
    public class ThemeApplier : MonoBehaviour, IThemeGraphicController
    {
        [SerializeField] private ThemeColorRole colorRole = ThemeColorRole.Primary;
        [SerializeField] private bool useHeadingFont;
        [SerializeField] private ThemeSpriteRole spriteRole;

        private Graphic _graphic;
        private TMP_Text _text;
        private ThemeService _themeService;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _text = GetComponent<TMP_Text>();
            _themeService = ServiceLocator.Get<ThemeService>();

            if (_themeService != null)
            {
                _themeService.OnThemeChanged += Apply;
                Apply(_themeService.ActiveTheme);
            }
        }

        private void OnDestroy()
        {
            if (_themeService != null)
                _themeService.OnThemeChanged -= Apply;
        }

        public void Configure(ThemeColorRole role, bool headingFont = false, ThemeSpriteRole themedSprite = ThemeSpriteRole.None)
        {
            colorRole = role;
            useHeadingFont = headingFont;
            spriteRole = themedSprite;

            if (_themeService == null)
                _themeService = ServiceLocator.Get<ThemeService>();

            if (_themeService != null)
                Apply(_themeService.ActiveTheme);
        }

        private void Apply(ThemeData theme)
        {
            if (theme == null) return;

            Color color = colorRole switch
            {
                ThemeColorRole.Primary => theme.primary,
                ThemeColorRole.Secondary => theme.secondary,
                ThemeColorRole.Accent => theme.accent,
                ThemeColorRole.Background => theme.background,
                ThemeColorRole.TextPrimary => theme.textPrimary,
                ThemeColorRole.TextSecondary => theme.textSecondary,
                _ => Color.white
            };

            if (_text != null)
            {
                _text.color = color;
                var font = useHeadingFont ? theme.headingFont : theme.bodyFont;
                if (font != null)
                    _text.font = font;
            }
            else if (_graphic != null)
            {
                _graphic.color = color;
            }

            if (_graphic is Image image)
            {
                var sprite = spriteRole switch
                {
                    ThemeSpriteRole.Button => theme.buttonSprite,
                    ThemeSpriteRole.Panel => theme.panelSprite,
                    _ => null
                };

                if (spriteRole != ThemeSpriteRole.None && sprite != null)
                    image.sprite = sprite;
            }
        }
    }
}
