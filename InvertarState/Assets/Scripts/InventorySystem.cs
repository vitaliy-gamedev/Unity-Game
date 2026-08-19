using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public enum InventorySlotArea
{
    Inventory,
    Crafting,
    Result
}

public class InventorySystem : MonoBehaviour
{
    private const int InventorySlotCount = 27;
    private const int CraftingSlotCount = 4;
    private const string InventorySaveKey = "InvertarState.Inventory";

    private readonly List<InventorySlotData> _inventorySlots = new();
    private readonly List<InventorySlotData> _craftingSlots = new();
    private readonly List<RecipeDefinition> _recipes = new();
    private readonly Dictionary<string, ItemDefinition> _itemsById = new();

    private readonly InventorySlotData _cursorSlot = new();
    private bool _loadedSavedInventory;

    public IReadOnlyList<InventorySlotData> InventorySlots => _inventorySlots;
    public IReadOnlyList<InventorySlotData> CraftingSlots => _craftingSlots;
    public InventorySlotData CursorSlot => _cursorSlot;

    public void Initialize(
        IEnumerable<RecipeDefinition> recipes,
        IEnumerable<ItemDefinition> itemDefinitions = null)
    {
        _inventorySlots.Clear();
        _craftingSlots.Clear();
        _recipes.Clear();
        _itemsById.Clear();
        _loadedSavedInventory = false;

        for (int i = 0; i < InventorySlotCount; i++)
        {
            _inventorySlots.Add(new InventorySlotData());
        }

        for (int i = 0; i < CraftingSlotCount; i++)
        {
            _craftingSlots.Add(new InventorySlotData());
        }

        if (recipes != null)
        {
            _recipes.AddRange(recipes);
        }

        if (itemDefinitions != null)
        {
            foreach (ItemDefinition item in itemDefinitions)
            {
                if (item != null && !string.IsNullOrEmpty(item.ItemId))
                {
                    _itemsById[item.ItemId] = item;
                }
            }
        }

        LoadInventory();
    }

    public InventorySlotData GetSlot(InventorySlotArea area, int index)
    {
        IReadOnlyList<InventorySlotData> slots = area switch
        {
            InventorySlotArea.Inventory => _inventorySlots,
            InventorySlotArea.Crafting => _craftingSlots,
            _ => null
        };

        if (slots == null || index < 0 || index >= slots.Count)
        {
            return null;
        }

        return slots[index];
    }

    public InventorySlotData GetCraftingResult()
    {
        RecipeDefinition recipe = FindMatchingRecipe();

        if (recipe == null)
        {
            return new InventorySlotData();
        }

        InventorySlotData result = new InventorySlotData();
        result.Set(recipe.Result, recipe.ResultAmount);
        return result;
    }

    public void HandleSlotClick(InventorySlotArea area, int index, PointerEventData.InputButton button, bool exactSplit)
    {
        string beforeState = GetInventorySaveData();

        if (area == InventorySlotArea.Result)
        {
            if (button == PointerEventData.InputButton.Left)
            {
                TryCraft();
                SaveInventoryIfChanged(beforeState);
            }

            return;
        }

        InventorySlotData slot = GetSlot(area, index);

        if (slot == null)
        {
            return;
        }

        if (exactSplit && button == PointerEventData.InputButton.Right)
        {
            return;
        }

        if (button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick(slot);
            SaveInventoryIfChanged(beforeState);
            return;
        }

        if (button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(slot);
            SaveInventoryIfChanged(beforeState);
        }
    }

    public bool TakeExactFromSlot(InventorySlotArea area, int index, int amount)
    {
        if (!_cursorSlot.IsEmpty)
        {
            return false;
        }

        InventorySlotData source = GetSlot(area, index);

        if (source == null || source.IsEmpty)
        {
            return false;
        }

        if (amount <= 0 || amount > source.Amount)
        {
            return false;
        }

        _cursorSlot.Set(source.Item, amount);
        source.Remove(amount);
        SaveInventory();

        return true;
    }

