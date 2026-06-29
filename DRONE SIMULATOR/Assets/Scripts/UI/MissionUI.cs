using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generic mission-end overlay. Call Show() from drone controller or HUD.
/// </summary>
public class MissionUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject panel;
    public TMP_Text   titleText;
    public TMP_Text   summaryText;
    public Button     returnButton;
    public Button     replayButton;

    void Start()
    {
        panel.SetActive(false);
        returnButton.onClick.AddListener(OnReturn);
        if (replayButton != null) replayButton.onClick.AddListener(OnReplay);
    }

    public void ShowScoutComplete(int targetCount)
    {
        panel.SetActive(true);
        var loc  = LocalizationManager.Instance;
        var lang = GameManager.Instance?.currentLanguage ?? GameManager.Language.Ukrainian;

        titleText.text = loc != null
            ? loc.Get("mission_scout_complete")
            : "РОЗВІДКА ЗАВЕРШЕНА";

        summaryText.text = lang == GameManager.Language.Ukrainian
            ? $"Цілей позначено: <b>{targetCount}</b>\n\nКоординати передані бомберу."
            : $"Targets marked: <b>{targetCount}</b>\n\nCoordinates relayed to bomber.";

        if (returnButton != null)
        {
            var txt = returnButton.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = loc?.Get("btn_return_menu") ?? "MENU";
        }

        Time.timeScale = 0f; // Pause
    }

    public void ShowBomberComplete(int destroyed, int total)
    {
        panel.SetActive(true);
        var loc  = LocalizationManager.Instance;
        var lang = GameManager.Instance?.currentLanguage ?? GameManager.Language.Ukrainian;

        titleText.text = loc?.Get("mission_bomber_complete") ?? "МІСІЯ ВИКОНАНА";

        string acc = destroyed >= total ? (lang == GameManager.Language.Ukrainian ? "★ ВІДМІННО" : "★ EXCELLENT") :
                     destroyed > total / 2 ? (lang == GameManager.Language.Ukrainian ? "✓ ВИКОНАНО" : "✓ COMPLETED") :
                     (lang == GameManager.Language.Ukrainian ? "△ ЧАСТКОВО" : "△ PARTIAL");

        summaryText.text = lang == GameManager.Language.Ukrainian
            ? $"Знищено цілей: <b>{destroyed}/{total}</b>\n\n{acc}"
            : $"Targets destroyed: <b>{destroyed}/{total}</b>\n\n{acc}";

        Time.timeScale = 0f;
    }

    void OnReturn()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.LoadDroneSelect();
    }

    void OnReplay()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
