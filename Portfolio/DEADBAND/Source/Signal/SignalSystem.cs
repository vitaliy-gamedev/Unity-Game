using System;
using UnityEngine;

namespace Deadband.Signals
{
    [DefaultExecutionOrder(-550)]
    [DisallowMultipleComponent]
    public sealed class SignalSystem : MonoBehaviour
    {
        [SerializeField] private SignalConfig config;
        [SerializeField] private SignalSource source;
        [SerializeField] private SignalReceiver receiver;
        [SerializeField, Min(0.02f)] private float sampleInterval = 0.1f;

        private float nextSampleTime;
        private bool signalDetected;
        private bool sourceReached;

        public event Action<float> SignalStrengthChanged;
        public event Action<SignalLevel> SignalLevelChanged;
        public event Action SignalDetected;
        public event Action SignalLost;
        public event Action SignalSourceReached;
        public event Action<float> SignalSurge;

        public SignalConfig Config => config;
        public float Strength { get; private set; }
        public SignalLevel Level { get; private set; } = SignalLevel.Normal;
        public float DistanceToSource { get; private set; }
        public int ReceiverUpgradeLevel => receiver != null ? receiver.UpgradeLevel : 1;
        public Vector3 DirectionToSource => source != null && receiver != null
            ? (source.Position - receiver.Position).normalized
            : Vector3.zero;

        public void Configure(SignalConfig signalConfig, SignalSource signalSource, SignalReceiver signalReceiver)
        {
            config = signalConfig;
            source = signalSource;
            receiver = signalReceiver;
        }

        public float EvaluateStrengthAtPosition(Vector3 worldPosition)
        {
            if (config == null || source == null)
            {
                return 0f;
            }

            float distance = Vector3.Distance(worldPosition, source.Position);
            return config.EvaluateStrength(distance);
        }

        public void TriggerSurge(float intensity)
        {
            SignalSurge?.Invoke(Mathf.Clamp01(intensity));
        }

        private void Start()
        {
            SampleSignal(true);
        }

        private void Update()
        {
            if (Time.time < nextSampleTime)
            {
                return;
            }

            nextSampleTime = Time.time + sampleInterval;
            SampleSignal(false);
        }

        private void SampleSignal(bool forceNotification)
        {
            if (config == null || source == null || receiver == null)
            {
                return;
            }

            DistanceToSource = Vector3.Distance(receiver.Position, source.Position);
            float newStrength = config.EvaluateStrength(DistanceToSource);
            SignalLevel newLevel = config.EvaluateLevel(newStrength);

            if (forceNotification || Mathf.Abs(newStrength - Strength) >= 0.002f)
            {
                Strength = newStrength;
                SignalStrengthChanged?.Invoke(Strength);
            }

            if (forceNotification || newLevel != Level)
            {
                Level = newLevel;
                SignalLevelChanged?.Invoke(Level);
            }

            bool isDetectedNow = Strength > 0.01f;
            if (isDetectedNow != signalDetected)
            {
                signalDetected = isDetectedNow;
                if (signalDetected)
                {
                    SignalDetected?.Invoke();
                }
                else
                {
                    SignalLost?.Invoke();
                }
            }

            if (!sourceReached && DistanceToSource <= config.CoreRadius)
            {
                sourceReached = true;
                SignalSourceReached?.Invoke();
            }
        }
    }
}
