using UnityEngine;
using UnityEngine.UI;

public class ShopButton : MonoBehaviour
{
    [Header("Налаштування товару")]
    public string itemName;
    public int price;

    [Header("UI Елементи")]
    public Text coinDisplayText;

    // Викликається автоматично, коли кнопка стає активною (магазин відкрився)
    private void OnEnable()
    {
        UpdateBalanceUI();

        // РОБОТА З МИШКОЮ:
        Cursor.lockState = CursorLockMode.None; // Розблокувати курсор
        Cursor.visible = true;                  // Зробити його видимим
    }

    // Головна функція для кнопки
    public void ClickBuy()
    {
        if (GameManager.Instance.Coins >= price)
        {
            GameManager.Instance.Coins -= price;
            GameManager.Instance.SaveCoins();
            GameManager.Instance.AddItem(itemName);
            UpdateBalanceUI();
            FindObjectOfType<PlayerSync>()?.UpdateVisuals();

            Debug.Log($"<color=green>Успіх!</color> Куплено {itemName}");
        }
        else
        {
            Debug.Log("<color=red>Замало бабла!</color>");
        }
    }

    public void UpdateBalanceUI()
    {
        if (coinDisplayText != null)
        {
            coinDisplayText.text = "Coins: " + GameManager.Instance.Coins;
        }
    }
}