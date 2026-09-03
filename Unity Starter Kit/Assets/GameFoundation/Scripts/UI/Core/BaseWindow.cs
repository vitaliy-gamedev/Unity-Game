using System;
using System.Collections;
using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>
    /// Base class for every menu screen (MainMenu, Settings, LevelSelect, popups...).
    /// Handles interaction locking and delegates the actual open/close animation to
    /// an IWindowAnimator if one is present on the same GameObject. Otherwise it
    /// falls back to a simple built-in fade and scale animation.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseWindow : MonoBehaviour, IWindow
    {
        [Header("Default Animation (used when no IWindowAnimator is present)")]
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private float scaleFrom = 0.96f;
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        protected CanvasGroup CanvasGroup;
        private RectTransform _rect;
        private Coroutine _animRoutine;
        private IWindowAnimator _customAnimator;

        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            _rect = transform as RectTransform;
            _customAnimator = GetComponent<IWindowAnimator>();

            if (ServiceLocator.TryGet<UIService>(out var uiService))
                uiService.Register(this);

            gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            if (ServiceLocator.TryGet<UIService>(out var uiService))
                uiService.Unregister(this);
        }

        public virtual void Open()
        {
            gameObject.SetActive(true);
            IsOpen = true;

            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(RunOpen());

            OnOpened();
        }

        public virtual void Close()
        {
            IsOpen = false;

            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(RunClose());
        }

        /// <summary>Called right after the window becomes active, before the fade-in finishes.</summary>
        protected virtual void OnOpened() { }

        /// <summary>Called after the fade-out finishes and the window is deactivated.</summary>
        protected virtual void OnClosed() { }

        private IEnumerator RunOpen()
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = true;

            if (_customAnimator != null)
                yield return _customAnimator.PlayOpen(_rect, CanvasGroup);
            else
                yield return DefaultAnimateRoutine(opening: true);

            CanvasGroup.interactable = true;
        }

        private IEnumerator RunClose()
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;

            if (_customAnimator != null)
                yield return _customAnimator.PlayClose(_rect, CanvasGroup);
            else
                yield return DefaultAnimateRoutine(opening: false);

            gameObject.SetActive(false);
            OnClosed();
        }

        private IEnumerator DefaultAnimateRoutine(bool opening)
        {
            float from = opening ? 0f : 1f;
            float to = opening ? 1f : 0f;
            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float linear = Mathf.Clamp01(t / fadeDuration);
                float eased = easeCurve.Evaluate(linear);

                CanvasGroup.alpha = Mathf.Lerp(from, to, eased);

                if (_rect != null)
                {
                    float scale = Mathf.Lerp(opening ? scaleFrom : 1f, opening ? 1f : scaleFrom, eased);
                    _rect.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            CanvasGroup.alpha = to;
            if (_rect != null) _rect.localScale = Vector3.one;
        }
    }
}
