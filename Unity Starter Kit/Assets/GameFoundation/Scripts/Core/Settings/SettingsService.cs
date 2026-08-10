using System;
using UnityEngine;

namespace GameFoundation.Core
{
    public class SettingsService : ISettingsService
    {
        private const string LanguageKey = "gf_settings_language";
        private static readonly string[] SupportedLanguages = { "uk", "en", "fr" };

        public event Action<string> OnLanguageChanged;

        public string CurrentLanguageCode { get; private set; }
        public string[] AvailableLanguageCodes => SupportedLanguages;

        public SettingsService()
        {
            CurrentLanguageCode = PlayerPrefs.GetString(LanguageKey, DetectSystemLanguageOrDefault());
        }

        public void SetLanguage(string languageCode)
        {
            if (Array.IndexOf(SupportedLanguages, languageCode) < 0)
            {
                GFLogger.Warn("SettingsService", $"Unsupported language code '{languageCode}', ignoring.");
                return;
            }
            if (languageCode == CurrentLanguageCode) return;

            CurrentLanguageCode = languageCode;
            PlayerPrefs.SetString(LanguageKey, languageCode);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke(languageCode);
        }

        public void Save() => PlayerPrefs.Save();

        private static string DetectSystemLanguageOrDefault()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Ukrainian => "uk",
                SystemLanguage.French => "fr",
                _ => "en"
            };
        }
    }
}
