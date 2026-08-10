using GameFoundation.Core;
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
            //_uiService?.Register(this);

            _audio = ServiceLocator.Get<IAudioService>();
            _settings = ServiceLocator.Get<ISettingsService>();

            if (!ok) return; // window still registers, just skips wiring missing controls

            backButton.onClick.AddListener(() =>
            {
                _settings?.Save();
                _uiService.Back();
            });

            masterSlider.onValueChanged.AddListener(v => { if (!_suppressCallbacks) _audio?.SetMasterVolume(v); });
            musicSlider.onValueChanged.AddListener(v => { if (!_suppressCallbacks) _audio?.SetMusicVolume(v); });
            sfxSlider.onValueChanged.AddListener(v => { if (!_suppressCallbacks) _audio?.SetSfxVolume(v); });

            SetupLanguageDropdown();
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
        }

        private void SetupLanguageDropdown()
        {
            if (_settings == null || languageDropdown == null) return;

            languageDropdown.ClearOptions();
            var codes = _settings.AvailableLanguageCodes;
            languageDropdown.AddOptions(new System.Collections.Generic.List<string>(codes));

            int current = System.Array.IndexOf(codes, _settings.CurrentLanguageCode);
            if (current >= 0) languageDropdown.SetValueWithoutNotify(current);

            languageDropdown.onValueChanged.AddListener(index =>
            {
                if (index >= 0 && index < codes.Length)
                    _settings.SetLanguage(codes[index]);
            });
        }
    }
}
