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

            if (!ok) return;

            confirmButton.onClick.AddListener(() =>
            {
                _onConfirm?.Invoke();
                _uiService.Back();
            });
            cancelButton.onClick.AddListener(() => _uiService.Back());
        }

        public void Setup(string titleKey, string messageKey, Action onConfirm)
        {
            _onConfirm = onConfirm;
            titleText.text = _localization != null ? _localization.Get(titleKey) : titleKey;
            messageText.text = _localization != null ? _localization.Get(messageKey) : messageKey;
        }
    }
}
