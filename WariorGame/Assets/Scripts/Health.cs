using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    [SerializeField] Slider healthSlider;
    [SerializeField] Animator animator;
    [SerializeField] PlayerInput playerInput;

    private float maxHealth = 100f;
    private float currentHealth;
    private bool isDead = false; // Прапорець, щоб уникнути повторного виклику смерті

    private void Start()
    {
        currentHealth = maxHealth;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return; // Якщо вже мертві, далі шкоду не приймаємо

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        healthSlider.value = currentHealth;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void AddHealth(float heal)
    {
        if (isDead) return; // Мертвих лікувати не можна

        currentHealth = Mathf.Clamp(currentHealth + heal, 0f, maxHealth);
        healthSlider.value = currentHealth;
    }

    private void Die()
    {
        isDead = true;
        playerInput.enabled = false;
        animator.SetBool("IsDead", true);
        Debug.Log("Player is dead!");

        
        StartCoroutine(RespawnRoutine(3f));
    }

   
    private IEnumerator RespawnRoutine(float delay)
    {
        
        yield return new WaitForSeconds(delay);

     
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}