using UnityEngine;

namespace Deadband.Signals
{
    [CreateAssetMenu(menuName = "DEADBAND/Signal/Signal Config", fileName = "SC_DefaultSignal")]
    public sealed class SignalConfig : ScriptableObject
    {
        [SerializeField, Min(1f)] private float detectionRadius = 27f;
        [SerializeField, Min(0.1f)] private float coreRadius = 2.2f;
        [SerializeField] private AnimationCurve strengthByProximity = new(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0.34f),
            new Keyframe(1f, 1f));
        [SerializeField, Range(0f, 1f)] private float interferenceThreshold = 0.25f;
        [SerializeField, Range(0f, 1f)] private float dangerThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float highRiskThreshold = 0.75f;
        [SerializeField, Range(0f, 1f)] private float coreThreshold = 0.9f;

        public float DetectionRadius => detectionRadius;
        public float CoreRadius => coreRadius;

        public float EvaluateStrength(float distance)
        {
            float proximity = 1f - Mathf.InverseLerp(coreRadius, detectionRadius, distance);
            return Mathf.Clamp01(strengthByProximity.Evaluate(proximity));
        }

        public SignalLevel EvaluateLevel(float strength)
        {
            if (strength >= coreThreshold)
            {
                return SignalLevel.SignalCore;
            }
            if (strength >= highRiskThreshold)
            {
                return SignalLevel.HighRisk;
            }
            if (strength >= dangerThreshold)
            {
                return SignalLevel.Danger;
            }
            if (strength >= interferenceThreshold)
            {
                return SignalLevel.Interference;
            }
            return SignalLevel.Normal;
        }

        public void Configure(float radius, float sourceCoreRadius)
        {
            detectionRadius = Mathf.Max(1f, radius);
            coreRadius = Mathf.Clamp(sourceCoreRadius, 0.1f, detectionRadius - 0.1f);
            strengthByProximity = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 0.34f),
                new Keyframe(1f, 1f));
            interferenceThreshold = 0.25f;
            dangerThreshold = 0.5f;
            highRiskThreshold = 0.75f;
            coreThreshold = 0.9f;
        }
    }
}
