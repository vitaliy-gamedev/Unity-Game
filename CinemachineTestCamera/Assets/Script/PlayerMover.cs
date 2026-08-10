using UnityEngine;

namespace CameraDemo
{
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;

        private void Update()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.position += direction * (moveSpeed * Time.deltaTime);

                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
