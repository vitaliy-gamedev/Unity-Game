using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectUI : MonoBehaviour
{
    [Header("Button Template")]
    [SerializeField] private Button _levelButtonPrefab;
    [SerializeField] private Transform _buttonGrid;

    [Header("Navigation")]
    [SerializeField] private Button _backButton;

    [Header("Settings")]
    [SerializeField] private Color _lockedColor = Color.gray;
    [SerializeField] private Color _availableColor = Color.white;
    [SerializeField] private Color _completedColor = Color.green;

    private void Start()
    {
        GameStateManager.Instance?.SetMenu();
        GenerateLevelButtons();

        if (_backButton != null)
            _backButton.onClick.AddListener(OnBackClicked);
    }

    private void GenerateLevelButtons()
    {
        if (_levelButtonPrefab == null || _buttonGrid == null) return;

        IReadOnlyList<LevelConfigSO> levels = GameManager.Instance?.GetAllLevelConfigs();
        if (levels == null || levels.Count == 0)
        {
            Debug.LogWarning("[LevelSelectUI] No level configs found!");
            return;
        }

        foreach (LevelConfigSO level in levels)
        {
            Button button = Instantiate(_levelButtonPrefab, _buttonGrid);
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
                buttonText.text = level.DisplayName;

            int capturedLevel = level.LevelNumber;

            if (GameManager.Instance.IsLevelUnlocked(level.LevelNumber))
            {
                ColorBlock cb = button.colors;
                cb.normalColor = GameManager.Instance.IsLevelCompleted(level.LevelNumber)
                    ? _completedColor
                    : _availableColor;
                button.colors = cb;

                button.onClick.AddListener(() => OnLevelClicked(capturedLevel));
                button.interactable = true;
            }
            else
            {
                ColorBlock cb = button.colors;
                cb.normalColor = _lockedColor;
                button.colors = cb;

                button.interactable = false;
            }
        }
    }

    private void OnLevelClicked(int levelNumber)
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.LoadLevel(levelNumber);
    }

    private void OnBackClicked()
    {
        AudioManager.Instance?.PlayButtonClick();
        GameManager.Instance?.LoadMainMenu();
    }
}
