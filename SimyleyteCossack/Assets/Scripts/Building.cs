using UnityEngine;

public class Building : MonoBehaviour
{
    [SerializeField] private GameObject _unitPrefab;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private int _spawnCount = 3;

    private int _storedResources;
    public int StoredResources => _storedResources;

    private void OnEnable() => Unit.AllBuildings.Add(this);
    private void OnDisable() => Unit.AllBuildings.Remove(this);

    private void Awake()
    {
        if (_spawnPoint == null)
            _spawnPoint = transform;
    }

    public void SpawnUnits()
    {
        if (_unitPrefab == null)
        {
            Debug.LogWarning($"[{name}] Unit Prefab не призначено!", this);
            return;
        }

        for (var i = 0; i < _spawnCount; i++)
        {
            var offset = Random.insideUnitSphere * 2f;
            offset.y = 0;
            Vector3 spawnPos = _spawnPoint.position + offset;

            GameObject newUnitObj = Instantiate(_unitPrefab, spawnPos, Quaternion.identity);
            Unit unitScript = newUnitObj.GetComponent<Unit>();

            if (unitScript != null)
            {
                // КРИТИЧНО: Прив'язуємо юніта до цієї конкретної будівлі
                unitScript.SetHomeBuilding(this);

                // Відправляємо його трохи вбік, щоб не було купи-мали
                Vector3 moveAwayPos = spawnPos + offset.normalized * 2f;
                unitScript.MoveToCommand(moveAwayPos);
            }
        }
    }

    public void Deposit(int amount)
    {
        _storedResources += amount;
    }
}