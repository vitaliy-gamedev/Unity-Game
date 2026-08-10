using UnityEngine;

namespace CameraDemo
{
    public class TargetMover : MonoBehaviour
    {
        [SerializeField] private float radius = 4f;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float height = 1.5f;

        private Vector3 center;
        private float angle;

        private void Start()
        {
            center = transform.position;
        }

        private void Update()
        {
            angle += speed * Time.deltaTime;

            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;
            float y = center.y + Mathf.Sin(angle * 2f) * height;

            transform.position = new Vector3(x, y, z);
        }
    }
}
