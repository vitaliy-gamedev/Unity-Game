using System;

namespace GameFoundation.Core
{
    /// <summary>
    /// Contracts implemented by the concrete services in Core/Audio, Core/Settings,
    /// Core/Localization, Core/Save. The UI layer only ever talks to these
    /// interfaces, never the concrete classes directly.
    /// </summary>
    public interface IAudioService
    {
        void PlayUIClick();
        void PlayUIHover();
        void SetMasterVolume(float value01);
        void SetMusicVolume(float value01);
        void SetSfxVolume(float value01);
        float MasterVolume { get; }
        float MusicVolume { get; }
        float SfxVolume { get; }
    }

    public interface ISettingsService
    {
        void Save();
        string CurrentLanguageCode { get; }
        void SetLanguage(string languageCode);
        string[] AvailableLanguageCodes { get; }

        /// <summary>Fires after SetLanguage persists the new code — LocalizationService listens to this.</summary>
        event Action<string> OnLanguageChanged;
    }

    public interface ILocalizationService
    {
        string Get(string key);

        /// <summary>Fires after the localization table finishes reloading for a new language — UI text refreshes on this.</summary>
        event Action OnLanguageChanged;
    }

    public interface ISceneService
    {
        /// <param name="onProgress">0..1 loading progress</param>
        void LoadSceneAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null);
    }

    public interface ISaveService
    {
        void Save<T>(string key, T data);
        T Load<T>(string key, T defaultValue = default);
        bool HasSave(string key);
        void DeleteSave(string key);
    }
}
