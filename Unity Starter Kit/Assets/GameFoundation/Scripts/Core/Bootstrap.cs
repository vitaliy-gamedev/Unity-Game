using GameFoundation.UI;
using GameFoundation.Pro.Achievements;
using GameFoundation.Pro.Addressables;
using GameFoundation.Pro.Analytics;
using GameFoundation.Pro.CloudSave;
using GameFoundation.Pro.Pooling;
using GameFoundation.Pro.Theme;
using UnityEngine;

namespace GameFoundation.Core
{
    /// <summary>
    /// Lives in the Bootstrap scene only. It registers every Core service in
    /// ServiceLocator, keeps the service hierarchy alive, then loads MainMenuScene.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [Header("Scene-based services (drag from Bootstrap scene hierarchy)")]
        [SerializeField] private AudioService audioService;
        [SerializeField] private UIService uiService;
        [SerializeField] private SceneTransitionService sceneTransitionService;
        [SerializeField] private PoolService poolService;
        [SerializeField] private ThemeService themeService;

        [Header("Pro services")]
        [SerializeField] private AchievementDefinition[] achievementDefinitions = new AchievementDefinition[0];

        [SerializeField] private string firstSceneToLoad = "MainMenuScene";

        private bool _referencesValid;

        private void Awake()
        {
            ServiceLocator.Clear();
            DontDestroyOnLoad(gameObject);

            _referencesValid = ValidateReferences();
            if (!_referencesValid)
            {
                GFLogger.Error("Bootstrap", "Startup aborted. Fix the missing references on the Bootstrap GameObject, then press Play again.");
                return;
            }

            RegisterServices();
        }

        private void Start()
        {
            if (!_referencesValid) return;

            ServiceLocator.Get<ISceneService>().LoadSceneAsync(firstSceneToLoad);
        }

        private bool ValidateReferences()
        {
            bool ok = true;
            ok &= GFLogger.RequireField(audioService, nameof(Bootstrap), nameof(audioService));
            ok &= GFLogger.RequireField(uiService, nameof(Bootstrap), nameof(uiService));
            ok &= GFLogger.RequireField(sceneTransitionService, nameof(Bootstrap), nameof(sceneTransitionService));
            ok &= GFLogger.RequireField(poolService, nameof(Bootstrap), nameof(poolService));
            ok &= GFLogger.RequireField(themeService, nameof(Bootstrap), nameof(themeService));
            return ok;
        }

        private void RegisterServices()
        {
            var settingsService = new SettingsService();
            ServiceLocator.Register<ISettingsService>(settingsService);

            var localizationService = new LocalizationService(settingsService);
            ServiceLocator.Register<ILocalizationService>(localizationService);

            var saveService = new SaveService();
            ServiceLocator.Register<ISaveService>(saveService);

            ServiceLocator.Register(uiService);
            ServiceLocator.Register<IAudioService>(audioService);
            ServiceLocator.Register<ISceneService>(sceneTransitionService);
            ServiceLocator.Register(poolService);
            ServiceLocator.Register(themeService);

            var definitions = achievementDefinitions;
            if (definitions == null || definitions.Length == 0)
                definitions = Resources.LoadAll<AchievementDefinition>("Achievements");

            ServiceLocator.Register<IAchievementService>(new LocalAchievementService(definitions));
            ServiceLocator.Register<IAnalyticsService>(new ConsoleAnalyticsService());
            ServiceLocator.Register<ICloudSaveProvider>(new LocalCloudSaveStub());
            ServiceLocator.Register<IAssetProvider>(new ResourcesAssetProvider());

            var lightDarkThemeService = GetComponentInChildren<LightDarkThemeService>(true);
            if (lightDarkThemeService != null)
                ServiceLocator.Register(lightDarkThemeService);

            GFLogger.Log("Bootstrap", "All Core services registered.");
        }
    }
}
