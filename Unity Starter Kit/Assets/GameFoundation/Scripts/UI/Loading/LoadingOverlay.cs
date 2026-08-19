using System.Collections;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class LoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressBar;
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float fillSpeed = 1.6f;

        private bool _isValid;
        private float _targetProgress;

        private void Awake()
        {
            _isValid = GFLogger.RequireField(canvasGroup, nameof(LoadingOverlay), nameof(canvasGroup));
            if (!_isValid) return;

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

            progressBar.value = Mathf.MoveTowards(
                progressBar.value,
                _targetProgress,
                fillSpeed * Time.unscaledDeltaTime);
        }

        private void ResetProgress()
        {
            _targetProgress = 0f;

            if (progressBar != null)
                progressBar.value = 0f;
        }

        private IEnumerator WaitForDisplayedProgress(float value01)
        {
            if (progressBar == null) yield break;

            while (progressBar.value < value01)
                yield return null;

            progressBar.value = 1f;
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
