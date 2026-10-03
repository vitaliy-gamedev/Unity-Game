using Deadband.Combat;
using Deadband.Squad;
using UnityEngine;
using UnityEngine.AI;

namespace Deadband.AI
{
    [DefaultExecutionOrder(-390)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public sealed class MeleeEnemyController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private bool boss;
        [SerializeField] private float detectionRange = 27f;
        [SerializeField] private float attackRange = 2.1f;
        [SerializeField] private float attackRadius = 2.35f;
        [SerializeField] private float attackDamage = 14f;
        [SerializeField] private float attackInterval = 1.35f;
        [SerializeField] private float moveSpeed = 4.6f;
        [SerializeField] private float patrolRadius = 7f;
        [SerializeField] private float patrolSpeed = 1.65f;

        private NavMeshAgent agent;
        private Health health;
        private SquadUnit[] squad;
        private SquadUnit target;
        private float nextScanTime;
        private float nextAttackTime;
        private float attackAnimationUntil;
        private float hitAnimationUntil;
        private float deathDisableAt;
        private float nextPatrolDecisionTime;
        private float lastHealth;
        private Vector3 homePosition;
        private string animationState;
        private bool dead;
        private bool berserk;

        public bool IsBoss => boss;
        public bool IsEngaged { get; private set; }
        public bool IsInCombat { get; private set; }
        public bool IsBerserk => berserk;
        public string CurrentAnimationState => animationState;
        public Animator PresentationAnimator => animator;

        public bool IsAnimatorPlaying(string state)
        {
            if (animator == null || !animator.isActiveAndEnabled) return false;
            int shortHash = Animator.StringToHash(state);
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            return current.shortNameHash == shortHash || next.shortNameHash == shortHash;
        }

        internal void ForceAnimationForDiagnostics(string state)
        {
            if (animator == null) return;
            animationState = state;
            int stateHash = Animator.StringToHash(state);
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
        }

        public void Configure(Animator targetAnimator, bool isBoss)
        {
            animator = targetAnimator;
            boss = isBoss;
            detectionRange = isBoss ? 52f : 36f;
            attackRange = isBoss ? 3.05f : 2.1f;
            attackRadius = isBoss ? 3.8f : 2.35f;
            attackDamage = isBoss ? 30f : 14f;
            attackInterval = isBoss ? 1.75f : 1.35f;
            moveSpeed = isBoss ? 4.1f : 4.8f;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();
            squad = FindObjectsByType<SquadUnit>(FindObjectsSortMode.None);
            homePosition = transform.position;
            lastHealth = health != null ? health.MaximumHealth : 0f;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.Rebind();
                animator.Update(0f);
                PlayState("idle1", 0f, true);
            }
            health.HealthChanged += HandleHealthChanged;
            health.Died += HandleDeath;
        }

        private void OnDestroy()
        {
            if (health == null) return;
            health.HealthChanged -= HandleHealthChanged;
            health.Died -= HandleDeath;
        }

