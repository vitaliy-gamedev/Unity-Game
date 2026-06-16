using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceType { Gold, Wood, Stone, Food }

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Serializable]
    public struct StartingResource
    {
        public ResourceType type;
        public int amount;
    }

    [Tooltip("Скільки ресурсів є на старті гри")]
    [SerializeField] private List<StartingResource> startingResources = new();

    private readonly Dictionary<ResourceType, int> _amounts = new();
    
    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
            _amounts[t] = 0;

        foreach (var s in startingResources)
            _amounts[s.type] = s.amount;
    }

    public int Get(ResourceType type) => _amounts.TryGetValue(type, out var v) ? v : 0;

    public bool CanAfford(ResourceType type, int cost) => Get(type) >= cost;

    public void Add(ResourceType type, int amount)
    {
        _amounts[type] = Get(type) + amount;
        OnChanged?.Invoke();
    }

    public bool TrySpend(ResourceType type, int amount)
    {
        if (!CanAfford(type, amount)) return false;
        _amounts[type] = Get(type) - amount;
        OnChanged?.Invoke();
        return true;
    }
}
