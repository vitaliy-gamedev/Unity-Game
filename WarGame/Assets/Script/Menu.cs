using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject _bntMenu;
    [SerializeField] private GameObject _btnStart;
    [SerializeField] private GameObject _btnExit;

    [SerializeField] private GameObject _panelOptions;

    public void StartGame()
    {
        SceneManager.LoadScene("game");
    }

    public void ShowOptions()
    {
        _panelOptions.SetActive(true);

        _bntMenu.SetActive(false);
        _btnStart.SetActive(false);
        _btnExit.SetActive(false);
    }

    public void CloseOptions()
    {
        _panelOptions.SetActive(false);

        _bntMenu.SetActive(true);
        _btnStart.SetActive(true);
        _btnExit.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit");
    }
}
