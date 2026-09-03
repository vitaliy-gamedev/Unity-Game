using System.Collections.Generic;
using GameFoundation.Core;
using GameFoundation.Pro.Theme;
using TMPro;
using UnityEngine;
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
        private ISaveService _saveService;
        private ThemeService _themeService;
        private ScrollRect _scrollRect;
        private TMP_Text _titleText;
        private bool _gridBuilt;
        private Vector2Int _lastScreenSize;

        protected override void Awake()
        {
            base.Awake();

            _uiService = ServiceLocator.Get<UIService>();
            _sceneService = ServiceLocator.Get<ISceneService>();
            _localization = ServiceLocator.Get<ILocalizationService>();
            _saveService = ServiceLocator.Get<ISaveService>();
            _themeService = ServiceLocator.Get<ThemeService>();
            if (_localization != null)
                _localization.OnLanguageChanged += RefreshLocalization;
            if (_themeService != null)
                _themeService.OnThemeChanged += OnThemeChanged;

            if (backButton != null)
            {
                BackButtonStyle.Apply(backButton);
                backButton.onClick.AddListener(() => _uiService?.Back());
                RefreshLocalization();
            }
            else
                Debug.LogWarning("[LevelSelectWindow] Missing backButton reference", this);

            bool ok = true;
            if (gridParent == null) { Debug.LogWarning("[LevelSelectWindow] Missing gridParent", this); ok = false; }
            if (levelButtonPrefab == null) { Debug.LogWarning("[LevelSelectWindow] Missing levelButtonPrefab", this); ok = false; }

            if (!ok) return;
            BuildTacticalLayout();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_localization != null)
                _localization.OnLanguageChanged -= RefreshLocalization;
            if (_themeService != null)
                _themeService.OnThemeChanged -= OnThemeChanged;
        }

        private void RefreshLocalization()
        {
            if (_localization == null) return;

            if (backButton != null)
            {
                var label = backButton.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = _localization.Get("common_back");
            }

            if (_titleText != null)
                _titleText.text = _localization.Get("level_select_title");
        }

        protected override void OnOpened()
        {
            base.OnOpened();
            BuildGridIfNeeded();
            RefreshLocalization();
            ArrangeSpawnedButtons();
            ApplyTacticalPalette();
            foreach (var button in _spawned)
                button?.ApplyTacticalStyle();
        }

        private void Update()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize == _lastScreenSize) return;

            ArrangeSpawnedButtons();
        }

        private void BuildTacticalLayout()
        {
            _scrollRect = gridParent.GetComponentInParent<ScrollRect>(true);
            if (_scrollRect != null)
            {
                _scrollRect.horizontal = false;
                _scrollRect.vertical = true;
                _scrollRect.movementType = ScrollRect.MovementType.Clamped;
                _scrollRect.scrollSensitivity = 34f;

                if (_scrollRect.transform is RectTransform scrollPanel)
                {
                    scrollPanel.anchorMin = scrollPanel.anchorMax = new Vector2(0.5f, 0.5f);
                    scrollPanel.pivot = new Vector2(0.5f, 0.5f);
                    scrollPanel.anchoredPosition = new Vector2(0f, -35f);
                    scrollPanel.sizeDelta = new Vector2(720f, 560f);
                }

                if (_scrollRect.horizontalScrollbar != null)
                    _scrollRect.horizontalScrollbar.gameObject.SetActive(false);

                if (_scrollRect.verticalScrollbar != null)
                {
                    if (_scrollRect.verticalScrollbar.transform is RectTransform scrollbarRect)
                        scrollbarRect.sizeDelta = new Vector2(8f, 0f);
                    if (_scrollRect.verticalScrollbar.targetGraphic is Image handle)
                        handle.color = new Color(0.55f, 0.78f, 0.18f, 0.72f);
                }

                if (_scrollRect.GetComponent<Outline>() == null)
                {
                    var outline = _scrollRect.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.48f, 0.68f, 0.22f, 0.68f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            var titleObject = new GameObject("LevelSelectTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.layer = gameObject.layer;
            titleObject.transform.SetParent(transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 300f);
            titleRect.sizeDelta = new Vector2(720f, 64f);

            _titleText = titleObject.GetComponent<TextMeshProUGUI>();
            _titleText.font = TMP_Settings.defaultFontAsset;
            _titleText.fontSize = 34f;
            _titleText.fontWeight = FontWeight.Bold;
            _titleText.characterSpacing = 6f;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.raycastTarget = false;

            ApplyTacticalPalette();
            RefreshLocalization();
        }

        private void OnThemeChanged(ThemeData _) => ApplyTacticalPalette();

        private void ApplyTacticalPalette()
        {
            var theme = _themeService != null ? _themeService.ActiveTheme : null;

            if (_scrollRect != null && _scrollRect.TryGetComponent<Image>(out var panel))
            {
                var color = theme != null ? theme.secondary : new Color(0.05f, 0.11f, 0.065f, 1f);
                color.a = 0.58f;
                panel.material = null;
                panel.color = color;
            }

            if (_scrollRect != null && _scrollRect.viewport != null &&
                _scrollRect.viewport.TryGetComponent<Image>(out var viewportImage))
                viewportImage.color = new Color(0f, 0f, 0f, 0.04f);

            if (_titleText != null)
                _titleText.color = theme != null ? theme.accent : new Color(0.55f, 0.78f, 0.18f, 1f);
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
            float width = Mathf.Clamp(availableWidth * (portrait ? 0.82f : 0.84f), 320f, portrait ? 460f : 620f);
            float height = portrait ? 86f : 90f;
            float spacing = height + 20f;

            for (int i = 0; i < _spawned.Count; i++)
                PositionButton(_spawned[i].transform as RectTransform, i, width, height, spacing);

            if (gridParent is RectTransform contentRect)
            {
                float viewportHeight = _scrollRect != null && _scrollRect.viewport != null
                    ? _scrollRect.viewport.rect.height
                    : 500f;
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x,
                    Mathf.Max(viewportHeight, 30f + _spawned.Count * spacing));
            }
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

            _saveService?.Save(GameProgressSave.AutosaveKey, new GameProgressSave
            {
                lastSceneName = level.displaySceneName,
                savedAtUtc = System.DateTime.UtcNow.ToString("O")
            });

            _sceneService.LoadSceneAsync(level.displaySceneName);
        }

        private static bool IsSceneInBuildSettings(string sceneName)
        {
            return Application.CanStreamedLevelBeLoaded(sceneName);
        }
    }
}
