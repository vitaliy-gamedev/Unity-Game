using Deadband.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Deadband.AI
{
    [DefaultExecutionOrder(-260)]
    [DisallowMultipleComponent]
    public sealed class HumanoidEnemyPresentation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject rifleVisual;
        [SerializeField] private Transform rifleMuzzle;

        private NavMeshAgent agent;
        private HumanoidEnemyController controller;
        private Health health;
        private Transform rightShoulder;
        private float previousHealth;
        private float deathDisableAt;
        private bool dead;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int AimingHash = Animator.StringToHash("Aiming");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        public Animator Animator => animator;
        public GameObject RifleVisual => rifleVisual;

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
            animator.Play(Animator.StringToHash(state), 0, 0f);
            animator.Update(0f);
        }

        public void Configure(Animator targetAnimator, GameObject weapon, Transform muzzle)
        {
            animator = targetAnimator;
            rifleVisual = weapon;
            rifleMuzzle = muzzle;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            controller = GetComponent<HumanoidEnemyController>();
            health = GetComponent<Health>();
            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                rightShoulder = animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.RightShoulder)
                    : null;
            }
        }

        private void Start()
        {
            if (health != null)
            {
                previousHealth = health.CurrentHealth;
                health.HealthChanged += HandleHealthChanged;
                health.Died += HandleDeath;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.HealthChanged -= HandleHealthChanged;
                health.Died -= HandleDeath;
            }
        }

        private void Update()
        {
            if (dead)
            {
                if (Time.time >= deathDisableAt) gameObject.SetActive(false);
                return;
            }
            if (animator == null || controller == null || controller.State == EnemyState.Dead)
            {
                return;
            }

            float speed = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
            bool aiming = controller.State is EnemyState.Alert or EnemyState.Combat or EnemyState.Chase;
            animator.speed = controller.IsBerserk ? 1.15f : 1f;
            animator.SetFloat(SpeedHash, speed, 0.12f, Time.deltaTime);
            animator.SetFloat(MotionSpeedHash, speed > 0.05f ? 1f : 0f);
            animator.SetBool(GroundedHash, true);
            animator.SetBool(AimingHash, aiming);
        }

        private void LateUpdate()
        {
            if (rifleVisual == null || rifleVisual.transform.parent == null)
            {
                return;
            }

            Vector3 direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector3.forward;
            }
            Transform hand = rifleVisual.transform.parent;
            Transform anchor = rightShoulder != null ? rightShoulder : hand;
            rifleVisual.transform.SetPositionAndRotation(
                anchor.position,
                Quaternion.LookRotation(direction, Vector3.up));
            Renderer[] renderers = rifleVisual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            float extent = Vector3.Dot(bounds.extents, new Vector3(
                Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z)));
            Vector3 desiredStock = rightShoulder != null
                ? rightShoulder.position + direction * 0.05f - Vector3.up * 0.08f
                : hand.position - direction * 0.2f;
            Vector3 shift = desiredStock - (bounds.center - direction * extent);
            rifleVisual.transform.position += shift;
            if (rifleMuzzle != null)
            {
                rifleMuzzle.SetPositionAndRotation(
                    bounds.center + shift + direction * extent,
                    Quaternion.LookRotation(direction, Vector3.up));
            }
        }

        private void HandleHealthChanged(float currentHealth, float _)
        {
            if (animator != null && currentHealth > 0f && currentHealth < previousHealth)
            {
                animator.SetTrigger(HitHash);
            }
            previousHealth = currentHealth;
        }

        private void HandleDeath()
        {
            if (dead) return;
            dead = true;
            foreach (Collider targetCollider in GetComponentsInChildren<Collider>())
            {
                targetCollider.enabled = false;
            }
            if (agent != null) agent.enabled = false;
            if (animator != null)
            {
                for (int layer = 1; layer < animator.layerCount; layer++) animator.SetLayerWeight(layer, 0f);
                animator.CrossFadeInFixedTime(Animator.StringToHash("Death"), 0.08f, 0);
            }
            deathDisableAt = Time.time + 3.25f;
        }
    }
}
