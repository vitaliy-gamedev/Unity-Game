using UnityEngine;

public class ShopTrigger : MonoBehaviour
{
    public GameObject shopCanvas; // Сюди тягни весь об'єкт Canvas

    void Start()
    {
        // При старті гри самі ховаємо канвас, щоб не клацати галочки вручну
        if (shopCanvas != null)
        {
            shopCanvas.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("МАГАЗИН ВІДКРИТО");
            shopCanvas.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("ГРАВЕЦЬ ПІШОВ — ХОВАЮ КАНВАС");
            CloseShop();
        }
    }

    public void CloseShop()
    {
        shopCanvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}