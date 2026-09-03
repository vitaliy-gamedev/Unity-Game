using GameFoundation.Core;
using GameFoundation.Pro.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class CreditsWindow : BaseWindow
    {
        [SerializeField] private Button backButton;

        private ILocalizationService _localization;
        private TMP_Text _developerText;
        private TMP_Text _frameworkText;

        protected override void Awake()
        {
            base.Awake();

            bool ok = GFLogger.RequireField(backButton, nameof(CreditsWindow), nameof(backButton));

            var uiService = ServiceLocator.Get<UIService>();
            _localization = ServiceLocator.Get<ILocalizationService>();
            if (_localization != null)
                _localization.OnLanguageChanged += RefreshLocalization;

            if (!ok) return;

            BuildCreditsVisual();
            BackButtonStyle.Apply(backButton);
            backButton.onClick.AddListener(() => uiService?.Back());
            RefreshLocalization();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_localization != null)
                _localization.OnLanguageChanged -= RefreshLocalization;
        }

        private void RefreshLocalization()
        {
            SetButtonLabel(backButton, "common_back");
            SetLabel(_developerText, "credits_developed_by");
            SetLabel(_frameworkText, "credits_framework");
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            RefreshLocalization();
        }

        private void SetButtonLabel(Button button, string key)
        {
            if (button == null || _localization == null) return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = _localization.Get(key);
        }

        private void SetLabel(TMP_Text label, string key)
        {
            if (label != null && _localization != null)
                label.text = _localization.Get(key);
        }

        private void BuildCreditsVisual()
        {
            foreach (Transform child in transform)
            {
                if (child == backButton.transform)
                    continue;
                child.gameObject.SetActive(false);
            }

            var panelObject = new GameObject("DeadbandCreditsPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelObject.layer = gameObject.layer;
            panelObject.transform.SetParent(transform, false);
            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(700f, 500f);

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.material = null;
            panelImage.raycastTarget = false;
            AddTheme(panelImage, ThemeColorRole.Secondary);

            var outline = panelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.58f, 0.20f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            CreateText(panelObject.transform, "DeadbandTitle", "DEADBAND", 170f, 620f, 80f, 54f,
                FontWeight.Bold, TextAlignmentOptions.Center, ThemeColorRole.Accent, 10f);
            CreateText(panelObject.transform, "DeadbandSubtitle", "TACTICAL SQUAD EXTRACTION", 105f, 620f, 46f, 22f,
                FontWeight.SemiBold, TextAlignmentOptions.Center, ThemeColorRole.TextPrimary, 6f);

            var dividerObject = new GameObject("AccentDivider", typeof(RectTransform), typeof(Image));
            dividerObject.layer = gameObject.layer;
            dividerObject.transform.SetParent(panelObject.transform, false);
            var dividerRect = dividerObject.GetComponent<RectTransform>();
            dividerRect.anchorMin = dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
            dividerRect.sizeDelta = new Vector2(520f, 2f);
            dividerRect.anchoredPosition = new Vector2(0f, 66f);
            var divider = dividerObject.GetComponent<Image>();
            divider.raycastTarget = false;
            AddTheme(divider, ThemeColorRole.Accent);

            _developerText = CreateText(panelObject.transform, "Developer", string.Empty, -5f, 620f, 100f, 26f,
                FontWeight.SemiBold, TextAlignmentOptions.Center, ThemeColorRole.TextPrimary, 3f);
            _frameworkText = CreateText(panelObject.transform, "Framework", string.Empty, -118f, 620f, 70f, 20f,
                FontWeight.Medium, TextAlignmentOptions.Center, ThemeColorRole.TextSecondary, 4f);
            CreateText(panelObject.transform, "Copyright", "© 2026 DEADBAND", -205f, 620f, 48f, 18f,
                FontWeight.Medium, TextAlignmentOptions.Center, ThemeColorRole.TextSecondary, 3f);
        }

        private TMP_Text CreateText(Transform parent, string objectName, string value, float y, float width, float height,
            float fontSize, FontWeight weight, TextAlignmentOptions alignment, ThemeColorRole role, float characterSpacing)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.fontWeight = weight;
            text.alignment = alignment;
            text.characterSpacing = characterSpacing;
            text.raycastTarget = false;
            AddTheme(text, role, role == ThemeColorRole.Accent);
            return text;
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
