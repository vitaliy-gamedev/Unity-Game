using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drone selection screen. Shows Scout and Bomber cards.
/// Wire in Inspector: scoutButton, bomberButton, flyButton, backButton, infoText.
/// </summary>
public class DroneSelectUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button scoutButton;
    public Button bomberButton;
    public Button flyButton;
    public Button backButton;

    [Header("Card Highlights")]
    public Image scoutCardBorder;
    public Image bomberCardBorder;
    public Color selectedColor = new Color(0.2f, 0.8f, 0.4f);
    public Color unselectedColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("Info")]
    public TMP_Text infoText;
    public TMP_Text bomberWarningText; // "No scout data yet" warning

    private GameManager.DroneType _selected = GameManager.DroneType.Scout;

    void Start()
    {
        scoutButton.onClick.AddListener(() => SelectDrone(GameManager.DroneType.Scout));
        bomberButton.onClick.AddListener(() => SelectDrone(GameManager.DroneType.Bomber));
        flyButton.onClick.AddListener(OnFly);
        backButton.onClick.AddListener(OnBack);

        // Default selection
        SelectDrone(GameManager.Instance != null
            ? GameManager.Instance.selectedDrone
            : GameManager.DroneType.Scout);
    }

    void SelectDrone(GameManager.DroneType type)
    {
        _selected = type;

        bool isScout = type == GameManager.DroneType.Scout;

        if (scoutCardBorder != null) scoutCardBorder.color = isScout ? selectedColor : unselectedColor;
        if (bomberCardBorder != null) bomberCardBorder.color = !isScout ? selectedColor : unselectedColor;

        // Show localized info
        var loc = LocalizationManager.Instance;
        if (infoText != null && loc != null)
            infoText.text = isScout ? loc.Get("select_scout") : loc.Get("select_bomber");

        // Bomber warning if no scout data
        if (bomberWarningText != null)
        {
            bool hasTargets = GameManager.Instance != null &&
                              GameManager.Instance.GetMarkedTargets().Length > 0;
            bomberWarningText.gameObject.SetActive(!isScout && !hasTargets);
            if (!isScout && !hasTargets)
            {
                var lang = GameManager.Instance?.currentLanguage ?? GameManager.Language.Ukrainian;
                bomberWarningText.text = lang == GameManager.Language.Ukrainian
                    ? "⚠ Спочатку виконайте розвідувальний виліт!"
                    : "⚠ Complete a Scout mission first!";
            }
        }
    }

    public void OnFly()
    {
        if (GameManager.Instance == null) return;
        if (_selected == GameManager.DroneType.Scout)
            GameManager.Instance.StartScoutMission();
        else
            GameManager.Instance.StartBomberMission();
    }

    public void OnBack() => GameManager.Instance?.LoadMainMenu();
}