using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DemolishButton : MonoBehaviour
{
    [Tooltip("Текст на кнопці (legacy UI Text). Необов'язково")]
    [SerializeField] private Text label;
    [SerializeField] private string idleText = "Знести";
    [SerializeField] private string activeText = "Скасувати";

    [Tooltip("Підсвічування кнопки (зазвичай Image самої кнопки). Необов'язково")]
    [SerializeField] private Graphic highlight;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color activeColor = new(1f, 0.5f, 0.5f, 1f);

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (highlight == null) highlight = GetComponent<Image>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(OnClick);
        if (DemolishManager.Instance != null)
            DemolishManager.Instance.OnModeChanged += Refresh;
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(OnClick);
        if (DemolishManager.Instance != null)
            DemolishManager.Instance.OnModeChanged -= Refresh;
    }

    private void Start() => Refresh(DemolishManager.Instance != null && DemolishManager.Instance.IsActive);

    private void OnClick()
    {
        if (DemolishManager.Instance != null) DemolishManager.Instance.Toggle();
    }

    private void Refresh(bool active)
    {
        if (label != null) label.text = active ? activeText : idleText;
        if (highlight != null) highlight.color = active ? activeColor : idleColor;
    }
}
