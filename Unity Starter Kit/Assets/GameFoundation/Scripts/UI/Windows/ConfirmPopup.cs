using System;
using GameFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>
    /// One popup, reused for every confirmation dialog in the game
    /// (quit, restart level, delete save, etc). Call Setup() right before OpenOverlay.
    /// </summary>
    public class ConfirmPopup : BaseWindow
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private Action _onConfirm;
        private UIService _uiService;
        private ILocalizationService _localization;
        private string _titleKey;
        private string _messageKey;

        protected override void Awake()
        {
            base.Awake();

            bool ok = true;
            ok &= GFLogger.RequireField(titleText, nameof(ConfirmPopup), nameof(titleText));
            ok &= GFLogger.RequireField(messageText, nameof(ConfirmPopup), nameof(messageText));
            ok &= GFLogger.RequireField(confirmButton, nameof(ConfirmPopup), nameof(confirmButton));
            ok &= GFLogger.RequireField(cancelButton, nameof(ConfirmPopup), nameof(cancelButton));

            _uiService = ServiceLocator.Get<UIService>();
            _uiService?.Register(this);
            _localization = ServiceLocator.Get<ILocalizationService>();
            if (_localization != null)
                _localization.OnLanguageChanged += RefreshLocalization;

            if (!ok) return;

            confirmButton.onClick.AddListener(() =>
            {
                _onConfirm?.Invoke();
                _uiService.Back();
            });
            cancelButton.onClick.AddListener(() => _uiService.Back());
            RefreshButtonLabels();
        }

        private void OnDestroy()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= RefreshLocalization;
        }

        public void Setup(string titleKey, string messageKey, Action onConfirm)
        {
            _titleKey = titleKey;
            _messageKey = messageKey;
            _onConfirm = onConfirm;
            RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            if (!string.IsNullOrEmpty(_titleKey))
                titleText.text = _localization != null ? _localization.Get(_titleKey) : _titleKey;

            if (!string.IsNullOrEmpty(_messageKey))
                messageText.text = _localization != null ? _localization.Get(_messageKey) : _messageKey;

            RefreshButtonLabels();
        }

        private void RefreshButtonLabels()
        {
            SetButtonLabel(confirmButton, "popup_yes");
            SetButtonLabel(cancelButton, "popup_no");
        }

        private void SetButtonLabel(Button button, string key)
        {
            if (button == null) return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = _localization != null ? _localization.Get(key) : key;
        }
    }
}
