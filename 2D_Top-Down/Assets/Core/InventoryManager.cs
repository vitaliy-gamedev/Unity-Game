using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Debug")] [SerializeField] private bool _clearOnStart = true;

    private readonly HashSet<string> _keys = new HashSet<string>();
    private static InventoryManager _instance;

    public static InventoryManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_clearOnStart)
            _keys.Clear();
    }

    public void AddKey(string keyId)
    {
        if (!string.IsNullOrEmpty(keyId))
            _keys.Add(keyId);
    }

    public bool HasKey(string keyId)
    {
        return !string.IsNullOrEmpty(keyId) && _keys.Contains(keyId);
    }

    public void RemoveKey(string keyId)
    {
        if (!string.IsNullOrEmpty(keyId))
            _keys.Remove(keyId);
    }

    public void ClearAll()
    {
        _keys.Clear();
    }
}