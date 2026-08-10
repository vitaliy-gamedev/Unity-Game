using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Core
{
    [Serializable]
    internal class LocalizationEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    internal class LocalizationTable
    {
        public List<LocalizationEntry> entries = new();
    }

    /// <summary>
    /// Loads Resources/Localization/{languageCode}.json (a flat key→value table) and
    /// reloads it whenever ISettingsService reports a language change. Missing keys
    /// return the key itself wrapped in brackets, so untranslated text is obvious
    /// in-game rather than silently blank.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private readonly ISettingsService _settingsService;
        private readonly Dictionary<string, string> _table = new();

        public event Action OnLanguageChanged;

        public LocalizationService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settingsService.OnLanguageChanged += _ => Reload();
            Reload();
        }

        public string Get(string key)
        {
            if (_table.TryGetValue(key, out var value))
                return value;

            GFLogger.Warn("LocalizationService", $"Missing key '{key}' for language '{_settingsService.CurrentLanguageCode}'.");
            return $"[{key}]";
        }

        private void Reload()
        {
            _table.Clear();

            var asset = Resources.Load<TextAsset>($"Localization/{_settingsService.CurrentLanguageCode}");
            if (asset == null)
            {
                GFLogger.Warn("LocalizationService", $"No localization file found at Resources/Localization/{_settingsService.CurrentLanguageCode}.json");
                OnLanguageChanged?.Invoke();
                return;
            }

            var parsed = JsonUtility.FromJson<LocalizationTable>(asset.text);
            if (parsed?.entries != null)
                foreach (var entry in parsed.entries)
                    _table[entry.key] = entry.value;

            OnLanguageChanged?.Invoke();
        }
    }
}
