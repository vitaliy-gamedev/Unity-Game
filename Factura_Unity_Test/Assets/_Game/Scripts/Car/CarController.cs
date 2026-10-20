using System;
using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Автоматичний рух авто вздовж прямої траси та повідомлення про прогрес/фініш.
    /// Кінематичний Rigidbody рухається у FixedUpdate, а колеса — лише візуально в LateUpdate.
    /// WheelCollider не потрібен: у цьому прототипі немає симуляції підвіски чи ручного водіння.
    /// </summary>
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
            // Кешуємо компонент і початкові пози/оберти один раз, а не шукаємо при кожному рестарті.
            body = GetComponent<Rigidbody>();
            startPosition = body.position;
            initialWheelRotations = new Quaternion[wheels.Length];
            for (int i = 0; i < wheels.Length; i++) initialWheelRotations[i] = wheels[i].localRotation;
        }

        // Повторний дозвіл руху після фінішу не запускає авто до явного ResetToStart.
        public void SetRunning(bool value) => isRunning = value && !hasReachedFinish;

        public void ResetToStart()
        {
            // Рестарт — миттєва телепортація, тому тут задаємо позицію напряму,
            // а не MovePosition, призначений нижче для звичайного фізичного кроку.
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
            // Шлях обмежений routeLength: авто не проїжджає за фініш на зайвий крок.
            // fixedDeltaTime задає метри за фізичний крок, а не за довільний кадр рендеру.
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
            // Оберт залежить від фактичного видимого переміщення, а не просто часу:
            // коли авто зупинено, колеса теж стоять. s / r дає кут у радіанах.
            float distance = transform.position.z - startPosition.z;
            float degrees = (distance - visualDistance) / wheelRadius * Mathf.Rad2Deg;
            visualDistance = distance;
            foreach (var wheel in wheels) wheel.Rotate(transform.right, degrees, Space.World);
        }
    }
}
