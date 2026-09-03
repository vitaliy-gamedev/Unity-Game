using System.Collections;
using GameFoundation.UI;
using UnityEngine;

namespace GameFoundation.Pro.Animation
{
    /// <summary>
    /// Drop this on the same GameObject as any BaseWindow subclass (MainMenuWindow,
    /// SettingsWindow, ConfirmPopup...) and BaseWindow will automatically use it
    /// instead of the built-in Free fade — no code changes anywhere else.
    ///
    /// Gives per-phase (open/close) control over ease curve, duration, and an
    /// optional scale punch, all editable straight in the Inspector.
    /// </summary>
    public class AdvancedWindowAnimator : MonoBehaviour, IWindowAnimator
    {
        [Header("Open")]
        [SerializeField] private EaseType openEase = EaseType.OutBack;
        [SerializeField] private float openDuration = 0.25f;
        [SerializeField] private float openScaleFrom = 0.85f;

        [Header("Close")]
        [SerializeField] private EaseType closeEase = EaseType.InQuad;
        [SerializeField] private float closeDuration = 0.15f;
        [SerializeField] private float closeScaleTo = 0.9f;

        public IEnumerator PlayOpen(RectTransform rect, CanvasGroup canvasGroup)
        {
            float currentScale = rect != null ? rect.localScale.x : 1f;
            float scaleFrom = canvasGroup.alpha <= 0.001f ? openScaleFrom : currentScale;
            yield return Animate(rect, canvasGroup, canvasGroup.alpha, 1f, scaleFrom, 1f, openDuration, openEase);
        }

        public IEnumerator PlayClose(RectTransform rect, CanvasGroup canvasGroup)
        {
            float currentScale = rect != null ? rect.localScale.x : 1f;
            yield return Animate(rect, canvasGroup, canvasGroup.alpha, 0f, currentScale, closeScaleTo, closeDuration, closeEase);
        }

        private static IEnumerator Animate(
            RectTransform rect, CanvasGroup canvasGroup,
            float alphaFrom, float alphaTo,
            float scaleFrom, float scaleTo,
            float duration, EaseType ease)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float eased = Easing.Evaluate(ease, Mathf.Clamp01(t / duration));

                canvasGroup.alpha = Mathf.Lerp(alphaFrom, alphaTo, eased);
                if (rect != null)
                    rect.localScale = Vector3.one * Mathf.Lerp(scaleFrom, scaleTo, eased);

                yield return null;
            }

            canvasGroup.alpha = alphaTo;
            if (rect != null) rect.localScale = Vector3.one * scaleTo;
        }
    }
}
