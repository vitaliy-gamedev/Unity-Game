using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Deadband.Squad
{
    [DefaultExecutionOrder(-700)]
    [DisallowMultipleComponent]
    public sealed class CompanionSquadAI : MonoBehaviour
    {
        [SerializeField] private PlayerUnitController controlledUnit;
        [SerializeField, Min(1f)] private float leaderClearance = 4.2f;
        [SerializeField, Min(1f)] private float rowSpacing = 2.2f;
        [SerializeField, Min(1f)] private float lateralSpacing = 3.1f;
        [SerializeField, Min(0.05f)] private float repathInterval = 0.12f;
        [SerializeField, Min(0.1f)] private float slotArrivalDistance = 0.4f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 1.5f;
        [SerializeField, Min(0f)] private float movementPrediction = 0.22f;

        private readonly List<SquadUnit> companions = new();
        private float nextRepathTime;

        public bool IsHolding { get; private set; }

        public void Configure(PlayerUnitController leaderController)
        {
            controlledUnit = leaderController;
        }

        private void Awake()
        {
            if (controlledUnit == null)
            {
                controlledUnit = FindFirstObjectByType<PlayerUnitController>();
            }

            CacheCompanions();
        }

        private void Update()
        {
            if (controlledUnit == null || IsHolding || Time.time < nextRepathTime)
            {
                return;
            }

            nextRepathTime = Time.time + repathInterval;
            UpdateFormationDestinations();
        }

        public void SetHold(bool shouldHold)
        {
            IsHolding = shouldHold;
            if (!IsHolding)
            {
                nextRepathTime = 0f;
                return;
            }

            foreach (SquadUnit companion in companions)
            {
                companion?.Stop();
            }
        }

        public void Regroup()
        {
            IsHolding = false;
            nextRepathTime = 0f;
            UpdateFormationDestinations();
        }

        private void CacheCompanions()
        {
            companions.Clear();
            CharacterController leaderCollider = controlledUnit != null
                ? controlledUnit.GetComponent<CharacterController>()
                : null;
            SquadUnit[] units = FindObjectsByType<SquadUnit>(FindObjectsSortMode.InstanceID);
            Array.Sort(units, (left, right) => string.CompareOrdinal(left.name, right.name));
            foreach (SquadUnit unit in units)
            {
                if (controlledUnit == null || unit.gameObject != controlledUnit.gameObject)
                {
                    companions.Add(unit);
                    Collider companionCollider = unit.GetComponent<Collider>();
                    if (leaderCollider != null && companionCollider != null)
                    {
                        Physics.IgnoreCollision(leaderCollider, companionCollider, true);
                    }
                }
            }
        }

        private void UpdateFormationDestinations()
        {
            Vector3 forward = controlledUnit.FormationForward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 predictedAnchor = controlledUnit.transform.position +
                                      controlledUnit.MovementVelocity * movementPrediction;

            for (int index = 0; index < companions.Count; index++)
            {
                SquadUnit companion = companions[index];
                if (companion == null || !companion.IsAvailable)
                {
                    continue;
                }

                int row = index / 2;
                float side = index % 2 == 0 ? -1f : 1f;
                float depth = leaderClearance + row * rowSpacing;
                float lateral = lateralSpacing + row * 0.35f;
                Vector3 desiredSlot = predictedAnchor - forward * depth +
                                      right * (side * lateral);

                if (!NavMesh.SamplePosition(desiredSlot, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                if ((companion.Position - navHit.position).sqrMagnitude <=
                    slotArrivalDistance * slotArrivalDistance)
                {
                    companion.Stop();
                }
                else
                {
                    companion.MoveTo(navHit.position);
                }
            }
        }
    }
}
