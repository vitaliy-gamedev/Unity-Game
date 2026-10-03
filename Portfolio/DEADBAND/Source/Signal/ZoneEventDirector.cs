using Deadband.Combat;
using Deadband.Squad;
using UnityEngine;

namespace Deadband.Signals
{
    [DisallowMultipleComponent]
    public sealed class ZoneEventDirector : MonoBehaviour
    {
        private enum ZoneEventType { PowerFailure, FalseContact, SignalSurge }

        [SerializeField] private SignalSystem signalSystem;
        [SerializeField] private Light[] affectedLights;
        [SerializeField, Min(0)] private int maximumEvents = 2;
        [SerializeField, Min(5f)] private float minimumInterval = 45f;
        [SerializeField, Min(1f)] private float eventDuration = 8f;

        private SquadUnit[] squad;
        private float[] normalLightIntensities;
        private int eventCount;
        private float nextAllowedTime;
        private float eventTimeRemaining;
        private float messageTimeRemaining;
        private string activeMessage;

        public int EventCount => eventCount;
        public int MaximumEvents => maximumEvents;

        public void Configure(SignalSystem signal, Light[] lights)
        {
            signalSystem = signal;
            affectedLights = lights;
        }

        private void Start()
        {
            squad = FindObjectsByType<SquadUnit>(FindObjectsSortMode.None);
            normalLightIntensities = new float[affectedLights?.Length ?? 0];
            for (int index = 0; index < normalLightIntensities.Length; index++)
            {
                normalLightIntensities[index] = affectedLights[index] != null ? affectedLights[index].intensity : 0f;
            }
            if (signalSystem != null) signalSystem.SignalLevelChanged += HandleSignalLevelChanged;
        }

        private void OnDestroy()
        {
            if (signalSystem != null) signalSystem.SignalLevelChanged -= HandleSignalLevelChanged;
            RestoreLights();
        }

        private void Update()
        {
            if (eventTimeRemaining <= 0f) return;
            eventTimeRemaining -= Time.deltaTime;
            messageTimeRemaining = Mathf.Max(0f, messageTimeRemaining - Time.unscaledDeltaTime);
            if (eventTimeRemaining <= 0f)
            {
                RestoreLights();
                activeMessage = string.Empty;
            }
        }

        private void HandleSignalLevelChanged(SignalLevel level)
        {
            if (eventCount >= maximumEvents || Time.time < nextAllowedTime || level < SignalLevel.Danger) return;
            int selection = level == SignalLevel.SignalCore ? 2 : Mathf.Abs((eventCount * 2 + (int)level)) % 3;
            TriggerEvent((ZoneEventType)selection);
        }

        private void TriggerEvent(ZoneEventType eventType)
        {
            eventCount++;
            nextAllowedTime = Time.time + minimumInterval;
            eventTimeRemaining = eventDuration;
            messageTimeRemaining = Mathf.Min(5f, eventDuration);
            switch (eventType)
            {
                case ZoneEventType.PowerFailure:
                    activeMessage = "ZONE EVENT  //  POWER FAILURE  //  LOCAL LIGHTING UNSTABLE";
                    SetLightMultiplier(0.12f);
                    break;
                case ZoneEventType.FalseContact:
                    activeMessage = "ZONE EVENT  //  FALSE CONTACT  //  HOSTILES INVESTIGATING SIGNAL ECHO";
                    if (TryGetSquadCenter(out Vector3 center)) CombatNoiseSystem.Emit(center + Vector3.right * 12f, 38f, gameObject);
                    break;
                default:
                    activeMessage = "ZONE EVENT  //  SIGNAL SURGE  //  EXPECT ELECTRONIC DISTORTION";
                    signalSystem?.TriggerSurge(0.75f);
                    break;
            }
        }

        private void SetLightMultiplier(float multiplier)
        {
            for (int index = 0; index < normalLightIntensities.Length; index++)
            {
                if (affectedLights[index] != null) affectedLights[index].intensity = normalLightIntensities[index] * multiplier;
            }
        }

        private void RestoreLights()
        {
            if (normalLightIntensities == null) return;
            for (int index = 0; index < normalLightIntensities.Length; index++)
            {
                if (affectedLights[index] != null) affectedLights[index].intensity = normalLightIntensities[index];
            }
        }

        private bool TryGetSquadCenter(out Vector3 center)
        {
            center = Vector3.zero;
            int count = 0;
            foreach (SquadUnit unit in squad)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;
                center += unit.Position;
                count++;
            }
            if (count == 0) return false;
            center /= count;
            return true;
        }

        private void OnGUI()
        {
            // Zone events communicate through lighting, audio and gameplay effects.
            // A large center-screen debug caption is deliberately not rendered.
        }
    }
}
