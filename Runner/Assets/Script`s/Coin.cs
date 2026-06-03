using UnityEngine;

public class Coin : MonoBehaviour
{
    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        transform.Rotate(0, 200 * Time.deltaTime, 0);

        if (CoinMagnetManager.Instance != null &&
            CoinMagnetManager.Instance.IsMagnetActive())
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position + Vector3.up,
                12f * Time.deltaTime
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.AddCoin(1);
            Destroy(gameObject);
        }
    }
}