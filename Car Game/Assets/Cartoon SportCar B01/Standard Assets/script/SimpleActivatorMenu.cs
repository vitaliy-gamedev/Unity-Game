using UnityEngine;
using TMPro; // Для сучасного інтерфейсу. Якщо використовуєте звичайний Text, замініть на UnityEngine.UI

namespace UnityStandardAssets.Utility
{
    public class SimpleActivatorMenu : MonoBehaviour
    {
        public TextMeshProUGUI camSwitchButton;
        public GameObject[] objects;

        private int m_CurrentActiveObject;

        private void OnEnable()
        {
            if (objects.Length > 0)
            {
                m_CurrentActiveObject = 0;
                UpdateUI();
            }
        }

        public void NextCamera()
        {
            if (objects.Length == 0) return;

           
            m_CurrentActiveObject = (m_CurrentActiveObject + 1) % objects.Length;

            for (int i = 0; i < objects.Length; i++)
            {
             
                bool isActive = (i == m_CurrentActiveObject);
                objects[i].SetActive(isActive);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (camSwitchButton != null)
            {
                
                camSwitchButton.text = objects[m_CurrentActiveObject].name;
            }
        }
    }
}