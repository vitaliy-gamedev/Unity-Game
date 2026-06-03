using UnityEngine;

public class Portal : MonoBehaviour
{

    [SerializeField] private Transform _exitPoint;
        private void OnTriggerEnter2D(Collider2D collision)

    {

        if (collision.CompareTag("Player"))

        {

            collision.transform.position = _exitPoint.position;

        }

    }

}