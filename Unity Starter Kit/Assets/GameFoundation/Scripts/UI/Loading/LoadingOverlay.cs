using System.Collections;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>
    /// Full-screen fade + progress bar overlay, shown during scene transitions.
    /// Must live somewhere under the root Bootstrap GameObject (see Bootstrap.cs) —
    /// it does NOT call DontDestroyOnLoad itself, because it's not a root object;
    /// the parent Bootstrap protects the whole hierarchy in one call.
    /// </summary>
    public class LoadingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressBar;
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float fillSpeed = 5f; // Швидкість плавного заповнення слайдера

        private bool _isValid;
        private float _targetProgress;

        private void Awake()
        {
            _isValid = GFLogger.RequireField(canvasGroup, nameof(LoadingOverlay), nameof(canvasGroup));
            if (!_isValid) return; // degrade to "no fade visuals" rather than crashing scene loads entirely

            gameObject.SetActive(false);
            canvasGroup.alpha = 0f;

            if (progressBar != null)
                progressBar.value = 0f;
        }

        public IEnumerator FadeIn()
        {
            if (!_isValid) yield break;
            gameObject.SetActive(true);
            if (progressBar != null)
            {
                progressBar.value = 0f;
                _targetProgress = 0f;
            }
            yield return Fade(0f, 1f);
        }

        public IEnumerator FadeOut()
        {
            if (!_isValid) yield break;
            // Перед закриттям гарантовано дотягуємо до 100%
            _targetProgress = 1f;
            if (progressBar != null)
                progressBar.value = 1f;

            yield return Fade(1f, 0f);
            gameObject.SetActive(false);
        }

        public void SetProgress(float value01)
        {
            _targetProgress = Mathf.Clamp01(value01);
        }

        private void Update()
        {
            if (progressBar != null && gameObject.activeSelf)
            {
                // Плавно наближаємо поточне значення слайдера до цільового
                progressBar.value = Mathf.MoveTowards(progressBar.value, _targetProgress, fillSpeed * Time.unscaledDeltaTime);
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            canvasGroup.alpha = from;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}