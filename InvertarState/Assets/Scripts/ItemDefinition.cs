using UnityEngine;

[CreateAssetMenu(fileName = "ItemDefinition", menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [SerializeField] private string _itemId;
    [SerializeField] private string _displayName;
    [SerializeField] private int _maxStackSize = 64;
    [SerializeField] private Color _iconColor = Color.white;

    public string ItemId => _itemId;
    public string DisplayName => _displayName;
    public int MaxStackSize => _maxStackSize;
    public Color IconColor => _iconColor;

    public void Initialize(string itemId, string displayName, int maxStackSize, Color iconColor)
    {
        _itemId = itemId;
        _displayName = displayName;
        _maxStackSize = maxStackSize;
        _iconColor = iconColor;
    }
}