using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    private Text _text;
    private Button _btn;

    public bool IsTaken { get; private set; }

    private void Awake()
    {
        _text = GetComponentInChildren<Text>();
        _btn = GetComponent<Button>();

        _text.text = "";
    }

    public void Init(System.Action<Cell> onClick)
    {
        _btn.onClick.RemoveAllListeners();
        _btn.onClick.AddListener(() => onClick(this));
    }
    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
    {
        Debug.Log("Клік отримано на об'єкті: " + gameObject.name);
    }
       
    public void SetSymbol(string symbol)
    {
        _text.text = symbol;
        IsTaken = (symbol != ""); 
    }
    public void Clear()
    {
        _text.text = "";
        IsTaken = false; 
    }
}