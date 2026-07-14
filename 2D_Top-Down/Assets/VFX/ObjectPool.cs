using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private readonly GameObject _prefab;
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private readonly Transform _parent;

    public ObjectPool(GameObject prefab, int initialSize = 10, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNewObject();
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    public GameObject Get()
    {
        if (_pool.Count == 0)
        {
            GameObject newObj = CreateNewObject();
            newObj.SetActive(true);
            return newObj;
        }

        GameObject obj = _pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(false);
        _pool.Enqueue(obj);
    }

    private GameObject CreateNewObject()
    {
        if (_prefab == null)
        {
            Debug.LogError("[ObjectPool] Prefab is not assigned!");
            return new GameObject("PoolError");
        }

        GameObject obj = Object.Instantiate(_prefab, _parent);
        obj.name = $"{_prefab.name}_Pooled";
        return obj;
    }

    public void Clear()
    {
        while (_pool.Count > 0)
        {
            GameObject obj = _pool.Dequeue();
            Object.Destroy(obj);
        }
    }
}
