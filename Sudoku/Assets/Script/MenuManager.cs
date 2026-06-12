using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void OnStartClick()
    {
        SceneManager.LoadScene("GameCanvas");
    }

    public void OnSettingsClick()
    {
        // поки пусто, пізніше додаси
    }

    public void OnExitClick()
    {
        Application.Quit();
    }
}