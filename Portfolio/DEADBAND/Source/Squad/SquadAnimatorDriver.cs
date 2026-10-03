using Deadband.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Deadband.Squad
{
    [DefaultExecutionOrder(-250)]
    [DisallowMultipleComponent]
    public sealed class SquadAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField, Min(0.1f)] private float speedDamping = 0.12f;

        private PlayerUnitController playerController;
        private PlayerWeaponController playerWeaponController;
        private CompanionWeaponController companionWeaponController;
        private NavMeshAgent navMeshAgent;
        private Health health;
        private float previousHealth;
        private bool previousGrounded = true;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");
        private static readonly int AimingHash = Animator.StringToHash("Aiming");
        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int ReloadHash = Animator.StringToHash("Reload");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        public Animator Animator => animator;

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
        }

        private void Awake()
        {
            playerController = GetComponent<PlayerUnitController>();
            playerWeaponController = GetComponent<PlayerWeaponController>();
            companionWeaponController = GetComponent<CompanionWeaponController>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
        }

        private void Start()
        {
            if (playerWeaponController != null)
            {
                playerWeaponController.Fired += HandleFired;
                playerWeaponController.ReloadStarted += HandleReloadStarted;
            }
            if (companionWeaponController != null)
            {
                companionWeaponController.Fired += HandleFired;
                companionWeaponController.ReloadStarted += HandleReloadStarted;
            }
            if (health != null)
            {
                previousHealth = health.CurrentHealth;
                health.HealthChanged += HandleHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (playerWeaponController != null)
            {
                playerWeaponController.Fired -= HandleFired;
                playerWeaponController.ReloadStarted -= HandleReloadStarted;
            }
            if (companionWeaponController != null)
            {
                companionWeaponController.Fired -= HandleFired;
                companionWeaponController.ReloadStarted -= HandleReloadStarted;
            }
            if (health != null)
            {
                health.HealthChanged -= HandleHealthChanged;
            }
        }

        private void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled ||
                (health != null && !health.IsAlive))
            {
                return;
            }

            float speed = playerController != null
                ? playerController.MovementVelocity.magnitude
                : navMeshAgent != null && navMeshAgent.enabled
                    ? navMeshAgent.velocity.magnitude
                    : 0f;
            bool grounded = playerController == null || playerController.IsGrounded;
            bool jumpStarted = previousGrounded && !grounded;

            animator.SetFloat(SpeedHash, speed, speedDamping, Time.deltaTime);
            animator.SetFloat(MotionSpeedHash, speed > 0.05f ? 1f : 0f);
            animator.SetBool(GroundedHash, grounded);
            animator.SetBool(JumpHash, jumpStarted);
            animator.SetBool(FreeFallHash, !grounded && !jumpStarted);
            animator.SetBool(
                AimingHash,
                playerController != null ? playerController.IsAiming :
                companionWeaponController != null && companionWeaponController.HasTarget);
            previousGrounded = grounded;
        }

        private void HandleFired()
        {
            animator?.SetTrigger(FireHash);
        }

        private void HandleReloadStarted()
        {
            animator?.SetTrigger(ReloadHash);
        }

        private void HandleHealthChanged(float currentHealth, float _)
        {
            if (currentHealth < previousHealth && currentHealth > 0f)
            {
                animator?.SetTrigger(HitHash);
            }
            previousHealth = currentHealth;
        }
    }
}
