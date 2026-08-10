using GameFoundation.Core;
using TMPro;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>
    /// Drop on any TMP_Text, set the localization key in the Inspector, done.
    /// Refreshes automatically whenever LocalizationService reloads for a new language.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;

        private TMP_Text _text;
        private ILocalizationService _localization;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            _localization = ServiceLocator.Get<ILocalizationService>();

            if (_localization != null)
            {
                _localization.OnLanguageChanged += Refresh;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            if (!string.IsNullOrEmpty(key))
                _text.text = _localization.Get(key);
        }

        /// <summary>Change the key at runtime (e.g. for dynamically generated labels) and repaint immediately.</summary>
        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }
    }
}
