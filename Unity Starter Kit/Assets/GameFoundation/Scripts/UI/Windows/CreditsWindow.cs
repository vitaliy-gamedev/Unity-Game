using GameFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class CreditsWindow : BaseWindow
    {
        [SerializeField] private Button backButton;

        private ILocalizationService _localization;

        protected override void Awake()
        {
            base.Awake();

            bool ok = GFLogger.RequireField(backButton, nameof(CreditsWindow), nameof(backButton));

            var uiService = ServiceLocator.Get<UIService>();
            uiService?.Register(this);
            _localization = ServiceLocator.Get<ILocalizationService>();
            if (_localization != null)
                _localization.OnLanguageChanged += RefreshLocalization;

            if (!ok) return;

            backButton.onClick.AddListener(() => uiService.Back());
            RefreshLocalization();
        }

        private void OnDestroy()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= RefreshLocalization;
        }

        private void RefreshLocalization()
        {
            SetButtonLabel(backButton, "common_back");
        }

        private void SetButtonLabel(Button button, string key)
        {
            if (button == null || _localization == null) return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = _localization.Get(key);
        }
    }
}
