using UnityEngine;
using UnityEngine.SceneManagement; 

public class MenuManage : MonoBehaviour
{
    
    public void StartGame()
    {        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);          
    }
        
    public void OpenOptions()
    {   
        Debug.Log("Відкрито налаштування");
    }

    
    public void ExitGame()
    {
        
        Application.Quit();                
        Debug.Log("Гра закрилася");
    }
}