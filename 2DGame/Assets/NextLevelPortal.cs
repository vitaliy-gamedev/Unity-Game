using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelPortal : MonoBehaviour
{
    [Header("Налаштування порталу")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float delayBeforeLoad = 0.5f;

    private bool isTransitioning;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTransitioning)
            return;

        if (!collision.CompareTag(playerTag))
            return;

        isTransitioning = true;

        Invoke(nameof(LoadNextLevel), delayBeforeLoad);
    }

    private void LoadNextLevel()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        int nextScene = currentScene + 1;

        if (nextScene >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log("Останній рівень. Повернення на перший.");
            nextScene = 0;
        }

        SceneManager.LoadScene(nextScene);
    }
}