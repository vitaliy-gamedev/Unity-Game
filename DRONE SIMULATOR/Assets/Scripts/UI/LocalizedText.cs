using UnityEngine;
using TMPro;

/// <summary>
/// Attach to any TextMeshPro object. Set locKey to auto-update on language change.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Key from LocalizationManager dictionary")]
    public string locKey;

    private TMP_Text _text;

    void Awake() => _text = GetComponent<TMP_Text>();

    void Start() => Refresh();

    public void Refresh()
    {
        if (string.IsNullOrEmpty(locKey)) return;
        if (_text == null) _text = GetComponent<TMP_Text>();
        _text.text = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.Get(locKey)
            : locKey;
    }
}
