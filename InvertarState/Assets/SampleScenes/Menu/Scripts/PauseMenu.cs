using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private Toggle m_MenuToggle;
	private float m_TimeScaleRef = 1f;
    private float m_VolumeRef = 1f;
    private bool m_Paused;
    private bool m_PreviousCursorVisible;
    private CursorLockMode m_PreviousCursorLockState;


    void Awake()
    {
        m_MenuToggle = GetComponent <Toggle> ();
	}


    private void MenuOn ()
    {
        m_TimeScaleRef = Time.timeScale;
        Time.timeScale = 0f;

        m_VolumeRef = AudioListener.volume;
        AudioListener.volume = 0f;

        m_PreviousCursorVisible = Cursor.visible;
        m_PreviousCursorLockState = Cursor.lockState;
        KeepCursorUnlocked();

        m_Paused = true;
    }


    public void MenuOff ()
    {
        Time.timeScale = m_TimeScaleRef;
        AudioListener.volume = m_VolumeRef;
        Cursor.visible = m_PreviousCursorVisible;
        Cursor.lockState = m_PreviousCursorLockState;
        m_Paused = false;
    }


    public void OnMenuStatusChange ()
    {
        if (m_MenuToggle.isOn && !m_Paused)
        {
            MenuOn();
        }
        else if (!m_MenuToggle.isOn && m_Paused)
        {
            MenuOff();
        }
    }


#if !MOBILE_INPUT
	void Update()
	{
		if(Input.GetKeyUp(KeyCode.Escape))
		{
		    m_MenuToggle.isOn = !m_MenuToggle.isOn;
		}

        if (m_Paused)
        {
            KeepCursorUnlocked();
        }
	}
#endif

    private void KeepCursorUnlocked()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

}
