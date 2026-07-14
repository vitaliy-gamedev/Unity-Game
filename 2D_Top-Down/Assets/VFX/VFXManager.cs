using System.Collections;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject _itemPickupVFX;
    [SerializeField] private GameObject _enemyDeathVFX;
    [SerializeField] private GameObject _levelCompleteVFX;
    [SerializeField] private GameObject _playerHitVFX;

    [Header("Pool Settings")]
    [SerializeField] private int _poolSize = 10;

    private ObjectPool _itemPickupPool;
    private ObjectPool _enemyDeathPool;
    private ObjectPool _levelCompletePool;
    private ObjectPool _playerHitPool;

    private static VFXManager _instance;

    public static VFXManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        _itemPickupPool = new ObjectPool(_itemPickupVFX, _poolSize, transform);
        _enemyDeathPool = new ObjectPool(_enemyDeathVFX, _poolSize, transform);
        _levelCompletePool = new ObjectPool(_levelCompleteVFX, _poolSize, transform);
        _playerHitPool = new ObjectPool(_playerHitVFX, _poolSize, transform);
    }

    public void PlayItemPickup(Vector3 position)
    {
        PlayEffect(_itemPickupPool, position);
    }

    public void PlayEnemyDeath(Vector3 position)
    {
        PlayEffect(_enemyDeathPool, position);
    }

    public void PlayLevelComplete()
    {
        if (_levelCompletePool == null) return;
        GameObject effect = _levelCompletePool.Get();
        effect.transform.position = Vector3.zero;
        StartCoroutine(ReturnAfterDelay(_levelCompletePool, effect, 2f));
    }

    public void PlayPlayerHit(Vector3 position)
    {
        PlayEffect(_playerHitPool, position);
    }

    private void PlayEffect(ObjectPool pool, Vector3 position)
    {
        if (pool == null) return;

        GameObject effect = pool.Get();
        effect.transform.position = position;

        StartCoroutine(ReturnAfterDelay(pool, effect, 1f));
    }

    private IEnumerator ReturnAfterDelay(ObjectPool pool, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (pool != null && obj != null)
            pool.Return(obj);
    }
}
