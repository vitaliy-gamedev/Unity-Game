using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int Coins;
    public List<string> CollectedItems = new List<string>();

    private void Awake()
    {
        // Singleton захист
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    private void LoadData()
    {
        Coins = PlayerPrefs.GetInt("SavedCoins", 0);
    }

    public void AddCoin(int amount = 1)
    {
        Coins += amount;
        SaveCoins();
    }

    public void SaveCoins()
    {
        PlayerPrefs.SetInt("SavedCoins", Coins);
        PlayerPrefs.Save();
    }

    public void AddItem(string itemName)
    {
        if (!CollectedItems.Contains(itemName))
        {
            CollectedItems.Add(itemName);
        }
    }

    public bool HasItem(string itemName)
    {
        return CollectedItems.Contains(itemName);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // тут просто без крашів
        PlayerSync player = FindObjectOfType<PlayerSync>();

        if (player != null)
        {
            player.UpdateVisuals();
        }
    }
}