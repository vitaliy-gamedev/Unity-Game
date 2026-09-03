using System.Collections.Generic;
using GameFoundation.Core;
using GameFoundation.Pro.Achievements;
using GameFoundation.Pro.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class SettingsWindow : BaseWindow
    {
        [Header("Audio")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        [Header("Language")]
        [SerializeField] private TMP_Dropdown languageDropdown;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        private UIService _uiService;
        private IAudioService _audio;
        private ISettingsService _settings;
        private ILocalizationService _localization;
        private ThemeService _themeService;
        private IAchievementService _achievementService;
        private TMP_Dropdown _themeDropdown;
        private TMP_Text _titleText;
        private TMP_Text _masterLabel;
        private TMP_Text _musicLabel;
        private TMP_Text _sfxLabel;
        private TMP_Text _languageLabel;
        private TMP_Text _themeLabel;
        private bool _suppressCallbacks;

        protected override void Awake()
        {
            base.Awake();

            bool ok = true;
            ok &= GFLogger.RequireField(masterSlider, nameof(SettingsWindow), nameof(masterSlider));
            ok &= GFLogger.RequireField(musicSlider, nameof(SettingsWindow), nameof(musicSlider));
            ok &= GFLogger.RequireField(sfxSlider, nameof(SettingsWindow), nameof(sfxSlider));
            ok &= GFLogger.RequireField(backButton, nameof(SettingsWindow), nameof(backButton));
            // languageDropdown is checked separately in SetupLanguageDropdown() — it already
            // degrades gracefully (language switching just won't be available in the UI).

            _uiService = ServiceLocator.Get<UIService>();
            _audio = ServiceLocator.Get<IAudioService>();
            _settings = ServiceLocator.Get<ISettingsService>();
            _localization = ServiceLocator.Get<ILocalizationService>();
            _themeService = ServiceLocator.Get<ThemeService>();
            _achievementService = ServiceLocator.Get<IAchievementService>();

            if (_localization != null)
                _localization.OnLanguageChanged += RefreshLanguageDropdownLabels;
            if (_themeService != null)
                _themeService.OnThemeChanged += OnThemeChanged;

            if (!ok) return; // window still registers, just skips wiring missing controls

            backButton.onClick.AddListener(() =>
            {
                _settings?.Save();
                _uiService?.Back();
            });

            masterSlider.onValueChanged.AddListener(v => { if (!_suppressCallbacks) _audio?.SetMasterVolume(v); });
            musicSlider.onValueChanged.AddListener(v => { if (!_suppressCallbacks) _audio?.SetMusicVolume(v); });
            sfxSlider.onValueChanged.AddListener(v => { if (!_suppressCallbacks) _audio?.SetSfxVolume(v); });

            SetupLanguageDropdown();
            BuildTacticalLayout();
            SetupThemeDropdown();
            ApplyProVisualStyle();
            RefreshStaticLabels();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_localization != null)
                _localization.OnLanguageChanged -= RefreshLanguageDropdownLabels;
            if (_themeService != null)
                _themeService.OnThemeChanged -= OnThemeChanged;
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            RefreshFromCurrentState();
        }

        private void RefreshFromCurrentState()
        {
            if (_audio == null) return;

            _suppressCallbacks = true;
            masterSlider.value = _audio.MasterVolume;
            musicSlider.value = _audio.MusicVolume;
            sfxSlider.value = _audio.SfxVolume;
            _suppressCallbacks = false;
            RefreshStaticLabels();
        }

        private void SetupLanguageDropdown()
        {
            if (_settings == null || languageDropdown == null) return;

            languageDropdown.ClearOptions();
            var codes = _settings.AvailableLanguageCodes;
            languageDropdown.AddOptions(BuildLanguageOptions(codes));

            int current = System.Array.IndexOf(codes, _settings.CurrentLanguageCode);
            if (current >= 0) languageDropdown.SetValueWithoutNotify(current);

            languageDropdown.onValueChanged.AddListener(index =>
            {
                if (index >= 0 && index < codes.Length)
                    _settings.SetLanguage(codes[index]);
            });
        }

        private System.Collections.Generic.List<string> BuildLanguageOptions(string[] codes)
        {
            var options = new System.Collections.Generic.List<string>(codes.Length);
            foreach (var code in codes)
                options.Add(_localization != null ? _localization.Get($"language_{code}") : code);

            return options;
        }

        private void RefreshLanguageDropdownLabels()
        {
            if (_settings == null || languageDropdown == null) return;

            _suppressCallbacks = true;
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(BuildLanguageOptions(_settings.AvailableLanguageCodes));

            int current = System.Array.IndexOf(_settings.AvailableLanguageCodes, _settings.CurrentLanguageCode);
            if (current >= 0)
                languageDropdown.SetValueWithoutNotify(current);

            languageDropdown.RefreshShownValue();
            _suppressCallbacks = false;
            RefreshThemeDropdownLabels();
            RefreshStaticLabels();
        }

        private void RefreshStaticLabels()
        {
            SetButtonLabel(backButton, "common_back");
            SetLabel(_titleText, "settings_title");
            SetLabel(_masterLabel, "settings_master_volume");
            SetLabel(_musicLabel, "settings_music_volume");
            SetLabel(_sfxLabel, "settings_sfx_volume");
            SetLabel(_languageLabel, "settings_language");
            SetLabel(_themeLabel, "settings_theme");
        }

        private void SetLabel(TMP_Text label, string key)
        {
            if (label != null && _localization != null)
                label.text = _localization.Get(key);
        }

        private void SetButtonLabel(Button button, string key)
        {
            if (button == null || _localization == null) return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = _localization.Get(key);
        }

        private void ApplyProVisualStyle()
        {
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                bool isHeading = text.gameObject.name.ToLowerInvariant().Contains("title");
                AddTheme(text, isHeading ? ThemeColorRole.Accent : ThemeColorRole.TextPrimary, isHeading);
            }

            if (backButton != null)
            {
                if (backButton.targetGraphic is Image backImage)
                {
                    backImage.material = null;
                    AddTheme(backImage, ThemeColorRole.Primary);
                }

                var backLabel = backButton.GetComponentInChildren<TMP_Text>(true);
                if (backLabel != null)
                    AddTheme(backLabel, ThemeColorRole.TextPrimary);
            }

            StyleSlider(masterSlider);
            StyleSlider(musicSlider);
            StyleSlider(sfxSlider);

            if (languageDropdown != null)
                StyleDropdown(languageDropdown);

            if (_themeDropdown != null)
                StyleDropdown(_themeDropdown);

            BackButtonStyle.Apply(backButton);
        }

        private void BuildTacticalLayout()
        {
            var oldToggle = GetComponentInChildren<ThemeToggleButton>(true);
            if (oldToggle != null)
                oldToggle.gameObject.SetActive(false);

            var panelObject = new GameObject("DeadbandSettingsPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelObject.layer = gameObject.layer;
            panelObject.transform.SetParent(transform, false);
            panelObject.transform.SetAsFirstSibling();

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 720f);

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.raycastTarget = false;
            panelImage.material = null;
            AddTheme(panelImage, ThemeColorRole.Secondary);

            var panelOutline = panelObject.GetComponent<Outline>();
            panelOutline.effectColor = new Color(0.42f, 0.58f, 0.20f, 0.72f);
            panelOutline.effectDistance = new Vector2(2f, -2f);
            panelOutline.useGraphicAlpha = true;

            _titleText = FindDirectTextChild();
            if (_titleText == null)
                _titleText = CreateLabel("SettingsTitle", 320f, 620f, 64f, 42f, FontWeight.Bold, TextAlignmentOptions.Center, ThemeColorRole.Accent);
            else
            {
                StyleTextRect(_titleText, 320f, 620f, 64f);
                _titleText.fontSize = 42f;
                _titleText.fontWeight = FontWeight.Bold;
                _titleText.alignment = TextAlignmentOptions.Center;
                AddTheme(_titleText, ThemeColorRole.Accent, true);
            }

            _masterLabel = CreateLabel("MasterVolumeLabel", 245f, 520f, 38f, 23f, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft, ThemeColorRole.TextPrimary);
            _musicLabel = CreateLabel("MusicVolumeLabel", 135f, 520f, 38f, 23f, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft, ThemeColorRole.TextPrimary);
            _sfxLabel = CreateLabel("SfxVolumeLabel", 25f, 520f, 38f, 23f, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft, ThemeColorRole.TextPrimary);
            _languageLabel = CreateLabel("LanguageLabel", -85f, 520f, 38f, 23f, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft, ThemeColorRole.TextPrimary);
            _themeLabel = CreateLabel("ThemeLabel", -215f, 520f, 38f, 23f, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft, ThemeColorRole.TextPrimary);

            PlaceControl(masterSlider.transform as RectTransform, 200f, 520f, 30f);
            PlaceControl(musicSlider.transform as RectTransform, 90f, 520f, 30f);
            PlaceControl(sfxSlider.transform as RectTransform, -20f, 520f, 30f);
            PlaceControl(languageDropdown.transform as RectTransform, -140f, 520f, 56f);
        }

        private TMP_Text FindDirectTextChild()
        {
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<TMP_Text>(out var text))
                    return text;
            }

            return null;
        }

        private TMP_Text CreateLabel(string objectName, float y, float width, float height, float fontSize,
            FontWeight weight, TextAlignmentOptions alignment, ThemeColorRole role)
        {
            var labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(transform, false);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;
            label.fontWeight = weight;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            StyleTextRect(label, y, width, height);
            AddTheme(label, role);
            return label;
        }

        private static void StyleTextRect(TMP_Text text, float y, float width, float height)
        {
            if (text.transform is not RectTransform rect) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceControl(RectTransform rect, float y, float width, float height)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void SetupThemeDropdown()
        {
            if (languageDropdown == null || _themeService == null) return;

            _themeService.RefreshAvailableThemes();
            _themeDropdown = Instantiate(languageDropdown, transform);
            _themeDropdown.name = "ThemeDropdown";
            _themeDropdown.onValueChanged.RemoveAllListeners();
            PlaceControl(_themeDropdown.transform as RectTransform, -270f, 520f, 56f);
            RefreshThemeDropdownLabels();

            int activeIndex = Mathf.Max(0, _themeService.ActiveThemeIndex);
            _themeDropdown.SetValueWithoutNotify(activeIndex);
            _themeDropdown.RefreshShownValue();
            _themeDropdown.onValueChanged.AddListener(SetThemeByIndex);
        }

        private void SetThemeByIndex(int index)
        {
            if (_themeService == null || index < 0 || index >= _themeService.AvailableThemes.Count)
                return;

            _themeService.SetThemeByIndex(index);
            _achievementService?.IncrementProgress("theme_explorer");
        }

        private void OnThemeChanged(ThemeData _)
        {
            if (_themeDropdown == null || _themeService == null) return;
            _themeDropdown.SetValueWithoutNotify(Mathf.Max(0, _themeService.ActiveThemeIndex));
            _themeDropdown.RefreshShownValue();
        }

        private void RefreshThemeDropdownLabels()
        {
            if (_themeDropdown == null || _themeService == null) return;

            int selected = Mathf.Max(0, _themeService.ActiveThemeIndex);
            var options = new List<string>(_themeService.AvailableThemes.Count);
            foreach (var theme in _themeService.AvailableThemes)
                options.Add(GetThemeDisplayName(theme));

            _themeDropdown.ClearOptions();
            _themeDropdown.AddOptions(options);
            if (options.Count > 0)
                _themeDropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, options.Count - 1));
            _themeDropdown.RefreshShownValue();
        }

        private string GetThemeDisplayName(ThemeData theme)
        {
            string key = theme != null ? theme.name switch
            {
                "01_FantasyDay" => "theme_fantasy_day",
                "02_MoonlitFantasy" => "theme_fantasy_night",
                "03_SignalGreen" => "theme_signal_green",
                "04_AmberAlert" => "theme_amber_alert",
                _ => string.Empty
            } : string.Empty;

            return !string.IsNullOrEmpty(key) && _localization != null ? _localization.Get(key) : theme?.name ?? string.Empty;
        }

        private static void StyleDropdown(TMP_Dropdown dropdown)
        {
            if (dropdown.targetGraphic is Image dropdownImage)
            {
                dropdownImage.material = null;
                AddTheme(dropdownImage, ThemeColorRole.Secondary);
                var outline = dropdownImage.GetComponent<Outline>();
                if (outline == null)
                    outline = dropdownImage.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.42f, 0.58f, 0.20f, 0.72f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            if (dropdown.captionText != null)
            {
                dropdown.captionText.fontSize = 22f;
                dropdown.captionText.fontWeight = FontWeight.SemiBold;
                AddTheme(dropdown.captionText, ThemeColorRole.TextPrimary);
            }

            StyleDropdownTemplate(dropdown);
        }

        private static void StyleDropdownTemplate(TMP_Dropdown dropdown)
        {
            if (dropdown.template == null) return;

            if (dropdown.template.TryGetComponent<Image>(out var templateImage))
            {
                templateImage.material = null;
                AddTheme(templateImage, ThemeColorRole.Secondary);
            }

            foreach (var image in dropdown.template.GetComponentsInChildren<Image>(true))
            {
                image.material = null;
                string objectName = image.gameObject.name.ToLowerInvariant();
                AddTheme(image, objectName.Contains("checkmark") ? ThemeColorRole.Accent : ThemeColorRole.Secondary);
            }

            foreach (var optionText in dropdown.template.GetComponentsInChildren<TMP_Text>(true))
            {
                optionText.fontSize = 21f;
                optionText.fontWeight = FontWeight.Medium;
                optionText.enableAutoSizing = false;
                AddTheme(optionText, ThemeColorRole.TextPrimary);
            }

            if (dropdown.itemText != null)
            {
                dropdown.itemText.fontSize = 21f;
                dropdown.itemText.fontWeight = FontWeight.Medium;
                AddTheme(dropdown.itemText, ThemeColorRole.TextPrimary);
            }
        }

        private static void StyleSlider(Slider slider)
        {
            if (slider == null) return;

            if (slider.fillRect != null && slider.fillRect.TryGetComponent<Image>(out var fill))
                AddTheme(fill, ThemeColorRole.Accent);

            if (slider.targetGraphic != null)
                AddTheme(slider.targetGraphic, ThemeColorRole.TextPrimary);

            var background = slider.transform.Find("Background");
            if (background != null && background.TryGetComponent<Image>(out var backgroundImage))
                AddTheme(backgroundImage, ThemeColorRole.Secondary);
        }

        private static void AddTheme(Graphic graphic, ThemeColorRole role, bool headingFont = false)
        {
            var applier = graphic.GetComponent<ThemeApplier>();
            if (applier == null)
                applier = graphic.gameObject.AddComponent<ThemeApplier>();
            applier.Configure(role, headingFont);
        }
    }
}
