using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerSync : MonoBehaviour
{
    [Serializable]
    public struct WeaponMapping
    {
        public string Name;
        public GameObject Visual;
    }

    public WeaponMapping[] AllWeapons;
    public Text CoinText;

    private void Start()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager is missing!");
            return;
        }

        // зброя
        foreach (var weapon in AllWeapons)
        {
            if (weapon.Visual == null) continue;

            bool hasItem = GameManager.Instance.HasItem(weapon.Name);
            weapon.Visual.SetActive(hasItem);
        }

        // монети
        if (CoinText != null)
        {
            CoinText.text = GameManager.Instance.Coins.ToString();
        }
    }
}