using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Configs")] [SerializeField] private List<LevelConfigSO> _levelConfigs = new List<LevelConfigSO>();

    public SaveData CurrentSave { get; private set; }
    public int CurrentLevelNumber { get; private set; } = 1;
    public int CurrentScore { get; set; }

    private static GameManager _instance;

    public static GameManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CurrentSave = SaveManager.Load();
        CurrentScore = CurrentSave.totalScore;
        SettingsManager.LoadAndApplySettings();
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadLevelSelect()
    {
        SceneManager.LoadScene("LevelSelect");
    }

    public void LoadLevel(int levelNumber)
    {
        LevelConfigSO config = GetLevelConfig(levelNumber);
        if (config == null)
        {
            Debug.LogError($"[GameManager] Config for level {levelNumber} not found!");
            return;
        }

        CurrentLevelNumber = levelNumber;
        SceneManager.LoadScene(config.SceneName);
    }

    public void RestartCurrentLevel()
    {
        LoadLevel(CurrentLevelNumber);
    }

    public void LoadNextLevel()
    {
        int next = CurrentLevelNumber + 1;
        if (GetLevelConfig(next) != null)
        {
            LoadLevel(next);
        }
        else
        {
            LoadLevelSelect();
        }
    }

    public void CompleteCurrentLevel()
    {
        if (CurrentSave == null) return;

        CurrentSave.CompleteLevel(CurrentLevelNumber);
        CurrentSave.totalScore = CurrentScore;
        SaveManager.Save(CurrentSave);
    }

    public bool IsLevelUnlocked(int levelNumber)
    {
        return CurrentSave != null && levelNumber <= CurrentSave.maxUnlockedLevel;
    }

    public bool IsLevelCompleted(int levelNumber)
    {
        return CurrentSave != null && CurrentSave.IsLevelCompleted(levelNumber);
    }

    public LevelConfigSO GetLevelConfig(int levelNumber)
    {
        return _levelConfigs.Find(c => c.LevelNumber == levelNumber);
    }

    public IReadOnlyList<LevelConfigSO> GetAllLevelConfigs() => _levelConfigs;

    public void ResetAllProgress()
    {
        SaveManager.DeleteSave();
        CurrentSave = new SaveData();
        CurrentScore = 0;
    }
}