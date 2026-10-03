using System;
using Deadband.Combat;
using Deadband.AudioSystem;
using Deadband.Signals;
using Deadband.Squad;
using UnityEngine;

namespace Deadband.AI
{
    public enum SecurityState
    {
        Scanning,
        Tracking,
        Firing,
        Disabled
    }

    [DefaultExecutionOrder(-390)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class SecurityTurretController : MonoBehaviour
    {
        [SerializeField] private SignalSystem signalSystem;
        [SerializeField] private Transform sensorPivot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask visibilityMask = ~0;
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private Light muzzleFlash;
        [SerializeField, Min(1f)] private float baseDetectionRange = 13f;
        [SerializeField, Range(10f, 180f)] private float detectionAngle = 78f;
        [SerializeField, Min(1f)] private float scanSpeed = 52f;
        [SerializeField, Min(0.05f)] private float lockDuration = 0.45f;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.62f;
        [SerializeField, Min(0.1f)] private float damage = 6f;

        private Health health;
        private SquadUnit[] squadUnits;
        private SquadUnit target;
        private float lockProgress;
        private float nextFireTime;
        private float effectFinishTime;
        private WeaponAudioEmitter weaponAudio;

        public SecurityState State { get; private set; } = SecurityState.Scanning;

        public void Configure(
            SignalSystem signal,
            Transform pivot,
            Transform muzzleTransform,
            LayerMask lineOfSightMask,
            LineRenderer tracerRenderer,
            Light flash)
        {
            signalSystem = signal;
            sensorPivot = pivot;
            muzzle = muzzleTransform;
            visibilityMask = lineOfSightMask;
            tracer = tracerRenderer;
            muzzleFlash = flash;
        }

        private void Awake()
        {
            weaponAudio = GetComponent<WeaponAudioEmitter>();
            health = GetComponent<Health>();
            health.Died += HandleDeath;
            squadUnits = FindObjectsByType<SquadUnit>(FindObjectsSortMode.None);
            SetEffects(false);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        private void Update()
        {
            UpdateEffects();
            if (State == SecurityState.Disabled || sensorPivot == null)
            {
                return;
            }

            float signalStrength = signalSystem != null ? signalSystem.Strength : 0f;
            float detectionRange = baseDetectionRange * (1f + signalStrength * 0.65f);
            if (!TryGetVisibleTarget(detectionRange, out SquadUnit visibleTarget))
            {
                target = null;
                lockProgress = 0f;
                State = SecurityState.Scanning;
                Scan(signalStrength);
                return;
            }

            target = visibleTarget;
            TrackTarget();
            lockProgress += Time.deltaTime * (1f + signalStrength * 0.4f);
            State = lockProgress >= lockDuration ? SecurityState.Firing : SecurityState.Tracking;
            if (State == SecurityState.Firing && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireInterval * (1f - signalStrength * 0.18f);
                TryFire();
            }
        }

        private void Scan(float signalStrength)
        {
            float phase = Time.time * scanSpeed * (1f + signalStrength * 0.3f) * Mathf.Deg2Rad;
            float yaw = Mathf.Sin(phase) * 72f;
            sensorPivot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void TrackTarget()
        {
            Vector3 direction = target.transform.position + Vector3.up * 1.1f - sensorPivot.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                sensorPivot.rotation = Quaternion.RotateTowards(
                    sensorPivot.rotation,
                    Quaternion.LookRotation(direction.normalized, Vector3.up),
                    240f * Time.deltaTime);
            }
        }

        private bool TryGetVisibleTarget(float range, out SquadUnit visibleTarget)
        {
            visibleTarget = null;
            float closest = float.PositiveInfinity;
            foreach (SquadUnit unit in squadUnits)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Health unitHealth = unit.GetComponent<Health>();
                if (unitHealth != null && !unitHealth.IsAlive)
                {
                    continue;
                }

                Vector3 direction = unit.transform.position + Vector3.up * 1.1f - sensorPivot.position;
                float distance = direction.magnitude;
                if (distance >= closest || distance > range || distance < 0.01f ||
                    Vector3.Angle(sensorPivot.forward, direction) > detectionAngle * 0.5f)
                {
                    continue;
                }

                if (!HasLineOfSight(sensorPivot.position, unit, direction / distance, distance))
                {
                    continue;
                }

                closest = distance;
                visibleTarget = unit;
            }
            return visibleTarget != null;
        }

        private bool HasLineOfSight(
            Vector3 origin,
            SquadUnit expectedTarget,
            Vector3 direction,
            float distance)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                distance + 0.3f,
                visibilityMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<SecurityTurretController>() == this)
                {
                    continue;
                }
                return hit.collider.GetComponentInParent<SquadUnit>() == expectedTarget;
            }
            return false;
        }

        private void TryFire()
        {
            if (target == null || muzzle == null)
            {
                return;
            }

            Vector3 targetPoint = target.transform.position + Vector3.up * 1.1f;
            Vector3 direction = targetPoint - muzzle.position;
            float distance = direction.magnitude;
            if (distance < 0.01f)
            {
                return;
            }
            direction /= distance;
            if (!HasLineOfSight(muzzle.position, target, direction, distance))
            {
                return;
            }

            target.GetComponent<Health>()?.ApplyDamage(damage, targetPoint, direction);
            weaponAudio?.PlayShot(false);
            if (tracer != null)
            {
                tracer.SetPosition(0, muzzle.position);
                tracer.SetPosition(1, targetPoint);
            }
            SetEffects(true);
            effectFinishTime = Time.time + 0.055f;
        }

        private void HandleDeath()
        {
            State = SecurityState.Disabled;
            SetEffects(false);
        }

        private void UpdateEffects()
        {
            if (effectFinishTime > 0f && Time.time >= effectFinishTime)
            {
                effectFinishTime = 0f;
                SetEffects(false);
            }
        }

        private void SetEffects(bool active)
        {
            if (tracer != null)
            {
                tracer.enabled = active;
            }
            if (muzzleFlash != null)
            {
                muzzleFlash.enabled = active;
            }
        }

        private void OnGUI()
        {
            // Runtime debug state labels are disabled for the release HUD.
        }
    }
}
