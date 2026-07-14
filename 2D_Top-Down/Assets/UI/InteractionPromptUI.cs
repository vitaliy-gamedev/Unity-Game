using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _promptPanel;
    [SerializeField] private TextMeshProUGUI _promptText;

    private void Awake()
    {
        if (_promptPanel != null)
            _promptPanel.SetActive(false);
    }

    public void ShowPrompt(bool show, string text)
    {
        if (_promptPanel == null) return;

        _promptPanel.SetActive(show);

        if (show && _promptText != null)
        {
            _promptText.text = text;
        }
    }

    public void HidePrompt()
    {
        ShowPrompt(false, "");
    }
}
