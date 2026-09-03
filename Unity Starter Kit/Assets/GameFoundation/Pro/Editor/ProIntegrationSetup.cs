#if UNITY_EDITOR
using System.IO;
using GameFoundation.Core;
using GameFoundation.Pro.Animation;
using GameFoundation.Pro.Pooling;
using GameFoundation.Pro.Theme;
using GameFoundation.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFoundation.Pro.Editor
{
    /// <summary>
    /// Applies the scene wiring required by the Pro add-on. The automatic pass is
    /// idempotent, and the menu command can be used to repair the setup later.
    /// </summary>
    [InitializeOnLoad]
    public static class ProIntegrationSetup
    {
        private const string BootstrapScenePath = "Assets/Scenes/BootstrapScene.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
        private const string ThemeFolderPath = "Assets/GameFoundation/Pro/Theme";
        private const string DefaultThemePath = ThemeFolderPath + "/DefaultTheme.asset";
        private const string SessionKey = "GameFoundation.Pro.IntegrationAttempted.v2";

        static ProIntegrationSetup()
        {
            EditorApplication.delayCall += TryAutomaticSetup;
        }

        [MenuItem("Tools/Game Foundation/Apply Pro Integration")]
        public static void ApplyFromMenu()
        {
            ApplyIntegration(showCompletionDialog: true);
        }

        private static void TryAutomaticSetup()
        {
            if (SessionState.GetBool(SessionKey, false) || !NeedsSetup()) return;

            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryAutomaticSetup;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            ApplyIntegration(showCompletionDialog: false);
        }

        private static bool NeedsSetup()
        {
            if (!File.Exists(BootstrapScenePath) || !File.Exists(MainMenuScenePath)) return false;
            if (AssetDatabase.LoadAssetAtPath<ThemeData>(DefaultThemePath) == null) return true;

            string bootstrapYaml = File.ReadAllText(BootstrapScenePath);
            string menuYaml = File.ReadAllText(MainMenuScenePath);
            return !bootstrapYaml.Contains("GameFoundation.Pro.Pooling.PoolService")
                   || !bootstrapYaml.Contains("GameFoundation.Pro.Theme.ThemeService")
                   || !menuYaml.Contains("GameFoundation.Pro.Animation.AdvancedWindowAnimator");
        }

        private static void ApplyIntegration(bool showCompletionDialog)
        {
            if (!File.Exists(BootstrapScenePath) || !File.Exists(MainMenuScenePath))
            {
                Debug.LogError("[ProIntegrationSetup] BootstrapScene or MainMenuScene was not found.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[ProIntegrationSetup] Setup cancelled so unsaved scene changes remain untouched.");
                return;
            }

            string originalScenePath = SceneManager.GetActiveScene().path;
            ThemeData defaultTheme = GetOrCreateDefaultTheme();

            ConfigureBootstrap(defaultTheme);
            ConfigureMainMenu();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(originalScenePath) && File.Exists(originalScenePath))
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);

            Debug.Log("[ProIntegrationSetup] Pro services, default theme, and window animators are configured.");

            if (showCompletionDialog)
                EditorUtility.DisplayDialog("Game Foundation Pro", "Pro integration is configured successfully.", "OK");
        }

        private static ThemeData GetOrCreateDefaultTheme()
        {
            EnsureFolder(ThemeFolderPath);

            var theme = AssetDatabase.LoadAssetAtPath<ThemeData>(DefaultThemePath);
            if (theme != null) return theme;

            theme = ScriptableObject.CreateInstance<ThemeData>();
            theme.name = "DefaultTheme";
            theme.primary = Color.white;
            theme.secondary = new Color(0.23f, 0.21f, 0.21f, 1f);
            theme.accent = new Color(0.72f, 0.08f, 0.07f, 1f);
            theme.background = new Color(0.16f, 0.15f, 0.15f, 1f);
            theme.textPrimary = new Color(0.93f, 0.90f, 0.86f, 1f);
            theme.textSecondary = new Color(0.72f, 0.68f, 0.62f, 1f);
            theme.headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            theme.bodyFont = theme.headingFont;

            AssetDatabase.CreateAsset(theme, DefaultThemePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(DefaultThemePath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<ThemeData>(DefaultThemePath);
        }

        private static void ConfigureBootstrap(ThemeData defaultTheme)
        {
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var bootstrap = Object.FindFirstObjectByType<Bootstrap>(FindObjectsInactive.Include);
            if (bootstrap == null)
            {
                Debug.LogError("[ProIntegrationSetup] Bootstrap component was not found in BootstrapScene.");
                return;
            }

            PoolService poolService = bootstrap.GetComponentInChildren<PoolService>(true);
            if (poolService == null)
            {
                var poolObject = new GameObject("PoolService");
                poolObject.transform.SetParent(bootstrap.transform, false);
                poolService = poolObject.AddComponent<PoolService>();
            }

            ThemeService themeService = bootstrap.GetComponentInChildren<ThemeService>(true);
            if (themeService == null)
            {
                var themeObject = new GameObject("ThemeService");
                themeObject.transform.SetParent(bootstrap.transform, false);
                themeService = themeObject.AddComponent<ThemeService>();
            }

            var themeSerialized = new SerializedObject(themeService);
            themeSerialized.FindProperty("activeTheme").objectReferenceValue = defaultTheme;
            themeSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(themeService);

            var bootstrapSerialized = new SerializedObject(bootstrap);
            bootstrapSerialized.FindProperty("poolService").objectReferenceValue = poolService;
            bootstrapSerialized.FindProperty("themeService").objectReferenceValue = themeService;
            bootstrapSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            var windows = Object.FindObjectsByType<BaseWindow>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var window in windows)
            {
                if (window.GetComponent<AdvancedWindowAnimator>() == null)
                    window.gameObject.AddComponent<AdvancedWindowAnimator>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string name = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
