using UnityEngine;

namespace Deadband.InventorySystem
{
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        VeryRare
    }

    [CreateAssetMenu(menuName = "DEADBAND/Inventory/Item Data", fileName = "ID_NewItem")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ItemRarity rarity;
        [SerializeField, Min(0.01f)] private float weight = 0.25f;
        [SerializeField, Min(1)] private int maximumStack = 4;
        [SerializeField, Min(0)] private int baseValue = 10;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemRarity Rarity => rarity;
        public float Weight => weight;
        public int MaximumStack => maximumStack;
        public int BaseValue => baseValue;

        public void Configure(
            string id,
            string itemName,
            string itemDescription,
            ItemRarity itemRarity,
            float itemWeight,
            int stackSize,
            int value)
        {
            itemId = id;
            displayName = itemName;
            description = itemDescription;
            rarity = itemRarity;
            weight = Mathf.Max(0.01f, itemWeight);
            maximumStack = Mathf.Max(1, stackSize);
            baseValue = Mathf.Max(0, value);
        }
    }
}
