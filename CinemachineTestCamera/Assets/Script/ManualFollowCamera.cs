using UnityEngine;

namespace CameraDemo
{
    public class ManualFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, -4f);
        [SerializeField] private float smoothTime = 0.2f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref velocity, smoothTime);

            Vector3 lookDirection = target.position - transform.position;
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }
}