    public bool TransferSlot(
        InventorySlotArea sourceArea,
        int sourceIndex,
        InventorySlotArea targetArea,
        int targetIndex)
    {
        if (sourceArea == InventorySlotArea.Result || targetArea == InventorySlotArea.Result)
        {
            return false;
        }

        InventorySlotData source = GetSlot(sourceArea, sourceIndex);
        InventorySlotData target = GetSlot(targetArea, targetIndex);

        if (source == null || target == null || source.IsEmpty)
        {
            return false;
        }

        if (target.IsEmpty)
        {
            target.Set(source.Item, source.Amount);
            source.Clear();
            SaveInventory();
            return true;
        }

        if (target.Item == source.Item)
        {
            int availableSpace = target.Item.MaxStackSize - target.Amount;

            if (availableSpace <= 0)
            {
                return false;
            }

            int transferAmount = Mathf.Min(source.Amount, availableSpace);

            target.Add(transferAmount);
            source.Remove(transferAmount);

            SaveInventory();
            return true;
        }

        ItemDefinition sourceItem = source.Item;
        int sourceAmount = source.Amount;

        source.Set(target.Item, target.Amount);
        target.Set(sourceItem, sourceAmount);

        SaveInventory();
        return true;
    }

    public bool TryAddItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        if (!CanAddItem(item, amount))
        {
            return false;
        }

        int remaining = amount;

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventorySlotData slot = _inventorySlots[i];

            if (slot.IsEmpty || slot.Item != item)
            {
                continue;
            }

            int availableSpace = item.MaxStackSize - slot.Amount;
            int addAmount = Mathf.Min(remaining, availableSpace);

            slot.Add(addAmount);
            remaining -= addAmount;

