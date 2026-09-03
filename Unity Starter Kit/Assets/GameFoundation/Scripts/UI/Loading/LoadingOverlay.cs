using System.Collections;
using GameFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class LoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressBar;
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float fillSpeed = 0.75f;

        private bool _isValid;
        private float _targetProgress;
        private TMP_Text _progressText;
        private RectTransform _progressFillRect;

        private void Awake()
        {
            _isValid = GFLogger.RequireField(canvasGroup, nameof(LoadingOverlay), nameof(canvasGroup));
            if (!_isValid) return;

            ApplyDeadbandVisualStyle();
            gameObject.SetActive(false);
            canvasGroup.alpha = 0f;
            ResetProgress();
        }

        public IEnumerator FadeIn()
        {
            if (!_isValid) yield break;

            gameObject.SetActive(true);
            ResetProgress();
            yield return Fade(0f, 1f);
        }

        public IEnumerator FadeOut()
        {
            if (!_isValid) yield break;

            _targetProgress = 1f;
            yield return WaitForDisplayedProgress(0.99f);

            yield return Fade(1f, 0f);
            gameObject.SetActive(false);
        }

        public void SetProgress(float value01)
        {
            _targetProgress = Mathf.Clamp01(value01);
        }

        private void Update()
        {
            if (progressBar == null || !gameObject.activeSelf) return;

            float displayedProgress = Mathf.MoveTowards(
                progressBar.value,
                _targetProgress,
                fillSpeed * Time.unscaledDeltaTime);
            progressBar.SetValueWithoutNotify(displayedProgress);
            SetVisualProgress(displayedProgress);

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(displayedProgress * 100f):00}%";
        }

        private void ResetProgress()
        {
            _targetProgress = 0f;

            if (progressBar != null)
                progressBar.SetValueWithoutNotify(0f);

            SetVisualProgress(0f);

            if (_progressText != null)
                _progressText.text = "00%";
        }

        private void ApplyDeadbandVisualStyle()
        {
            if (progressBar == null) return;

            progressBar.interactable = false;
            progressBar.gameObject.SetActive(false); // Never show the stock Slider or its handle.
            CreateLoadingHud();
        }

        private void CreateLoadingHud()
        {
            var hudObject = new GameObject("DeadbandLoadingHud", typeof(RectTransform));
            hudObject.layer = gameObject.layer;
            hudObject.transform.SetParent(transform, false);
            var hudRect = hudObject.GetComponent<RectTransform>();
            hudRect.anchorMin = hudRect.anchorMax = new Vector2(0.5f, 0f);
            hudRect.pivot = new Vector2(0.5f, 0f);
            hudRect.anchoredPosition = new Vector2(0f, 88f);
            hudRect.sizeDelta = new Vector2(720f, 76f);

            var labelObject = new GameObject("LoadingLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(hudObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = new Vector2(300f, 40f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = "LOADING";
            label.fontSize = 25f;
            label.fontWeight = FontWeight.SemiBold;
            label.characterSpacing = 6f;
            label.color = new Color(0.9f, 0.92f, 0.87f, 1f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            var percentObject = new GameObject("LoadingPercent", typeof(RectTransform), typeof(TextMeshProUGUI));
            percentObject.layer = gameObject.layer;
            percentObject.transform.SetParent(hudObject.transform, false);
            var percentRect = percentObject.GetComponent<RectTransform>();
            percentRect.anchorMin = percentRect.anchorMax = new Vector2(1f, 1f);
            percentRect.pivot = new Vector2(1f, 1f);
            percentRect.anchoredPosition = Vector2.zero;
            percentRect.sizeDelta = new Vector2(140f, 40f);

            _progressText = percentObject.GetComponent<TextMeshProUGUI>();
            _progressText.font = TMP_Settings.defaultFontAsset;
            _progressText.text = "00%";
            _progressText.fontSize = 25f;
            _progressText.fontWeight = FontWeight.SemiBold;
            _progressText.color = new Color(0.56f, 0.78f, 0.18f, 1f);
            _progressText.alignment = TextAlignmentOptions.MidlineRight;
            _progressText.raycastTarget = false;

            var trackObject = new GameObject("ProgressTrack", typeof(RectTransform), typeof(Image));
            trackObject.layer = gameObject.layer;
            trackObject.transform.SetParent(hudObject.transform, false);
            var trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 8f);
            trackRect.sizeDelta = new Vector2(0f, 5f);
            var trackImage = trackObject.GetComponent<Image>();
            trackImage.material = null;
            trackImage.color = new Color(0.015f, 0.03f, 0.02f, 0.86f);
            trackImage.raycastTarget = false;

            var fillObject = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fillObject.layer = gameObject.layer;
            fillObject.transform.SetParent(trackObject.transform, false);
            _progressFillRect = fillObject.GetComponent<RectTransform>();
            _progressFillRect.anchorMin = Vector2.zero;
            _progressFillRect.anchorMax = new Vector2(0f, 1f);
            _progressFillRect.pivot = new Vector2(0f, 0.5f);
            _progressFillRect.offsetMin = Vector2.zero;
            _progressFillRect.offsetMax = Vector2.zero;
            var fillImage = fillObject.GetComponent<Image>();
            fillImage.material = null;
            fillImage.color = new Color(0.56f, 0.78f, 0.18f, 1f);
            fillImage.raycastTarget = false;
        }

        private void SetVisualProgress(float value01)
        {
            if (_progressFillRect == null) return;

            var anchorMax = _progressFillRect.anchorMax;
            anchorMax.x = Mathf.Clamp01(value01);
            _progressFillRect.anchorMax = anchorMax;
        }

        private IEnumerator WaitForDisplayedProgress(float value01)
        {
            if (progressBar == null) yield break;

            while (progressBar.value < value01)
                yield return null;

            progressBar.SetValueWithoutNotify(1f);
            SetVisualProgress(1f);
        }

        private IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            canvasGroup.alpha = from;

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDuration));
                yield return null;
            }

            canvasGroup.alpha = to;
        }
    }
}
