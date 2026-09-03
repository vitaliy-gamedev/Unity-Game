using System;
using System.Collections;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.UI
{
    /// <summary>
    /// Reference implementation of ISceneService: fade out via LoadingOverlay,
    /// load the target scene async, report progress, fade back in.
    /// Register an instance of this in your Bootstrap under ISceneService.
    /// </summary>
    public class SceneTransitionService : MonoBehaviour, ISceneService
    {
        [SerializeField] private LoadingOverlay overlay;
        [SerializeField] private float minimumVisibleTime = 0.5f; // avoids a jarring flash on fast loads

        private bool _overlayValid;
        private bool _isLoading;

        private void Awake()
        {
            _overlayValid = GFLogger.RequireField(overlay, nameof(SceneTransitionService), nameof(overlay));
        }

        public void LoadSceneAsync(string sceneName, Action<float> onProgress = null, Action onComplete = null)
        {
            if (_isLoading)
            {
                GFLogger.Warn(nameof(SceneTransitionService), $"Ignored duplicate scene load request for '{sceneName}'.");
                return;
            }

            _isLoading = true;

            if (!_overlayValid)
            {
                // Can't show a fade/progress bar without it, but scene loading itself
                // doesn't depend on the overlay — fall back to a bare, instant load
                // rather than freezing the game entirely over a missing visual.
                GFLogger.Error(nameof(SceneTransitionService), $"Loading '{sceneName}' without a fade overlay because the reference is missing.");
                StartCoroutine(LoadWithoutOverlay(sceneName, onComplete));
                return;
            }

            StartCoroutine(LoadRoutine(sceneName, onProgress, onComplete));
        }

        private IEnumerator LoadWithoutOverlay(string sceneName, Action onComplete)
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
            _isLoading = false;
            onComplete?.Invoke();
        }

        private IEnumerator LoadRoutine(string sceneName, Action<float> onProgress, Action onComplete)
        {
            yield return overlay.FadeIn();

            float startTime = Time.unscaledTime;
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                float progress = op.progress / 0.9f;
                overlay.SetProgress(progress);
                onProgress?.Invoke(progress);
                yield return null;
            }

            overlay.SetProgress(1f);
            onProgress?.Invoke(1f);

            // keep the bar at 100% for a beat so it doesn't look like a glitch on fast SSD loads
            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < minimumVisibleTime)
                yield return new WaitForSecondsRealtime(minimumVisibleTime - elapsed);

            op.allowSceneActivation = true;
            yield return op;

            yield return overlay.FadeOut();
            _isLoading = false;
            onComplete?.Invoke();
        }
    }
}
