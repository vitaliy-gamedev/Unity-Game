using System.Collections.Generic;
using GameFoundation.Core;
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

        protected override void Awake()
        {
            base.Awake();

            _uiService = ServiceLocator.Get<UIService>();
            _uiService?.Register(this);
            _sceneService = ServiceLocator.Get<ISceneService>();

            // Завжди вішаємо слухач на кнопку назад, незалежно від інших помилок
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => _uiService.Back());
            }
            else
            {
                Debug.LogWarning("[LevelSelectWindow] Missing backButton reference", this);
            }

            bool ok = true;
            if (gridParent == null) { Debug.LogWarning("[LevelSelectWindow] Missing gridParent", this); ok = false; }
            if (levelButtonPrefab == null) { Debug.LogWarning("[LevelSelectWindow] Missing levelButtonPrefab", this); ok = false; }

            if (!ok) return;

            BuildGrid();
        }

        private void BuildGrid()
        {
            foreach (var level in levels)
            {
                var button = Instantiate(levelButtonPrefab, gridParent);
                bool unlocked = level.unlockedByDefault;
                button.Setup(level, unlocked, OnLevelPicked);
                _spawned.Add(button);
            }
        }

        private void OnLevelPicked(LevelData level)
        {
            if (_sceneService == null)
            {
                Debug.LogError("[LevelSelectWindow] ISceneService is not registered — cannot load level.");
                return;
            }
            _sceneService.LoadSceneAsync(level.displaySceneName);
        }
    }
}