using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void OpenAutoBattle()
    {
        SceneManager.LoadScene("AutoBattle");
    }

    public void OpenManualBattle()
    {
        SceneManager.LoadScene("ManualBattle");
    }

    public void ExitGame()
    {
        Application.Quit();

        Debug.Log("EXIT");
    }
}