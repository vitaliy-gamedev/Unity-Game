using UnityEngine;

public class Items : MonoBehaviour
{
    public string itemName;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager missing!");
            return;
        }

        if (!GameManager.Instance.CollectedItems.Contains(itemName))
        {
            GameManager.Instance.CollectedItems.Add(itemName);
        }

        PlayerSync sync = other.GetComponent<PlayerSync>();
        if (sync != null)
        {
            sync.UpdateVisuals();
        }

        gameObject.SetActive(false);
    }
}