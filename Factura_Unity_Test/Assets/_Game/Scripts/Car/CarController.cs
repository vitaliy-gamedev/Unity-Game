// Автоматичний рух кінематичного авто та прогрес кінцевого маршруту.
using System;
using UnityEngine;

namespace Factura
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class CarController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 4f;
        [SerializeField, Min(1f)] private float routeLength = 100f;
        [SerializeField, Min(0.01f)] private float wheelRadius = 0.36f;
        [SerializeField] private Transform[] wheels = Array.Empty<Transform>();
        private Rigidbody body;
        private Vector3 startPosition;
        private Quaternion[] initialWheelRotations;
        private bool isRunning;
        private bool hasReachedFinish;
        private float visualDistance;

        public event Action DestinationReached;
        public event Action<float> ProgressChanged;
        public float Progress { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            startPosition = body.position;
            initialWheelRotations = new Quaternion[wheels.Length];
            for (int i = 0; i < wheels.Length; i++) initialWheelRotations[i] = wheels[i].localRotation;
        }

        public void SetRunning(bool value) => isRunning = value && !hasReachedFinish;

        public void ResetToStart()
        {
            isRunning = false;
            hasReachedFinish = false;
            body.position = startPosition;
            transform.position = startPosition;
            visualDistance = 0;
            Progress = 0;
            for (int i = 0; i < wheels.Length; i++) wheels[i].localRotation = initialWheelRotations[i];
            ProgressChanged?.Invoke(Progress);
        }

        private void FixedUpdate()
        {
            if (!isRunning) return;
            float distance = Mathf.Min(routeLength, body.position.z - startPosition.z + speed * Time.fixedDeltaTime);
            body.MovePosition(startPosition + Vector3.forward * distance);
            Progress = distance / routeLength;
            ProgressChanged?.Invoke(Progress);
            if (distance < routeLength) return;
            hasReachedFinish = true;
            isRunning = false;
            DestinationReached?.Invoke();
        }

        private void LateUpdate()
        {
            float distance = transform.position.z - startPosition.z;
            float degrees = (distance - visualDistance) / wheelRadius * Mathf.Rad2Deg;
            visualDistance = distance;
            foreach (var wheel in wheels) wheel.Rotate(transform.right, degrees, Space.World);
        }
    }
}
