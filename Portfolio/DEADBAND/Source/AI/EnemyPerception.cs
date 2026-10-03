using System;
using Deadband.Combat;
using Deadband.Squad;
using UnityEngine;

namespace Deadband.AI
{
    [DefaultExecutionOrder(-520)]
    [DisallowMultipleComponent]
    public sealed class EnemyPerception : MonoBehaviour
    {
        [SerializeField] private Transform eyePoint;
        [SerializeField] private LayerMask visibilityMask = ~0;

        private SquadUnit[] squadUnits;
        private CombatNoiseEvent pendingNoise;
        private float pendingNoiseUntil;

        public string LastStimulus { get; private set; } = "NONE";

        public void Configure(Transform eyes, LayerMask lineOfSightMask)
        {
            eyePoint = eyes;
            visibilityMask = lineOfSightMask;
        }

        private void Awake()
        {
            squadUnits = FindObjectsByType<SquadUnit>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            CombatNoiseSystem.NoiseEmitted += HandleNoise;
        }

        private void OnDisable()
        {
            CombatNoiseSystem.NoiseEmitted -= HandleNoise;
        }

        public bool TryGetVisibleTarget(float range, float angle, out SquadUnit visibleTarget)
        {
            visibleTarget = null;
            if (eyePoint == null || squadUnits == null)
            {
                return false;
            }

            float bestDistance = float.PositiveInfinity;
            foreach (SquadUnit unit in squadUnits)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Health health = unit.GetComponent<Health>();
                if (health != null && !health.IsAlive)
                {
                    continue;
                }

                Vector3 targetPoint = unit.transform.position + Vector3.up * 1.15f;
                Vector3 direction = targetPoint - eyePoint.position;
                float distance = direction.magnitude;
                if (distance > range || distance >= bestDistance || distance < 0.01f ||
                    Vector3.Angle(transform.forward, direction) > angle * 0.5f)
                {
                    continue;
                }

                if (!HasLineOfSight(unit, direction / distance, distance))
                {
                    continue;
                }

                bestDistance = distance;
                visibleTarget = unit;
            }

            if (visibleTarget != null)
            {
                LastStimulus = "VISUAL";
                return true;
            }
            return false;
        }

        public bool TryConsumeNoise(float hearingMultiplier, out Vector3 position)
        {
            position = Vector3.zero;
            if (Time.time > pendingNoiseUntil)
            {
                return false;
            }

            float audibleRadius = pendingNoise.Radius * hearingMultiplier;
            if (Vector3.Distance(transform.position, pendingNoise.Position) > audibleRadius)
            {
                return false;
            }

            position = pendingNoise.Position;
            pendingNoiseUntil = 0f;
            LastStimulus = "SOUND";
            return true;
        }

        private bool HasLineOfSight(SquadUnit expectedTarget, Vector3 direction, float distance)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                eyePoint.position,
                direction,
                distance + 0.25f,
                visibilityMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<HumanoidEnemyController>() != null)
                {
                    continue;
                }

                return hit.collider.GetComponentInParent<SquadUnit>() == expectedTarget;
            }
            return false;
        }

        private void HandleNoise(CombatNoiseEvent noise)
        {
            if (noise.Source != null && noise.Source.GetComponentInParent<HumanoidEnemyController>() != null)
            {
                return;
            }

            pendingNoise = noise;
            pendingNoiseUntil = Time.time + 1.25f;
        }
    }
}
