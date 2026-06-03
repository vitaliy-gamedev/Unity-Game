using UnityEngine;

public class Magnet : MonoBehaviour
{
    [SerializeField] private float duration = 7f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CoinMagnetManager.Instance.ActivateMagnet(duration);
            Destroy(gameObject);
        }
    }
}