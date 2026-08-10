using UnityEngine;

namespace CameraDemo
{
    public class ManualOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 6f;
        [SerializeField] private float sensitivity = 0.5f;
        [SerializeField] private float zoomSensitivity = 3f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float minPitch = -60f;
        [SerializeField] private float maxPitch = 60f;

        private float yaw;
        private float pitch;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            if (Input.GetMouseButton(0))
            {
                yaw += Input.GetAxisRaw("Mouse X") * sensitivity;
                pitch -= Input.GetAxisRaw("Mouse Y") * sensitivity;
            }

            distance -= Input.GetAxisRaw("Mouse ScrollWheel") * zoomSensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rotation * Vector3.back * distance;

            transform.position = target.position + offset;
            transform.rotation = rotation;
        }
    }
}