            if (remaining <= 0)
            {
                SaveInventory();
                return true;
            }
        }

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventorySlotData slot = _inventorySlots[i];

            if (!slot.IsEmpty)
            {
                continue;
            }

            int addAmount = Mathf.Min(remaining, item.MaxStackSize);
            slot.Set(item, addAmount);
            remaining -= addAmount;

            if (remaining <= 0)
            {
                SaveInventory();
                return true;
            }
        }

        return remaining <= 0;
    }

    public bool ReturnCursorToInventory()
    {
        if (_cursorSlot.IsEmpty)
        {
            return true;
        }

        if (TryAddItem(_cursorSlot.Item, _cursorSlot.Amount))
        {
            _cursorSlot.Clear();
            return true;
        }

        return false;
    }

    public bool CanAddItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        int availableSpace = 0;

        foreach (InventorySlotData slot in _inventorySlots)
        {
            if (slot.IsEmpty)
            {
                availableSpace += item.MaxStackSize;
            }
            else if (slot.Item == item)
            {
                availableSpace += item.MaxStackSize - slot.Amount;
            }

            if (availableSpace >= amount)
            {
                return true;
            }
        }

        return false;
    }

    public void AddStartingItem(ItemDefinition item, int amount)
    {
        if (_loadedSavedInventory)
        {
            return;
        }

        TryAddItem(item, amount);
    }

    private void HandleLeftClick(InventorySlotData slot)
    {
        if (_cursorSlot.IsEmpty)
        {
            if (!slot.IsEmpty)
            {
                _cursorSlot.Set(slot.Item, slot.Amount);
                slot.Clear();
            }

            return;
        }

        if (slot.IsEmpty)
        {
            int amount = Mathf.Min(_cursorSlot.Amount, _cursorSlot.Item.MaxStackSize);
            slot.Set(_cursorSlot.Item, amount);
            _cursorSlot.Remove(amount);
            return;
        }

        if (slot.Item == _cursorSlot.Item)
        {
            int availableSpace = slot.Item.MaxStackSize - slot.Amount;

            if (availableSpace <= 0)
            {
                return;
            }

            int amount = Mathf.Min(_cursorSlot.Amount, availableSpace);

            slot.Add(amount);
            _cursorSlot.Remove(amount);

            return;
        }

        ItemDefinition cursorItem = _cursorSlot.Item;
        int cursorAmount = _cursorSlot.Amount;

        _cursorSlot.Set(slot.Item, slot.Amount);
        slot.Set(cursorItem, cursorAmount);
    }

    private void HandleRightClick(InventorySlotData slot)
    {
        if (_cursorSlot.IsEmpty)
        {
            if (slot.IsEmpty)
            {
                return;
            }

            int amount = Mathf.CeilToInt(slot.Amount / 2f);

            _cursorSlot.Set(slot.Item, amount);
            slot.Remove(amount);

            return;
        }

        if (slot.IsEmpty)
        {
            slot.Set(_cursorSlot.Item, 1);
            _cursorSlot.Remove(1);
            return;
        }

        if (slot.Item != _cursorSlot.Item)
        {
            return;
        }

        if (slot.Amount >= slot.Item.MaxStackSize)
        {
            return;
        }

        slot.Add(1);
        _cursorSlot.Remove(1);
    }

    private void TryCraft()
    {
        RecipeDefinition recipe = FindMatchingRecipe();

        if (recipe == null)
        {
            return;
        }

        if (!_cursorSlot.IsEmpty)
        {
            if (_cursorSlot.Item != recipe.Result)
            {
                return;
            }

            if (_cursorSlot.Amount + recipe.ResultAmount > recipe.Result.MaxStackSize)
            {
                return;
            }
        }

        ConsumeRecipe(recipe);

        if (_cursorSlot.IsEmpty)
        {
            _cursorSlot.Set(recipe.Result, recipe.ResultAmount);
        }
        else
        {
            _cursorSlot.Add(recipe.ResultAmount);
        }

        SaveInventory();
    }

    private void SaveInventoryIfChanged(string beforeState)
    {
        if (beforeState != GetInventorySaveData())
        {
            SaveInventory();
        }
    }

    private void SaveInventory()
    {
        PlayerPrefs.SetString(InventorySaveKey, GetInventorySaveData());
        PlayerPrefs.Save();
    }

    private void LoadInventory()
    {
        string data = PlayerPrefs.GetString(InventorySaveKey, "");

        if (string.IsNullOrWhiteSpace(data))
        {
            return;
        }

        _loadedSavedInventory = true;

        foreach (InventorySlotData slot in _inventorySlots)
        {
            slot.Clear();
        }

        string[] savedSlots = data.Split(';');

        for (int i = 0; i < savedSlots.Length && i < _inventorySlots.Count; i++)
        {
            string savedSlot = savedSlots[i];

            if (string.IsNullOrWhiteSpace(savedSlot))
            {
                continue;
            }

            string[] parts = savedSlot.Split(':');

            if (parts.Length != 2)
            {
                continue;
            }

            if (!_itemsById.TryGetValue(parts[0], out ItemDefinition item))
            {
                continue;
            }

            if (!int.TryParse(parts[1], out int amount) || amount <= 0)
            {
                continue;
            }

            _inventorySlots[i].Set(item, Mathf.Min(amount, item.MaxStackSize));
        }
    }

    private string GetInventorySaveData()
    {
        StringBuilder builder = new();

        for (int i = 0; i < _inventorySlots.Count; i++)
        {
            InventorySlotData slot = _inventorySlots[i];

            if (!slot.IsEmpty && slot.Item != null && !string.IsNullOrEmpty(slot.Item.ItemId))
            {
                builder.Append(slot.Item.ItemId);
                builder.Append(':');
                builder.Append(slot.Amount);
            }

            if (i < _inventorySlots.Count - 1)
            {
                builder.Append(';');
            }
        }

        return builder.ToString();
    }

    private RecipeDefinition FindMatchingRecipe()
    {
        foreach (RecipeDefinition recipe in _recipes)
        {
            if (MatchesRecipe(recipe))
            {
                return recipe;
            }
        }

        return null;
    }

    private bool MatchesRecipe(RecipeDefinition recipe)
    {
        Dictionary<ItemDefinition, int> currentItems = new();

        foreach (InventorySlotData slot in _craftingSlots)
        {
            if (slot.IsEmpty)
            {
                continue;
            }

            if (!currentItems.ContainsKey(slot.Item))
            {
                currentItems.Add(slot.Item, 0);
            }

            currentItems[slot.Item] += slot.Amount;
        }

        Dictionary<ItemDefinition, int> requiredItems = new();

        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            if (!requiredItems.ContainsKey(ingredient.Item))
            {
                requiredItems.Add(ingredient.Item, 0);
            }

            requiredItems[ingredient.Item] += ingredient.Amount;
        }

        if (currentItems.Count != requiredItems.Count)
        {
            return false;
        }

        foreach (KeyValuePair<ItemDefinition, int> required in requiredItems)
        {
            if (!currentItems.TryGetValue(required.Key, out int currentAmount))
            {
                return false;
            }

            if (currentAmount < required.Value)
            {
                return false;
            }
        }

        return true;
    }

    private void ConsumeRecipe(RecipeDefinition recipe)
    {
        foreach (RecipeIngredient ingredient in recipe.Ingredients)
        {
            int remaining = ingredient.Amount;

            foreach (InventorySlotData slot in _craftingSlots)
            {
                if (slot.IsEmpty || slot.Item != ingredient.Item)
                {
                    continue;
                }

                int removeAmount = Mathf.Min(remaining, slot.Amount);
                slot.Remove(removeAmount);
                remaining -= removeAmount;

                if (remaining <= 0)
                {
                    break;
                }
            }
        }
    }
}
