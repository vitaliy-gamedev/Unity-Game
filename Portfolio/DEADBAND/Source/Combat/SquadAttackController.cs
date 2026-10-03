using System;
using System.Collections.Generic;
using Deadband.AI;
using Deadband.Squad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadband.Combat
{
    [DefaultExecutionOrder(-420)]
    [DisallowMultipleComponent]
    public sealed class SquadAttackController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private PlayerUnitController controlledUnit;
        [SerializeField] private CompanionSquadAI companionMovement;
        [SerializeField] private CompanionWeaponController[] companionWeapons;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField, Min(5f)] private float autonomousEngagementRange = 42f;
        [SerializeField, Min(0.1f)] private float autonomousScanInterval = 0.3f;

        private Health commandedTarget;
        private float nextAutonomousScanTime;
        private string statusMessage = "SQUAD READY";
        private float statusMessageUntil;

        public bool AutonomousTargetingEnabled => autonomousEngagementRange > 0f;

        public void Configure(
            Camera cameraReference,
            PlayerUnitController leader,
            CompanionSquadAI movement,
            CompanionWeaponController[] weapons,
            LayerMask combatTargetMask)
        {
            gameplayCamera = cameraReference;
            controlledUnit = leader;
            companionMovement = movement;
            companionWeapons = weapons;
            targetMask = combatTargetMask;
        }

        private void Update()
        {
            if (commandedTarget != null && (!commandedTarget.IsAlive || !commandedTarget.gameObject.activeInHierarchy))
            {
                ClearAttackOrder("TARGET DOWN // REGROUPING", true);
            }

            if (commandedTarget == null && Time.time >= nextAutonomousScanTime)
            {
                AcquireAutonomousTargets();
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                TryIssueAttackOrder();
            }
            if (keyboard.hKey.wasPressedThisFrame && companionMovement != null)
            {
                companionMovement.SetHold(!companionMovement.IsHolding);
                SetStatus(companionMovement.IsHolding ? "SQUAD HOLDING" : "SQUAD FOLLOWING");
            }
            if (keyboard.gKey.wasPressedThisFrame)
            {
                ClearAttackOrder("REGROUP", true);
            }
        }

        public void ScanForAutonomousTargetsNow()
        {
            if (commandedTarget == null)
            {
                AcquireAutonomousTargets();
            }
        }

        private void AcquireAutonomousTargets()
        {
            nextAutonomousScanTime = Time.time + autonomousScanInterval;
            if (companionWeapons == null || companionWeapons.Length == 0)
            {
                return;
            }

            Health[] healthComponents = FindObjectsByType<Health>(FindObjectsSortMode.None);
            var candidates = new List<Health>(healthComponents.Length);
            foreach (Health candidate in healthComponents)
            {
                if (candidate != null && candidate.IsAlive && IsHostile(candidate))
                {
                    candidates.Add(candidate);
                }
            }

            var assignments = new Dictionary<Health, int>();
            foreach (CompanionWeaponController weapon in companionWeapons)
            {
                if (weapon == null || !weapon.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Health bestTarget = null;
                float bestScore = float.PositiveInfinity;
                foreach (Health candidate in candidates)
                {
                    if (!weapon.CanEngage(candidate, autonomousEngagementRange))
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(weapon.transform.position, candidate.transform.position);
                    float score = distance + ThreatScoreAdjustment(candidate);
                    if (candidate == weapon.CurrentTarget) score -= 3.5f;
                    if (assignments.TryGetValue(candidate, out int assignedCount)) score += assignedCount * 5f;
                    if (score >= bestScore) continue;
                    bestScore = score;
                    bestTarget = candidate;
                }

                if (bestTarget == null)
                {
                    weapon.ClearTarget();
                    continue;
                }

                weapon.SetTarget(bestTarget);
                assignments.TryGetValue(bestTarget, out int currentAssignments);
                assignments[bestTarget] = currentAssignments + 1;
            }
        }

        private static bool IsHostile(Health candidate)
        {
            return candidate.GetComponent<HumanoidEnemyController>() != null ||
                   candidate.GetComponent<MeleeEnemyController>() != null ||
                   candidate.GetComponent<SecurityTurretController>() != null ||
                   candidate.GetComponent<SignalAnomalyController>() != null;
        }

        private static float ThreatScoreAdjustment(Health candidate)
        {
            MeleeEnemyController melee = candidate.GetComponent<MeleeEnemyController>();
            if (melee != null)
            {
                if (melee.IsInCombat) return -12f;
                if (melee.IsEngaged) return -7f;
                if (melee.IsBoss) return -4f;
            }

            HumanoidEnemyController humanoid = candidate.GetComponent<HumanoidEnemyController>();
            if (humanoid != null && humanoid.State is EnemyState.Combat or EnemyState.Chase or EnemyState.Alert)
            {
                return -9f;
            }
            return 0f;
        }

        private void TryIssueAttackOrder()
        {
            if (gameplayCamera == null)
            {
                return;
            }

            Ray ray = gameplayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 120f, targetMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<SquadUnit>() != null)
                {
                    continue;
                }

                Health health = hit.collider.GetComponentInParent<Health>();
                if (health == null || !health.IsAlive)
                {
                    SetStatus("NO VALID TARGET");
                    return;
                }

                commandedTarget = health;
                if (companionMovement != null)
                {
                    companionMovement.SetHold(true);
                }
                foreach (CompanionWeaponController weapon in companionWeapons)
                {
                    weapon?.SetTarget(commandedTarget);
                }
                SetStatus("ATTACK // " + commandedTarget.name.ToUpperInvariant());
                return;
            }

            SetStatus("NO TARGET IN SIGHT");
        }

        private void ClearAttackOrder(string message, bool regroup)
        {
            commandedTarget = null;
            if (companionWeapons != null)
            {
                foreach (CompanionWeaponController weapon in companionWeapons)
                {
                    weapon?.ClearTarget();
                }
            }

            if (regroup && companionMovement != null)
            {
                companionMovement.Regroup();
            }
            SetStatus(message);
        }

        private void SetStatus(string message)
        {
            statusMessage = message;
            statusMessageUntil = Time.unscaledTime + 2.5f;
        }

        private void OnGUI()
        {
            // Companion orders remain audible/behavioral without a persistent debug strip.
        }
    }
}
