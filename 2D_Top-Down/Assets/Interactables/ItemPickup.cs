using UnityEngine;

public class ItemPickup : InteractableBase
{
    [Header("Item Config")]
    [SerializeField] private ItemConfigSO _itemConfig;

    protected override void Awake()
    {
        base.Awake();

        if (_itemConfig != null)
        {
            _promptText = $"Press E to pick up {_itemConfig.ItemName}";
        }
    }

    public override void Interact(GameObject interactor)
    {
        if (!_canInteract || _itemConfig == null) return;

        PlayerHealth playerHealth = interactor.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        switch (_itemConfig.ItemType)
        {
            case ItemType.Heart:
                playerHealth.Heal(_itemConfig.HealAmount);
                break;

            case ItemType.Coin:
                GameManager.Instance.CurrentScore += _itemConfig.ScoreValue;
                break;

            case ItemType.Key:
                InventoryManager.Instance?.AddKey(_itemConfig.KeyId);
                break;

            case ItemType.Collectible:
                GameManager.Instance.CurrentScore += _itemConfig.ScoreValue;
                break;

            case ItemType.PowerUp:
                break;
        }

        AudioManager.Instance?.PlayItemPickup();

        VFXManager.Instance?.PlayItemPickup(transform.position);

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.OnItemCollected();
        }

        Deactivate();
    }
}
