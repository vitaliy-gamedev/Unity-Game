using System;
using UnityEngine;

[Serializable]
public class InventorySlotData
{
    [SerializeField] private ItemDefinition _item;
    [SerializeField] private int _amount;

    public ItemDefinition Item => _item;
    public int Amount => _amount;
    public bool IsEmpty => _item == null || _amount <= 0;

    public void Set(ItemDefinition item, int amount)
    {
        _item = item;
        _amount = amount;

        if (_amount <= 0)
        {
            Clear();
        }
    }

    public void Add(int amount)
    {
        _amount += amount;
    }

    public void Remove(int amount)
    {
        _amount -= amount;

        if (_amount <= 0)
        {
            Clear();
        }
    }

    public void Clear()
    {
        _item = null;
        _amount = 0;
    }
}