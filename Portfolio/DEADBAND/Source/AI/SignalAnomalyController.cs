using Deadband.Combat;
using Deadband.Signals;
using Deadband.Squad;
using UnityEngine;

namespace Deadband.AI
{
    [DisallowMultipleComponent]
    public sealed class SignalAnomalyController : MonoBehaviour
    {
        private enum AnomalyState { Dormant, Watching, Charging, Cooldown, Disabled }

        [SerializeField] private SignalSystem signalSystem;
        [SerializeField] private Health health;
        [SerializeField] private Transform rotatingCore;
        [SerializeField] private Transform pulseRing;
        [SerializeField] private Light anomalyLight;
        [SerializeField, Range(0f, 1f)] private float activationSignal = 0.48f;
        [SerializeField, Min(2f)] private float awarenessRadius = 15f;
        [SerializeField, Min(1f)] private float pulseRadius = 9f;
        [SerializeField, Min(0.25f)] private float telegraphDuration = 2.2f;
        [SerializeField, Min(1f)] private float cooldownDuration = 8f;
        [SerializeField, Min(1f)] private float pulseDamage = 22f;

        private SquadUnit[] squad;
        private AnomalyState state;
        private float stateTimer;
        private Vector3 baseRingScale;

        public bool IsDisabled => state == AnomalyState.Disabled;
        public bool IsCharging => state == AnomalyState.Charging;
        public float PulseRadius => pulseRadius;

        public void Configure(SignalSystem signal, Health healthReference, Transform core, Transform ring, Light lightReference)
        {
            signalSystem = signal;
            health = healthReference;
            rotatingCore = core;
            pulseRing = ring;
            anomalyLight = lightReference;
        }

        private void Start()
        {
            squad = FindObjectsByType<SquadUnit>(FindObjectsSortMode.None);
            baseRingScale = pulseRing != null ? pulseRing.localScale : Vector3.one;
            if (health != null) health.Died += DisableAnomaly;
            SetState(AnomalyState.Dormant);
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= DisableAnomaly;
        }

        private void Update()
        {
            if (state == AnomalyState.Disabled) return;

            float strength = signalSystem != null ? signalSystem.EvaluateStrengthAtPosition(transform.position) : 1f;
            rotatingCore?.Rotate(Vector3.up, (55f + strength * 140f) * Time.deltaTime, Space.World);
            if (strength < activationSignal)
            {
                SetState(AnomalyState.Dormant);
                return;
            }

            switch (state)
            {
                case AnomalyState.Dormant:
                    SetState(AnomalyState.Watching);
                    break;
                case AnomalyState.Watching:
                    if (IsLivingSquadWithin(awarenessRadius)) SetState(AnomalyState.Charging);
                    break;
                case AnomalyState.Charging:
                    stateTimer -= Time.deltaTime;
                    UpdateChargeVisuals(1f - Mathf.Clamp01(stateTimer / telegraphDuration));
                    if (stateTimer <= 0f)
                    {
                        EmitPulse();
                        SetState(AnomalyState.Cooldown);
                    }
                    break;
                case AnomalyState.Cooldown:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        SetState(IsLivingSquadWithin(awarenessRadius) ? AnomalyState.Charging : AnomalyState.Watching);
                    }
                    break;
            }
        }

        private void SetState(AnomalyState next)
        {
            if (state == next && next != AnomalyState.Dormant) return;
            state = next;
            stateTimer = next == AnomalyState.Charging ? telegraphDuration : next == AnomalyState.Cooldown ? cooldownDuration : 0f;
            if (pulseRing != null)
            {
                pulseRing.gameObject.SetActive(next is AnomalyState.Charging or AnomalyState.Cooldown);
                pulseRing.localScale = baseRingScale;
            }
            if (anomalyLight != null)
            {
                anomalyLight.intensity = next switch
                {
                    AnomalyState.Charging => 5.5f,
                    AnomalyState.Cooldown => 1.2f,
                    AnomalyState.Watching => 2.4f,
                    _ => 0.35f
                };
            }
        }

        private void UpdateChargeVisuals(float progress)
        {
            if (pulseRing != null)
            {
                float diameter = Mathf.Lerp(0.2f, pulseRadius * 2f, progress);
                pulseRing.localScale = new Vector3(diameter, baseRingScale.y, diameter);
            }
            if (anomalyLight != null) anomalyLight.intensity = Mathf.Lerp(2.5f, 8f, progress);
        }

        private void EmitPulse()
        {
            foreach (SquadUnit unit in squad)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;
                Health targetHealth = unit.GetComponent<Health>();
                Vector3 offset = unit.Position - transform.position;
                offset.y = 0f;
                if (targetHealth != null && targetHealth.IsAlive && offset.magnitude <= pulseRadius)
                {
                    targetHealth.ApplyDamage(pulseDamage, unit.Position, offset.normalized);
                }
            }
            CombatNoiseSystem.Emit(transform.position, 30f, gameObject);
            signalSystem?.TriggerSurge(1f);
        }

        private bool IsLivingSquadWithin(float radius)
        {
            float radiusSquared = radius * radius;
            foreach (SquadUnit unit in squad)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;
                Health targetHealth = unit.GetComponent<Health>();
                Vector3 offset = unit.Position - transform.position;
                offset.y = 0f;
                if ((targetHealth == null || targetHealth.IsAlive) && offset.sqrMagnitude <= radiusSquared) return true;
            }
            return false;
        }

        private void DisableAnomaly()
        {
            state = AnomalyState.Disabled;
            if (pulseRing != null) pulseRing.gameObject.SetActive(false);
            if (anomalyLight != null) anomalyLight.enabled = false;
        }

        private void OnGUI()
        {
            // The expanding pulse ring is the telegraph; no center-screen debug text.
        }
    }
}
