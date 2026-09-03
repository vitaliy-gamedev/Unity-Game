using System;
using System.Collections;
using GameFoundation.Core;
using GameFoundation.Pro.Animation;
using GameFoundation.Pro.Theme;
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
        private RectTransform _panelRect;
        private Vector2 _confirmTarget;
        private Vector2 _cancelTarget;
        private Coroutine _choiceAnimation;

        protected override void Awake()
        {
            base.Awake();

            BuildFantasyLayout();

            bool ok = true;
            ok &= GFLogger.RequireField(titleText, nameof(ConfirmPopup), nameof(titleText));
            ok &= GFLogger.RequireField(messageText, nameof(ConfirmPopup), nameof(messageText));
            ok &= GFLogger.RequireField(confirmButton, nameof(ConfirmPopup), nameof(confirmButton));
            ok &= GFLogger.RequireField(cancelButton, nameof(ConfirmPopup), nameof(cancelButton));

            _uiService = ServiceLocator.Get<UIService>();
            _localization = ServiceLocator.Get<ILocalizationService>();
            if (_localization != null)
                _localization.OnLanguageChanged += RefreshLocalization;

            if (!ok) return;

            confirmButton.onClick.AddListener(() =>
            {
                _onConfirm?.Invoke();
                _uiService?.Back();
            });
            cancelButton.onClick.AddListener(() => _uiService?.Back());
            RefreshButtonLabels();
        }

        protected override void OnOpened()
        {
            base.OnOpened();

            if (_choiceAnimation != null)
                StopCoroutine(_choiceAnimation);
            _choiceAnimation = StartCoroutine(AnimateChoices());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
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

        private void BuildFantasyLayout()
        {
            if (confirmButton == null || cancelButton == null || _panelRect != null)
                return;

            var backdrop = new GameObject("TacticalBackdrop", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            backdrop.transform.SetParent(transform, false);
            backdrop.transform.SetAsFirstSibling();
            var backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            var backdropImage = backdrop.GetComponent<Image>();
            backdropImage.material = null;
            backdropImage.color = new Color(0.005f, 0.012f, 0.008f, 0.2f);
            backdrop.GetComponent<CanvasGroup>().alpha = 1f;

            var panel = new GameObject("TacticalConfirmPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panel.transform.SetParent(transform, false);
            _panelRect = panel.GetComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            _panelRect.pivot = new Vector2(0.5f, 0.5f);
            _panelRect.anchoredPosition = new Vector2(0f, 10f);
            _panelRect.sizeDelta = new Vector2(620f, 360f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.material = null;
            AddTheme(panelImage, ThemeColorRole.Secondary);

            var panelOutline = panel.GetComponent<Outline>();
            panelOutline.effectColor = new Color(0.5f, 0.7f, 0.22f, 0.78f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            titleText = CreateText(panel.transform, "ConfirmTitle", 34f, FontWeight.Bold,
                new Vector2(0f, 108f), new Vector2(540f, 64f));
            messageText = CreateText(panel.transform, "ConfirmMessage", 22f, FontWeight.Regular,
                new Vector2(0f, 30f), new Vector2(520f, 82f));
            messageText.textWrappingMode = TextWrappingModes.Normal;

            AddTheme(titleText, ThemeColorRole.Accent, true);
            AddTheme(messageText, ThemeColorRole.TextPrimary);

            confirmButton.transform.SetParent(panel.transform, false);
            cancelButton.transform.SetParent(panel.transform, false);
            _confirmTarget = new Vector2(-115f, -108f);
            _cancelTarget = new Vector2(115f, -108f);
            ConfigureButton(confirmButton, _confirmTarget, ThemeColorRole.Accent, ThemeColorRole.TextPrimary);
            ConfigureButton(cancelButton, _cancelTarget, ThemeColorRole.Secondary, ThemeColorRole.TextPrimary);
        }

        private static TMP_Text CreateText(Transform parent, string name, float fontSize, FontWeight weight,
            Vector2 anchoredPosition, Vector2 size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontWeight = weight;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureButton(Button button, Vector2 position, ThemeColorRole buttonRole, ThemeColorRole textRole)
        {
            if (button.transform is RectTransform rect)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
                rect.sizeDelta = new Vector2(200f, 64f);
            }

            if (button.targetGraphic is Image image)
            {
                image.material = null;
                AddTheme(image, buttonRole);
            }

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontSize = 24f;
                label.fontWeight = FontWeight.Bold;
                label.enableAutoSizing = false;
                AddTheme(label, textRole);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colors.pressedColor = new Color(0.72f, 0.78f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        private IEnumerator AnimateChoices()
        {
            if (_panelRect == null) yield break;

            var confirmRect = confirmButton.transform as RectTransform;
            var cancelRect = cancelButton.transform as RectTransform;
            if (confirmRect == null || cancelRect == null) yield break;

            var confirmGroup = GetOrAddCanvasGroup(confirmButton.gameObject);
            var cancelGroup = GetOrAddCanvasGroup(cancelButton.gameObject);
            confirmGroup.alpha = 0f;
            cancelGroup.alpha = 0f;
            confirmRect.anchoredPosition = new Vector2(-45f, _confirmTarget.y - 18f);
            cancelRect.anchoredPosition = new Vector2(45f, _cancelTarget.y - 18f);
            confirmRect.localScale = Vector3.one * 0.72f;
            cancelRect.localScale = Vector3.one * 0.72f;

            const float duration = 0.36f;
            const float stagger = 0.09f;
            float elapsed = 0f;
            while (elapsed < duration + stagger)
            {
                elapsed += Time.unscaledDeltaTime;
                AnimateChoice(confirmRect, confirmGroup, new Vector2(-45f, _confirmTarget.y - 18f), _confirmTarget,
                    Mathf.Clamp01(elapsed / duration));
                AnimateChoice(cancelRect, cancelGroup, new Vector2(45f, _cancelTarget.y - 18f), _cancelTarget,
                    Mathf.Clamp01((elapsed - stagger) / duration));
                yield return null;
            }

            confirmRect.anchoredPosition = _confirmTarget;
            cancelRect.anchoredPosition = _cancelTarget;
            confirmRect.localScale = Vector3.one;
            cancelRect.localScale = Vector3.one;
            confirmGroup.alpha = 1f;
            cancelGroup.alpha = 1f;
            _choiceAnimation = null;
        }

        private static void AnimateChoice(RectTransform rect, CanvasGroup group, Vector2 from, Vector2 to, float progress)
        {
            group.alpha = Easing.Evaluate(EaseType.OutQuad, progress);
            float eased = Easing.Evaluate(EaseType.OutBack, progress);
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            rect.localScale = Vector3.one * Mathf.LerpUnclamped(0.72f, 1f, eased);
        }

        private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            var group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
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
