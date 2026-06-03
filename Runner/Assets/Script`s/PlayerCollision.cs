using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private Menu menu;

    private PlayerController playerController;

    private void Start()
    {
        // Шукаємо контролер на цьому ж об'єкті
        playerController = GetComponent<PlayerController>();

        // Автоматично шукаємо скрипт меню на сцені, якщо забули прикріпити в інспекторі
        if (menu == null)
        {
            menu = Object.FindFirstObjectByType<Menu>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Перевіряємо зіткнення з перешкодою за тегом
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("GAME OVER 💀");

            // 1. Зупиняємо рух гравця через його контролер
            if (playerController != null)
            {
                playerController.DisableController();
            }

            // 2. Вмикаємо твою панель меню
            if (menu != null)
            {
                menu.GameOver();
            }
            else
            {
                // Якщо меню раптом не знайшлося, просто стопимо час
                Time.timeScale = 0f;
            }
        }
    }
}