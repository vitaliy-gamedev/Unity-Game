using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs; // масив замість одного
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private int enemiesPerWave = 5;
    [SerializeField] private float timeBetweenEnemies = 0.8f;
    [SerializeField] private float timeBetweenWaves = 3f;

    private int currentWave = 0;

    private void Start()
    {
        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        while (true)
        {
            currentWave++;
            Debug.Log($"Хвиля {currentWave}");

            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(timeBetweenEnemies);
            }

            enemiesPerWave += 2;
            timeBetweenEnemies = Mathf.Max(0.2f, timeBetweenEnemies - 0.05f);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void SpawnEnemy()
    {
        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        GameObject enemy = Instantiate(enemyPrefabs[randomIndex], waypoints[0].position, Quaternion.identity);

        EnemyMove move = enemy.GetComponent<EnemyMove>();
        if (move != null)
            move.Initialize(waypoints);
    }
}