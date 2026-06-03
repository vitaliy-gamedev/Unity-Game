using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private Text coinText;

    private int coins = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AddCoin(int amount)
    {
        coins += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (coinText != null)
        {
            coinText.text = "🪙 " + coins;
        }
    }

    // ❗ Новий метод, який віддасть кількість монет для меню програшу
    public int GetCoinsCount()
    {
        return coins;
    }

    public void ShowFinalCoins()
    {
        Debug.Log("Гра закінчена! Фінальний рахунок монет: " + coins);
    }
}