using UnityEngine;

public class GroundSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject magnetPrefab;

    [Header("Player")]
    [SerializeField] private Transform player;

    private float spawnZ = 0f;
    private float roadLength = 10f;

    private readonly float[] lanes = { -3f, 0f, 3f };

    private void Start()
    {
        for (int i = 0; i < 15; i++)
            SpawnRoad(i < 3);
    }

    private void Update()
    {
        if (player.position.z + 60f > spawnZ)
            SpawnRoad(false);
    }

    private void SpawnRoad(bool empty)
    {
        GameObject road = Instantiate(roadPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity);
        Destroy(road, 30f);

        if (!empty)
            SpawnObjects();

        spawnZ += roadLength;
    }

    private void SpawnObjects()
    {
        float chance = Random.value;
        int lane = Random.Range(0, 3);

        Vector3 basePos = new Vector3(lanes[lane], 0.5f, spawnZ + 5f);

        if (chance < 0.4f)
        {
            Spawn(obstaclePrefab, basePos);
        }
        else if (chance < 0.85f)
        {
            Spawn(coinPrefab, basePos + Vector3.forward * -2f);
            Spawn(coinPrefab, basePos + Vector3.forward * 2f);
        }
        else
        {
            Spawn(magnetPrefab, basePos);
        }
    }

    private void Spawn(GameObject obj, Vector3 pos)
    {
        GameObject go = Instantiate(obj, pos, Quaternion.identity);
        Destroy(go, 30f);
    }
}