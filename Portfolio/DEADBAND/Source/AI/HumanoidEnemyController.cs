using System;
using Deadband.Combat;
using Deadband.AudioSystem;
using Deadband.Signals;
using Deadband.Squad;
using UnityEngine;
using UnityEngine.AI;

namespace Deadband.AI
{
    [DefaultExecutionOrder(-400)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(Health), typeof(EnemyPerception))]
    public sealed class HumanoidEnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyData data;
        [SerializeField] private SignalSystem signalSystem;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask combatMask = ~0;
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private Light muzzleFlash;

        private NavMeshAgent agent;
        private Health health;
        private SquadUnit currentTarget;
        private Vector3 lastKnownPosition;
        private int patrolIndex;
        private float visualContactTime;
        private float alertUntil;
        private float searchUntil;
        private float nextSearchMoveTime;
        private float lastVisualTime;
        private float nextFireTime;
        private float effectFinishTime;
        private SquadUnit[] squad;
        private WeaponAudioEmitter weaponAudio;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public bool IsBerserk { get; private set; }

        public void Configure(
            EnemyData enemyData,
            SignalSystem signal,
            EnemyPerception enemyPerception,
            Transform[] route,
            Transform muzzleTransform,
            LayerMask weaponMask,
            LineRenderer tracerRenderer,
            Light flash)
        {
            data = enemyData;
            signalSystem = signal;
            perception = enemyPerception;
            patrolPoints = route;
            muzzle = muzzleTransform;
            combatMask = weaponMask;
            tracer = tracerRenderer;
            muzzleFlash = flash;
        }

        private void Awake()
        {
            weaponAudio = GetComponent<WeaponAudioEmitter>();
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            squad = FindObjectsByType<SquadUnit>(FindObjectsSortMode.None);
            if (perception == null)
            {
                perception = GetComponent<EnemyPerception>();
            }
            health.Died += HandleDeath;
            health.HealthChanged += HandleHealthChanged;
            SetShotEffects(false);
        }

        private void Start()
        {
            EnterState(patrolPoints != null && patrolPoints.Length > 0
                ? EnemyState.Patrol
                : EnemyState.Idle);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
                health.HealthChanged -= HandleHealthChanged;
            }
        }

        private void Update()
        {
            UpdateShotEffects();
            if (State == EnemyState.Dead || data == null || perception == null)
            {
                return;
            }

            float signalStrength = signalSystem != null ? signalSystem.Strength : 0f;
            float visionRange = data.VisionRange * (1f + signalStrength * 0.5f) * (IsBerserk ? 1.35f : 1f);
            float reactionTime = data.ReactionTime * (1f - signalStrength * 0.35f) * (IsBerserk ? 0.45f : 1f);
            bool seesTarget = perception.TryGetVisibleTarget(
                visionRange,
                data.VisionAngle,
                out SquadUnit visibleTarget);

            if (seesTarget)
            {
                currentTarget = visibleTarget;
                lastKnownPosition = visibleTarget.Position;
                lastVisualTime = Time.time;

                if (State is EnemyState.Patrol or EnemyState.Idle or EnemyState.ReturnToPatrol or
                    EnemyState.Investigate or EnemyState.Search)
                {
                    visualContactTime = 0f;
                    EnterState(EnemyState.Suspicious);
                }

                if (State == EnemyState.Suspicious)
                {
                    visualContactTime += Time.deltaTime;
                    FacePosition(lastKnownPosition);
                    if (visualContactTime >= reactionTime)
                    {
                        EnterState(EnemyState.Alert);
                    }
                }
            }
            else if (State == EnemyState.Suspicious)
            {
                EnterState(EnemyState.Search);
            }

            if (!seesTarget && (State is EnemyState.Patrol or EnemyState.Idle or EnemyState.ReturnToPatrol) &&
                perception.TryConsumeNoise(data.HearingMultiplier * (1f + signalStrength * 0.4f), out Vector3 noisePosition))
            {
                lastKnownPosition = noisePosition;
                EnterState(EnemyState.Investigate);
            }

            switch (State)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    break;
                case EnemyState.Investigate:
                    UpdateInvestigate();
                    break;
                case EnemyState.Alert:
                    FacePosition(lastKnownPosition);
                    if (Time.time >= alertUntil)
                    {
                        EnterState(EnemyState.Combat);
                    }
                    break;
                case EnemyState.Combat:
                    UpdateCombat(seesTarget, signalStrength);
                    break;
                case EnemyState.Chase:
                    UpdateChase(seesTarget);
                    break;
                case EnemyState.Search:
                    UpdateSearch(signalStrength);
                    break;
                case EnemyState.ReturnToPatrol:
                    UpdateReturnToPatrol();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                EnterState(EnemyState.Idle);
                return;
            }

            if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= 0.35f))
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                MoveTo(patrolPoints[patrolIndex].position, data.PatrolSpeed);
            }
        }

        private void UpdateInvestigate()
        {
            if (!agent.pathPending && agent.remainingDistance <= 0.55f)
            {
                EnterState(EnemyState.Search);
            }
        }

        private void UpdateCombat(bool seesTarget, float signalStrength)
        {
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                EnterState(EnemyState.Search);
                return;
            }

            if (!seesTarget)
            {
                if (Time.time - lastVisualTime > 0.8f)
                {
                    EnterState(EnemyState.Search);
                }
                return;
            }

            float distance = Vector3.Distance(transform.position, currentTarget.Position);
            if (distance > data.WeaponRange)
            {
                EnterState(EnemyState.Chase);
                return;
            }

            StopAgent();
            FacePosition(currentTarget.Position);
            if (Time.time >= nextFireTime)
            {
                float interval = data.FireInterval * (1f - signalStrength * 0.15f) * (IsBerserk ? 0.58f : 1f);
                nextFireTime = Time.time + interval;
                TryFireAtCurrentTarget();
            }
        }

        private void UpdateChase(bool seesTarget)
        {
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                EnterState(EnemyState.Search);
                return;
            }

            if (seesTarget)
            {
                lastKnownPosition = currentTarget.Position;
                if (Vector3.Distance(transform.position, lastKnownPosition) <= data.WeaponRange * 0.85f)
                {
                    EnterState(EnemyState.Combat);
                    return;
                }
            }
            else if (Time.time - lastVisualTime > 1.4f)
            {
                EnterState(EnemyState.Search);
                return;
            }

            MoveTo(lastKnownPosition, data.ChaseSpeed * (IsBerserk ? 1.32f : 1f));
        }

        private void UpdateSearch(float signalStrength)
        {
            if (Time.time >= searchUntil)
            {
                EnterState(EnemyState.ReturnToPatrol);
                return;
            }

            if (Time.time < nextSearchMoveTime || agent.pathPending ||
                (agent.hasPath && agent.remainingDistance > 0.5f))
            {
                return;
            }

            nextSearchMoveTime = Time.time + 1.1f;
            Vector3 randomPoint = lastKnownPosition + UnityEngine.Random.insideUnitSphere * (2.5f + signalStrength * 2f);
            randomPoint.y = lastKnownPosition.y;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, 2.5f, NavMesh.AllAreas))
            {
                MoveTo(navHit.position, data.PatrolSpeed * 1.2f);
            }
        }

        private void UpdateReturnToPatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                EnterState(EnemyState.Idle);
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= 0.45f)
            {
                EnterState(EnemyState.Patrol);
            }
        }

        private void EnterState(EnemyState newState)
        {
            if (State == EnemyState.Dead)
            {
                return;
            }

            State = newState;
            switch (newState)
            {
                case EnemyState.Idle:
                case EnemyState.Suspicious:
                case EnemyState.Combat:
                    StopAgent();
                    break;
                case EnemyState.Patrol:
                    if (patrolPoints != null && patrolPoints.Length > 0)
                    {
                        MoveTo(patrolPoints[patrolIndex].position, data.PatrolSpeed);
                    }
                    break;
                case EnemyState.Investigate:
                    MoveTo(lastKnownPosition, data.PatrolSpeed * 1.35f);
                    break;
                case EnemyState.Alert:
                    StopAgent();
                    alertUntil = Time.time + 0.35f;
                    break;
                case EnemyState.Search:
                    searchUntil = Time.time + data.SearchDuration *
                                  (1f + (signalSystem != null ? signalSystem.Strength : 0f) * 0.6f);
                    nextSearchMoveTime = 0f;
                    MoveTo(lastKnownPosition, data.PatrolSpeed * 1.2f);
                    break;
                case EnemyState.ReturnToPatrol:
                    if (patrolPoints != null && patrolPoints.Length > 0)
                    {
                        MoveTo(patrolPoints[patrolIndex].position, data.PatrolSpeed);
                    }
                    break;
            }
        }

        private void TryFireAtCurrentTarget()
        {
            if (muzzle == null || currentTarget == null)
            {
                return;
            }

            Vector3 targetPoint = currentTarget.transform.position + Vector3.up * 1.1f;
            Vector3 direction = targetPoint - muzzle.position;
            float distance = direction.magnitude;
            if (distance < 0.01f)
            {
                return;
            }
            direction /= distance;

            RaycastHit[] hits = Physics.RaycastAll(
                muzzle.position,
                direction,
                distance + 0.4f,
                combatMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<HumanoidEnemyController>() != null)
                {
                    continue;
                }

                SquadUnit hitUnit = hit.collider.GetComponentInParent<SquadUnit>();
                if (hitUnit != currentTarget)
                {
                    return;
                }

                Health targetHealth = hitUnit.GetComponent<Health>();
                targetHealth?.ApplyDamage(data.WeaponDamage * (IsBerserk ? 1.35f : 1f), hit.point, direction);
                weaponAudio?.PlayShot(false);
                ShowShot(muzzle.position, hit.point);
                return;
            }
        }

        private void MoveTo(Vector3 destination, float speed)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }

            agent.speed = speed;
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        private void StopAgent()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }
            agent.isStopped = true;
            agent.ResetPath();
        }

        private void FacePosition(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(direction.normalized, Vector3.up),
                    540f * Time.deltaTime);
            }
        }

        private void HandleDeath()
        {
            State = EnemyState.Dead;
            StopAgent();
            SetShotEffects(false);
        }

        private void HandleHealthChanged(float currentHealth, float maximumHealth)
        {
            if (IsBerserk || currentHealth <= 0f || currentHealth > maximumHealth * 0.5f) return;
            IsBerserk = true;
            health.SetBaseEmission(new Color(0.28f, 0.012f, 0.004f) * 1.8f);
            currentTarget = FindNearestLivingSquad();
            if (currentTarget != null)
            {
                lastKnownPosition = currentTarget.Position;
                lastVisualTime = Time.time;
                EnterState(EnemyState.Alert);
                alertUntil = Time.time + 0.12f;
            }
            CombatNoiseSystem.Emit(transform.position, 24f, gameObject);
        }

        private SquadUnit FindNearestLivingSquad()
        {
            SquadUnit nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (SquadUnit unit in squad)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;
                Health unitHealth = unit.GetComponent<Health>();
                if (unitHealth == null || !unitHealth.IsAlive) continue;
                float distance = (unit.Position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = unit;
            }
            return nearest;
        }

        private void ShowShot(Vector3 origin, Vector3 endPoint)
        {
            if (tracer != null)
            {
                tracer.SetPosition(0, origin);
                tracer.SetPosition(1, endPoint);
            }
            SetShotEffects(true);
            effectFinishTime = Time.time + 0.055f;
        }

        private void UpdateShotEffects()
        {
            if (effectFinishTime > 0f && Time.time >= effectFinishTime)
            {
                effectFinishTime = 0f;
                SetShotEffects(false);
            }
        }

        private void SetShotEffects(bool active)
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
            // Enemy state labels were useful during FSM development but are intentionally hidden in play.
        }

        private Color GetStateColor()
        {
            return State switch
            {
                EnemyState.Combat or EnemyState.Chase => new Color(1f, 0.2f, 0.12f),
                EnemyState.Alert => new Color(1f, 0.55f, 0.1f),
                EnemyState.Suspicious or EnemyState.Investigate or EnemyState.Search =>
                    new Color(1f, 0.85f, 0.25f),
                _ => new Color(0.72f, 0.82f, 0.75f)
            };
        }
    }
}
