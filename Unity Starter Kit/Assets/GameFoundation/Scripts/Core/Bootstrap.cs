using GameFoundation.UI;
using UnityEngine;

namespace GameFoundation.Core
{
    /// <summary>
    /// Lives in the Bootstrap scene only — this is the very first scene that runs
    /// when the game starts (set it as index 0 in Build Settings). It registers
    /// every Core service in ServiceLocator, then loads MainMenuScene.
    ///
    /// See README_UA.md section 3 for the exact GameObject hierarchy this script
    /// expects to find via the Inspector fields below.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [Header("Scene-based services (drag from Bootstrap scene hierarchy)")]
        [SerializeField] private AudioService audioService;
        [SerializeField] private UIService uiService;
        [SerializeField] private SceneTransitionService sceneTransitionService;

        [SerializeField] private string firstSceneToLoad = "MainMenuScene";

        private bool _referencesValid;

        private void Awake()
        {
            // CRITICAL: DontDestroyOnLoad only protects THIS GameObject and its
            // CHILDREN — not scene siblings. AudioService, UIService, and
            // SceneTransitionService must be nested as children of this exact
            // GameObject in the hierarchy (see README_UA.md section 3), otherwise
            // they get destroyed the instant MainMenuScene loads and replaces
            // BootstrapScene.
            DontDestroyOnLoad(gameObject);

            // Fail fast and loud: Debug.LogError does NOT stop script execution on
            // its own, so without the early "return" below, a single missing
            // Inspector reference here would silently register a null service and
            // then cause five confusing NullReferenceExceptions somewhere else in
            // the game, far away from the actual cause. Catching it here means
            // there's exactly one error message, and it tells you exactly what to
            // drag into which field.
            _referencesValid = ValidateReferences();
            if (!_referencesValid)
            {
                GFLogger.Error("Bootstrap", "Startup aborted — fix the missing references above (see Bootstrap GameObject in the Inspector), then press Play again.");
                return;
            }

            RegisterServices();
        }

        private void Start()
        {
            if (!_referencesValid) return; // already logged in Awake — don't cascade into more errors

            ServiceLocator.Get<ISceneService>().LoadSceneAsync(firstSceneToLoad);
        }

        private bool ValidateReferences()
        {
            bool ok = true;
            ok &= GFLogger.RequireField(audioService, nameof(Bootstrap), nameof(audioService));
            ok &= GFLogger.RequireField(uiService, nameof(Bootstrap), nameof(uiService));
            ok &= GFLogger.RequireField(sceneTransitionService, nameof(Bootstrap), nameof(sceneTransitionService));
            return ok;
        }

        private void RegisterServices()
        {
            // Order matters here: LocalizationService's constructor immediately reads
            // ISettingsService.CurrentLanguageCode, so Settings must be registered first.
            var settingsService = new SettingsService();
            ServiceLocator.Register<ISettingsService>(settingsService);

            var localizationService = new LocalizationService(settingsService);
            ServiceLocator.Register<ILocalizationService>(localizationService);

            var saveService = new SaveService();
            ServiceLocator.Register<ISaveService>(saveService);

            // MonoBehaviour services: assigned via Inspector because they need to live
            // on real GameObjects (AudioSources, Canvas, coroutines). Already
            // null-checked in ValidateReferences() above, so these are guaranteed
            // non-null here.
            ServiceLocator.Register<IAudioService>(audioService);
            ServiceLocator.Register<ISceneService>(sceneTransitionService);

            // UIService registers itself in its own Awake() — see UIService.cs.
            // Nothing to do here as long as it exists somewhere in this scene
            // (Bootstrap scene is the simplest place, per README_UA.md section 3).
            _ = uiService; // kept as an Inspector reference for convenience/visibility and the null-check above

            GFLogger.Log("Bootstrap", "All Core services registered.");
        }
    }
}
