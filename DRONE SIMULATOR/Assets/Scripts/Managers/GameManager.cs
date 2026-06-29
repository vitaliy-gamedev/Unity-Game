using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum DroneType { Scout, Bomber }
    public enum Language { Ukrainian, English }

    [Header("Settings")]
    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 1f;
    public Language currentLanguage = Language.Ukrainian;

    [Header("Mission Data")]
    public DroneType selectedDrone = DroneType.Scout;
    public TargetData[] markedTargets = new TargetData[0];

    // Scene names
    public const string SCENE_MAIN_MENU   = "MainMenu";
    public const string SCENE_DRONE_SELECT = "DroneSelect";
    public const string SCENE_SCOUT       = "ScoutMission";
    public const string SCENE_BOMBER      = "BomberMission";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    // ── Navigation ──────────────────────────────────────────────
    public void LoadMainMenu()        => SceneManager.LoadScene(SCENE_MAIN_MENU);
    public void LoadDroneSelect()     => SceneManager.LoadScene(SCENE_DRONE_SELECT);
    public void StartScoutMission()   { selectedDrone = DroneType.Scout;  SceneManager.LoadScene(SCENE_SCOUT); }
    public void StartBomberMission()  { selectedDrone = DroneType.Bomber; SceneManager.LoadScene(SCENE_BOMBER); }
    public void QuitGame()            { Application.Quit(); }

    // ── Target sharing between missions ─────────────────────────
    public void SaveMarkedTargets(TargetData[] targets) => markedTargets = targets;
    public TargetData[] GetMarkedTargets()              => markedTargets;

    // ── Settings persistence ─────────────────────────────────────
    public void ApplyVolume()
    {
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat("MasterVol", masterVolume);
        PlayerPrefs.SetFloat("MusicVol",  musicVolume);
        PlayerPrefs.SetFloat("SFXVol",    sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetLanguage(Language lang)
    {
        currentLanguage = lang;
        PlayerPrefs.SetInt("Language", (int)lang);
        PlayerPrefs.Save();
        LocalizationManager.Instance?.ApplyLanguage(lang);
    }

    void LoadSettings()
    {
        masterVolume    = PlayerPrefs.GetFloat("MasterVol", 1f);
        musicVolume     = PlayerPrefs.GetFloat("MusicVol",  0.7f);
        sfxVolume       = PlayerPrefs.GetFloat("SFXVol",    1f);
        currentLanguage = (Language)PlayerPrefs.GetInt("Language", 0);
        AudioListener.volume = masterVolume;
    }
}

[System.Serializable]
public class TargetData
{
    public Vector3 worldPosition;
    public string  gridCoordinate;   // e.g. "B4"
    public float   latitude;
    public float   longitude;
    public string  targetType;       // "Infantry", "Vehicle", etc.
    public float   timestamp;

    public TargetData(Vector3 pos, string grid, string type)
    {
        worldPosition  = pos;
        gridCoordinate = grid;
        targetType     = type;
        timestamp      = Time.time;
        // Simulate GPS from world pos
        latitude  = 48.3794f + pos.z * 0.00001f;
        longitude = 31.1656f + pos.x * 0.00001f;
    }

    public string ToDisplayString(GameManager.Language lang)
    {
        if (lang == GameManager.Language.Ukrainian)
            return $"Ціль: {targetType}\nСітка: {gridCoordinate}\nLat: {latitude:F5}\nLon: {longitude:F5}";
        else
            return $"Target: {targetType}\nGrid: {gridCoordinate}\nLat: {latitude:F5}\nLon: {longitude:F5}";
    }
}
