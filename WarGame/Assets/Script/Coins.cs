using UnityEngine;

public class Coin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager is missing!");
            return;
        }

        GameManager.Instance.Coins++;

        PlayerPrefs.SetInt(
            "SavedCoins",
            GameManager.Instance.Coins
        );

        other.GetComponent<PlayerSync>()?.UpdateVisuals();

        Destroy(gameObject);
    }
}