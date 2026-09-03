using System;
using GameFoundation.Core;
using GameFoundation.Pro.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TMP_Text titleText;

        private LevelData _data;
        private Action<LevelData> _onPicked;
        private bool _isValid;

        private void Awake()
        {
            _isValid = ResolveReferences(logMissing: true);
            ApplyTacticalStyle();
        }

        private void OnEnable() => ApplyTacticalStyle();

        public void Setup(LevelData data, bool unlocked, Action<LevelData> onPicked)
        {
            if (!_isValid)
                _isValid = ResolveReferences(logMissing: true);

            if (!_isValid) return;

            _data = data;
            _onPicked = onPicked;

            if (thumbnailImage != null)
            {
                thumbnailImage.enabled = data.thumbnail != null;
                thumbnailImage.sprite = data.thumbnail;
            }

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(data.displaySceneName) ? $"Level {data.levelId}" : data.displaySceneName;
                titleText.transform.SetAsLastSibling();
            }

            button.interactable = unlocked;
            if (lockIcon != null)
            {
                lockIcon.SetActive(!unlocked);
                if (!unlocked)
                    lockIcon.transform.SetAsLastSibling();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onPicked?.Invoke(_data));
            ApplyTacticalStyle();
        }

        public void ApplyTacticalStyle()
        {
            if (!_isValid)
                _isValid = ResolveReferences(logMissing: false);
            if (!_isValid) return;

            var themeService = ServiceLocator.Get<ThemeService>();
            var theme = themeService != null ? themeService.ActiveTheme : null;

            if (button.targetGraphic is Image panel)
            {
                panel.material = null;
                var panelColor = theme != null ? theme.secondary : new Color(0.05f, 0.11f, 0.065f, 1f);
                panelColor.a = button.interactable ? 0.72f : 0.46f;
                panel.color = panelColor;

                var outline = panel.GetComponent<Outline>();
                if (outline == null)
                    outline = panel.gameObject.AddComponent<Outline>();
                var outlineColor = theme != null ? theme.accent : new Color(0.55f, 0.78f, 0.18f, 1f);
                outlineColor.a = 0.72f;
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.72f, 0.82f, 0.62f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.58f);
            colors.fadeDuration = 0.12f;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;

            if (thumbnailImage != null)
            {
                thumbnailImage.material = null;
                thumbnailImage.color = new Color(1f, 1f, 1f, 0.34f);
                thumbnailImage.raycastTarget = false;
                var thumbnailRect = thumbnailImage.rectTransform;
                thumbnailRect.anchorMin = Vector2.zero;
                thumbnailRect.anchorMax = Vector2.one;
                thumbnailRect.offsetMin = new Vector2(5f, 5f);
                thumbnailRect.offsetMax = new Vector2(-5f, -5f);
            }

            if (titleText != null)
            {
                titleText.fontSize = 27f;
                titleText.fontWeight = FontWeight.SemiBold;
                titleText.alignment = TextAlignmentOptions.MidlineLeft;
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                titleText.color = theme != null ? theme.textPrimary : new Color(0.9f, 0.94f, 0.86f, 1f);
                titleText.raycastTarget = false;
                if (titleText.transform is RectTransform titleRect)
                {
                    titleRect.anchorMin = Vector2.zero;
                    titleRect.anchorMax = Vector2.one;
                    titleRect.offsetMin = new Vector2(30f, 0f);
                    titleRect.offsetMax = new Vector2(-70f, 0f);
                }
                titleText.transform.SetAsLastSibling();
            }

            AddAccentRail(theme);
        }

        private void AddAccentRail(ThemeData theme)
        {
            var rail = transform.Find("DeadbandAccentRail");
            if (rail == null)
            {
                var railObject = new GameObject("DeadbandAccentRail", typeof(RectTransform), typeof(Image));
                railObject.layer = gameObject.layer;
                railObject.transform.SetParent(transform, false);
                rail = railObject.transform;
            }

            var rect = rail as RectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(5f, 0f);

            var image = rail.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = theme != null ? theme.accent : new Color(0.55f, 0.78f, 0.18f, 1f);
            rail.SetAsLastSibling();
        }

        private bool ResolveReferences(bool logMissing)
        {
            if (button == null)
                button = GetComponent<Button>();

            if (thumbnailImage == null)
            {
                foreach (var image in GetComponentsInChildren<Image>(true))
                {
                    if (image.gameObject != gameObject)
                    {
                        thumbnailImage = image;
                        break;
                    }
                }
            }

            if (lockIcon == null)
            {
                var lockTransform = transform.Find("lockIcon");
                if (lockTransform != null)
                    lockIcon = lockTransform.gameObject;
            }

            if (titleText == null)
                titleText = GetComponentInChildren<TMP_Text>(true);

            if (!logMissing)
                return button != null && thumbnailImage != null && titleText != null;

            bool ok = true;
            ok &= GFLogger.RequireField(button, nameof(LevelButton), nameof(button));
            ok &= GFLogger.RequireField(thumbnailImage, nameof(LevelButton), nameof(thumbnailImage));
            ok &= GFLogger.RequireField(titleText, nameof(LevelButton), nameof(titleText));
            return ok;
        }
    }
}
