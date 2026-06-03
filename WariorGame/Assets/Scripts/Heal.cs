using UnityEngine;

public class Heal : MonoBehaviour
{
    [SerializeField] float heal;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                health.AddHealth(heal);
            }

            Destroy(gameObject);
        }
    }
}
