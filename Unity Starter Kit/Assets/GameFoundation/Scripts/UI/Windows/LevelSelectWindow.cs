using System.Collections.Generic;
using GameFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class LevelSelectWindow : BaseWindow
    {
        [Header("Data")]
        [SerializeField] private List<LevelData> levels = new();

        [Header("References")]
        [SerializeField] private Transform gridParent;
        [SerializeField] private LevelButton levelButtonPrefab;
        [SerializeField] private Button backButton;

        private readonly List<LevelButton> _spawned = new();
        private UIService _uiService;
        private ISceneService _sceneService;
        private ILocalizationService _localization;
        private bool _gridBuilt;
        private Vector2Int _lastScreenSize;

        protected override void Awake()
        {
            base.Awake();

            _uiService = ServiceLocator.Get<UIService>();
            _sceneService = ServiceLocator.Get<ISceneService>();
            _localization = ServiceLocator.Get<ILocalizationService>();
            if (_localization != null)
                _localization.OnLanguageChanged += RefreshLocalization;

            if (backButton != null)
            {
                backButton.onClick.AddListener(() => _uiService?.Back());
                RefreshLocalization();
            }
            else
                Debug.LogWarning("[LevelSelectWindow] Missing backButton reference", this);

            bool ok = true;
            if (gridParent == null) { Debug.LogWarning("[LevelSelectWindow] Missing gridParent", this); ok = false; }
            if (levelButtonPrefab == null) { Debug.LogWarning("[LevelSelectWindow] Missing levelButtonPrefab", this); ok = false; }

            if (!ok) return;
        }

        private void OnDestroy()
        {
            if (_localization != null)
                _localization.OnLanguageChanged -= RefreshLocalization;
        }

        private void RefreshLocalization()
        {
            if (backButton == null || _localization == null) return;

            var label = backButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = _localization.Get("common_back");
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            BuildGridIfNeeded();
            RefreshLocalization();
            ArrangeSpawnedButtons();
        }

        private void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize == _lastScreenSize) return;

            ArrangeSpawnedButtons();
        }

        private void BuildGridIfNeeded()
        {
            if (_gridBuilt) return;
            _gridBuilt = true;

            if (levels == null || levels.Count == 0)
            {
                Debug.LogWarning("[LevelSelectWindow] No levels assigned, so there is nothing to show.", this);
                return;
            }

            foreach (var level in levels)
            {
                if (level == null)
                {
                    Debug.LogWarning("[LevelSelectWindow] Levels list contains an empty slot.", this);
                    continue;
                }

                var button = Instantiate(levelButtonPrefab, gridParent);
                button.gameObject.SetActive(true);
                button.Setup(level, level.unlockedByDefault, OnLevelPicked);
                _spawned.Add(button);
            }

            ArrangeSpawnedButtons();
        }

        private void ArrangeSpawnedButtons()
        {
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            bool portrait = Screen.height > Screen.width;
            float availableWidth = GetContentWidth();
            float width = Mathf.Clamp(availableWidth * (portrait ? 0.76f : 0.42f), 300f, portrait ? 420f : 520f);
            float height = portrait ? 86f : 90f;
            float spacing = height + 20f;

            for (int i = 0; i < _spawned.Count; i++)
                PositionButton(_spawned[i].transform as RectTransform, i, width, height, spacing);
        }

        private float GetContentWidth()
        {
            if (gridParent is RectTransform contentRect && contentRect.rect.width > 0f)
                return contentRect.rect.width;

            if (transform is RectTransform windowRect && windowRect.rect.width > 0f)
                return windowRect.rect.width;

            return Screen.height > Screen.width ? 1080f : 1920f;
        }

        private static void PositionButton(RectTransform rect, int index, float width, float height, float spacing)
        {
            if (rect == null) return;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(0f, -20f - index * spacing);
        }

        private void OnLevelPicked(LevelData level)
        {
            if (_sceneService == null)
            {
                Debug.LogError("[LevelSelectWindow] ISceneService is not registered - cannot load level.");
                return;
            }

            if (level == null || string.IsNullOrWhiteSpace(level.displaySceneName))
            {
                Debug.LogWarning("[LevelSelectWindow] Level data has no scene name assigned.", this);
                return;
            }

            if (!IsSceneInBuildSettings(level.displaySceneName))
            {
                Debug.LogWarning($"[LevelSelectWindow] Scene '{level.displaySceneName}' is not in Build Settings.", this);
                return;
            }

            _sceneService.LoadSceneAsync(level.displaySceneName);
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            return SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{sceneName}.unity") >= 0;
        }
    }
}
