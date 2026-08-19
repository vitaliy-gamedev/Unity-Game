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
        private bool _isSubscribed;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            TryBindLocalization();
        }

        private void OnEnable()
        {
            TryBindLocalization();
            Refresh();
        }

        private void TryBindLocalization()
        {
            if (_isSubscribed) return;

            _localization = ServiceLocator.Get<ILocalizationService>();

            if (_localization != null)
            {
                _localization.OnLanguageChanged += Refresh;
                _isSubscribed = true;
                Refresh();
            }
        }

        private void OnDestroy()
        {
            if (_isSubscribed && _localization != null)
                _localization.OnLanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            if (!string.IsNullOrEmpty(key) && _localization != null)
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
