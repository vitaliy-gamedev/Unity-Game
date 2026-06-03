using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    [Header("Coins")]
    [SerializeField] private float _coins = 0;
    [SerializeField] private float _countCoinsClick = 1;

    [Header("Upgrade Data")]
    [SerializeField] private float[] _priceUp;
    [SerializeField] private float[] _lvlUp;

    [Header("UI")]
    [SerializeField] private Text[] _priceUpText;
    [SerializeField] private Text[] _lvlUpText;
    [SerializeField] private Text _coinsText;
    [SerializeField] private Text _clickPowerText;

    [Header("Panels")]
    [SerializeField] private GameObject _shopPanel;

    private void Start()
    {
        UpdateUI();
    }

    public void AddCoins()
    {
        _coins += _countCoinsClick;
        UpdateUI();
    }

    public void ShowShopPanel()
    {
        _shopPanel.SetActive(!_shopPanel.activeSelf);
    }

    public void BuyUp(int index)
    {
        // Захист від неправильного index
        if (index < 0 || index >= _priceUp.Length)
            return;

        // Перевірка грошей
        if (_coins >= _priceUp[index])
        {
            // Відняти гроші
            _coins -= _priceUp[index];

            // Підняти рівень
            _lvlUp[index]++;

            // Сила кліку
            _countCoinsClick += _lvlUp[index] + 1;

            // Нова ціна
            _priceUp[index] *= 2;

            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // Coins
        _coinsText.text = _coins.ToString("0") + "$";

        // Click Power
        if (_clickPowerText != null)
        {
            _clickPowerText.text = "" + _countCoinsClick.ToString("0");
        }

        // Upgrade UI
        for (int i = 0; i < _priceUp.Length; i++)
        {
            // Ціна
            if (i < _priceUpText.Length)
            {
                _priceUpText[i].text = _priceUp[i].ToString("0") + "$";
            }

            // Рівень
            if (i < _lvlUpText.Length)
            {
                _lvlUpText[i].text = "Lvl " + _lvlUp[i].ToString("0");
            }
        }
    }
}