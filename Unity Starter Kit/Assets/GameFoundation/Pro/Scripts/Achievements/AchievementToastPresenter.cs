using System.Collections;
using GameFoundation.Core;
using GameFoundation.Pro.Animation;
using GameFoundation.Pro.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Pro.Achievements
{
    /// <summary>Small self-building overlay that makes Pro achievement events visible in any menu scene.</summary>
    public sealed class AchievementToastPresenter : MonoBehaviour
    {
        private const float AnimationDuration = 0.38f;

        private IAchievementService _achievementService;
        private ILocalizationService _localization;
        private RectTransform _toastRect;
        private CanvasGroup _canvasGroup;
        private TMP_Text _header;
        private TMP_Text _title;
        private TMP_Text _description;
        private Coroutine _animation;
        private bool _initialized;

        public void Initialize()
        {
            if (_initialized) return;

            _achievementService = ServiceLocator.Get<IAchievementService>();
            _localization = ServiceLocator.Get<ILocalizationService>();
            if (_achievementService == null) return;

            BuildToast();
            _achievementService.OnUnlocked += Show;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_achievementService != null)
                _achievementService.OnUnlocked -= Show;
        }

        private void BuildToast()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var toast = new GameObject("ProAchievementToast", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            toast.transform.SetParent(canvas.transform, false);
            toast.transform.SetAsLastSibling();

            _toastRect = toast.GetComponent<RectTransform>();
            _toastRect.anchorMin = Vector2.one;
            _toastRect.anchorMax = Vector2.one;
            _toastRect.pivot = Vector2.one;
            _toastRect.sizeDelta = new Vector2(460f, 116f);
            _toastRect.anchoredPosition = new Vector2(-28f, -28f);

            _canvasGroup = toast.GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            var panel = toast.GetComponent<Image>();
            panel.material = null;
            panel.raycastTarget = false;
            toast.AddComponent<ThemeApplier>().Configure(ThemeColorRole.Secondary);

            _header = CreateLabel(toast.transform, "Header", 17f, FontWeight.Bold, new Vector2(22f, -38f), new Vector2(-22f, -13f));
            _title = CreateLabel(toast.transform, "Title", 27f, FontWeight.SemiBold, new Vector2(22f, -73f), new Vector2(-22f, -38f));
            _description = CreateLabel(toast.transform, "Description", 16f, FontWeight.Regular, new Vector2(22f, -106f), new Vector2(-22f, -75f));

            _header.gameObject.AddComponent<ThemeApplier>().Configure(ThemeColorRole.Accent);
            _title.gameObject.AddComponent<ThemeApplier>().Configure(ThemeColorRole.TextPrimary, true);
            _description.gameObject.AddComponent<ThemeApplier>().Configure(ThemeColorRole.TextSecondary);

            var outline = toast.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(2f, -2f);
        }

        private static TMP_Text CreateLabel(Transform parent, string name, float size, FontWeight weight, Vector2 offsetMin, Vector2 offsetMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);

            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontWeight = weight;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private void Show(string achievementId)
        {
            if (_toastRect == null || _canvasGroup == null) return;

            _header.text = Localize("achievement_unlocked");
            _title.text = Localize($"achievement_{achievementId}_title");
            _description.text = Localize($"achievement_{achievementId}_description");
            _toastRect.SetAsLastSibling();

            if (_animation != null)
                StopCoroutine(_animation);
            _animation = StartCoroutine(AnimateToast());
        }

        private string Localize(string key) => _localization != null ? _localization.Get(key) : key;

        private IEnumerator AnimateToast()
        {
            Vector2 visiblePosition = new(-28f, -28f);
            Vector2 hiddenPosition = new(500f, -28f);

            yield return Animate(hiddenPosition, visiblePosition, 0f, 1f, EaseType.OutBack);

            float hold = 0f;
            while (hold < 2.6f)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Animate(visiblePosition, hiddenPosition, 1f, 0f, EaseType.InCubic);
            _animation = null;
        }

        private IEnumerator Animate(Vector2 from, Vector2 to, float alphaFrom, float alphaTo, EaseType ease)
        {
            float elapsed = 0f;
            while (elapsed < AnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / AnimationDuration);
                float eased = Easing.Evaluate(ease, progress);
                _toastRect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                _canvasGroup.alpha = Mathf.Lerp(alphaFrom, alphaTo, Easing.Evaluate(EaseType.OutQuad, progress));
                yield return null;
            }

            _toastRect.anchoredPosition = to;
            _canvasGroup.alpha = alphaTo;
        }
    }
}