        private void Update()
        {
            if (dead)
            {
                if (Time.time >= deathDisableAt) gameObject.SetActive(false);
                return;
            }
            if (health == null || !health.IsAlive) return;
            if (Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + 0.3f;
                target = FindNearestTarget();
            }

            if (target == null)
            {
                IsEngaged = false;
                IsInCombat = false;
                Patrol();
                return;
            }

            float distance = Vector3.Distance(transform.position, target.Position);
            float activeDetectionRange = detectionRange * (berserk ? 1.25f : 1f);
            IsEngaged = distance <= activeDetectionRange;
            IsInCombat = IsEngaged && distance <= (boss ? 18f : berserk ? 14f : 10f);
            if (!IsEngaged)
            {
                Patrol();
                return;
            }

            FaceTarget(target.Position);
            if (distance <= attackRange)
            {
                StopAgent();
                if (Time.time >= nextAttackTime) Attack();
                else if (Time.time >= attackAnimationUntil) PlayState("idle1", 0.12f);
                return;
            }

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = moveSpeed * (berserk ? 1.38f : 1f);
                agent.SetDestination(target.Position);
            }
            PlayState("run", 0.16f);
        }

        private void Patrol()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                PlayState("idle1", 0.2f);
                return;
            }

            bool reached = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= 0.7f);
            if (reached && Time.time >= nextPatrolDecisionTime)
            {
                nextPatrolDecisionTime = Time.time + Random.Range(1.2f, 3.2f);
                Vector2 offset = Random.insideUnitCircle * patrolRadius;
                Vector3 candidate = homePosition + new Vector3(offset.x, 0f, offset.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, agent.areaMask))
                {
                    agent.isStopped = false;
                    agent.speed = (boss ? patrolSpeed * 0.8f : patrolSpeed) * (berserk ? 1.25f : 1f);
                    agent.SetDestination(hit.position);
                }
            }

            if (agent.hasPath && !reached)
            {
                PlayState("walk", 0.2f);
            }
            else
            {
                StopAgent();
                PlayState("idle1", 0.2f);
            }
        }

        private SquadUnit FindNearestTarget()
        {
            SquadUnit nearest = null;
            float bestDistance = float.MaxValue;
            foreach (SquadUnit unit in squad)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;
                Health unitHealth = unit.GetComponent<Health>();
                if (unitHealth == null || !unitHealth.IsAlive) continue;
                float sqrDistance = (unit.Position - transform.position).sqrMagnitude;
                if (sqrDistance >= bestDistance) continue;
                bestDistance = sqrDistance;
                nearest = unit;
            }
            return nearest;
        }

        private void Attack()
        {
            nextAttackTime = Time.time + attackInterval * (berserk ? 0.62f : 1f);
            IsInCombat = true;
            attackAnimationUntil = Time.time + Mathf.Min(0.9f, attackInterval * 0.65f);
            PlayState(Random.value > 0.5f ? "atack1" : "atack2", 0.08f, true);
            foreach (SquadUnit unit in squad)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;
                Health unitHealth = unit.GetComponent<Health>();
                if (unitHealth == null || !unitHealth.IsAlive ||
                    Vector3.Distance(transform.position, unit.Position) > attackRadius) continue;
                Vector3 direction = (unit.Position - transform.position).normalized;
                unitHealth.ApplyDamage(attackDamage * (berserk ? 1.45f : 1f), unit.Position + Vector3.up, direction);
            }
            CombatNoiseSystem.Emit(transform.position, boss ? 30f : 12f, gameObject);
        }

        private void FaceTarget(Vector3 position)
        {
            Vector3 direction = Vector3.ProjectOnPlane(position - transform.position, Vector3.up);
            if (direction.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                (boss ? 320f : 520f) * Time.deltaTime);
        }

        private void StopAgent()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.ResetPath();
        }

        private void PlayState(string state, float fade, bool force = false)
        {
            if (!force && Time.time < hitAnimationUntil && state is "idle1" or "walk" or "run") return;
            if (animator == null || (!force && animationState == state)) return;
            animationState = state;
            int stateHash = Animator.StringToHash(state);
            if (!animator.HasState(0, stateHash))
            {
                int fullPathHash = Animator.StringToHash("Base Layer." + state);
                if (!animator.HasState(0, fullPathHash))
                {
                    Debug.LogError($"Missing infected animation state '{state}' on {name}.", this);
                    return;
                }
                stateHash = fullPathHash;
            }
            if (fade <= 0f) animator.Play(stateHash, 0, Random.Range(0f, 0.2f));
            else animator.CrossFadeInFixedTime(stateHash, fade, 0);
        }

        private void HandleHealthChanged(float currentHealth, float _)
        {
            bool tookDamage = currentHealth > 0f && currentHealth < lastHealth;
            lastHealth = currentHealth;
            if (!tookDamage || dead) return;
            nextScanTime = 0f;
            hitAnimationUntil = Time.time + (boss ? 0.1f : 0.16f);
            PlayState("gethit", 0.025f, true);
            if (!berserk && currentHealth <= health.MaximumHealth * 0.5f)
            {
                ActivateBerserk();
            }
        }

        private void ActivateBerserk()
        {
            berserk = true;
            IsEngaged = true;
            IsInCombat = true;
            nextAttackTime = Mathf.Min(nextAttackTime, Time.time + 0.25f);
            if (animator != null) animator.speed = boss ? 1.16f : 1.24f;
            health.SetBaseEmission(new Color(0.32f, 0.018f, 0.005f) * 2.1f);
            CombatNoiseSystem.Emit(transform.position, boss ? 48f : 26f, gameObject);
        }

        private void HandleDeath()
        {
            if (dead) return;
            dead = true;
            IsEngaged = false;
            IsInCombat = false;
            StopAgent();
            if (agent != null) agent.enabled = false;
            foreach (Collider targetCollider in GetComponentsInChildren<Collider>())
            {
                targetCollider.enabled = false;
            }
            PlayState("death1", 0.1f, true);
            deathDisableAt = Time.time + (boss ? 4.8f : 3.25f);
        }
    }
}
