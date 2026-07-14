using UnityEngine;

[CreateAssetMenu(menuName = "Config/Level Config", fileName = "LevelConfig")]
public class LevelConfigSO : ScriptableObject
{
    [Header("Level Info")] [SerializeField]
    private int _levelNumber = 1;

    [SerializeField] private string _displayName = "Level 1";
    [SerializeField] private string _sceneName = "GameLevel_1";
    [SerializeField] private Sprite _previewImage;

    [Header("Win Conditions")] [SerializeField]
    private int _requiredItemCount = 0;

    [SerializeField] private bool _requiresAllEnemiesKilled = false;

    [Header("Level Settings")] [SerializeField]
    private float _timeLimit = 0f;

    public int LevelNumber => _levelNumber;
    public string DisplayName => _displayName;
    public string SceneName => _sceneName;
    public Sprite PreviewImage => _previewImage;
    public int RequiredItemCount => _requiredItemCount;
    public bool RequiresAllEnemiesKilled => _requiresAllEnemiesKilled;
    public float TimeLimit => _timeLimit;
}