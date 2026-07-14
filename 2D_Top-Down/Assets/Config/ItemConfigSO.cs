using UnityEngine;

[CreateAssetMenu(menuName = "Config/Item Config", fileName = "ItemConfig")]
public class ItemConfigSO : ScriptableObject
{
    [Header("Basic Info")] [SerializeField]
    private string _itemName = "Item";

    [SerializeField] private ItemType _itemType = ItemType.Heart;
    [SerializeField] private Sprite _icon;

    [Header("Values")] [SerializeField] private int _healAmount = 1;
    [SerializeField] private int _scoreValue = 50;
    [SerializeField] private string _keyId = "";

    public string ItemName => _itemName;
    public ItemType ItemType => _itemType;
    public Sprite Icon => _icon;
    public int HealAmount => _healAmount;
    public int ScoreValue => _scoreValue;
    public string KeyId => _keyId;
}

public enum ItemType
{
    Heart,
    Coin,
    Key,
    PowerUp,
    Collectible
}