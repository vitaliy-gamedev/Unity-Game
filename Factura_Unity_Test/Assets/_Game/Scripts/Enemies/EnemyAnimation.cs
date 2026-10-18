using UnityEngine;

namespace Factura
{
    // In-place bone animation: the AI/Rigidbody owns movement and damage, never the visual pose.
    [RequireComponent(typeof(EnemyAI), typeof(Health))]
    public sealed class EnemyAnimation : MonoBehaviour
    {
        [SerializeField] private Transform hips;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform chest;
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftUpperArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform rightUpperArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform leftThigh;
        [SerializeField] private Transform leftShin;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightThigh;
        [SerializeField] private Transform rightShin;
        [SerializeField] private Transform rightFoot;
        [SerializeField, Min(0.1f)] private float strideLength = 1.6f;
        private EnemyAI enemy;
        private Health health;
        private Vector3 hipRest;
        private Vector3 previousPosition;
        private float cycle;
        private float movement;
        private float strikeTime;
        private float flinch;

        public float MovementBlend => movement;

        private void Awake()
        {
            enemy = GetComponent<EnemyAI>();
            health = GetComponent<Health>();
            hipRest = hips.localPosition;
            previousPosition = transform.position;
            cycle = Mathf.Repeat(transform.position.x * 0.73f + transform.position.z * 0.31f, 1) * Mathf.PI * 2;
        }

        private void OnEnable()
        {
            enemy.Attacked += OnStrike;
            health.Damaged += OnHit;
            previousPosition = transform.position;
        }

        private void OnDisable()
        {
            enemy.Attacked -= OnStrike;
            health.Damaged -= OnHit;
        }

        private void OnStrike() => strikeTime = 0.26f;
        private void OnHit(DamageHit hit) => flinch = 0.14f;

        private void LateUpdate()
        {
            if (!hips.gameObject.activeInHierarchy) return;
            Vector3 travel = transform.position - previousPosition;
            travel.y = 0;
            previousPosition = transform.position;
            bool running = enemy.IsCombatActive && enemy.IsAlive && enemy.State == EnemyState.Chase;
            float speed = running ? travel.magnitude / Mathf.Max(Time.deltaTime, 0.001f) : 0;
            // Smooth fixed-step motion into the render pose; no allocations or mesh rebuilds per frame.
            movement = Mathf.MoveTowards(movement, Mathf.Clamp01(speed / 5.5f), Time.deltaTime * 8);
            cycle += Mathf.Min(speed, 6) * Time.deltaTime / strideLength * Mathf.PI * 2;
            strikeTime = Mathf.Max(0, strikeTime - Time.deltaTime);
            flinch = Mathf.Max(0, flinch - Time.deltaTime);
            bool attacking = enemy.IsCombatActive && enemy.IsAlive && enemy.State == EnemyState.Attack;
            Pose(cycle, movement, attacking ? enemy.AttackWindupProgress : 0,
                attacking ? strikeTime : 0, Time.time, enemy.IsAlive ? flinch / 0.14f : 0);
        }

        private void Pose(float phase, float run, float windup, float strike, float time, float hit)
        {
            float wave = Mathf.Sin(phase);
            float breathing = Mathf.Sin(time * 2.5f) * (1 - run);
            hips.localPosition = hipRest + Vector3.up * (Mathf.Abs(Mathf.Cos(phase)) * 0.025f * run + breathing * 0.004f);
            hips.localRotation = Quaternion.Euler(0, wave * 5 * run, 0);
            spine.localRotation = Quaternion.Euler(8 * run - hit * 10, 0, -wave * 3 * run);
            chest.localRotation = Quaternion.Euler(0, -wave * 9 * run, breathing);
            head.localRotation = Quaternion.Euler(-5 * run + hit * 8, wave * 3 * run, 0);
            Leg(leftThigh, leftShin, leftFoot, wave, Mathf.Cos(phase), run);
            Leg(rightThigh, rightShin, rightFoot, -wave, -Mathf.Cos(phase), run);

            float leftSwing = -5 + wave * 32 * run;
            float rightSwing = -5 - wave * 32 * run;
            float elbow = 12 + 53 * run;
            if (windup > 0)
            {
                float preparation = Mathf.SmoothStep(0, 1, windup);
                leftSwing = Mathf.Lerp(-15, -135, preparation);
                rightSwing = leftSwing;
                elbow = Mathf.Lerp(25, 60, preparation);
                chest.localRotation = Quaternion.Euler(-preparation * 8, 0, 0);
            }
            if (strike > 0)
            {
                float recovery = Mathf.Clamp01((0.26f - strike - 0.07f) / 0.19f);
                leftSwing = Mathf.Lerp(-85, -15, Mathf.SmoothStep(0, 1, recovery));
                rightSwing = leftSwing;
                elbow = Mathf.Lerp(15, 25, recovery);
                chest.localRotation = Quaternion.Euler((1 - recovery) * 12, 0, 0);
            }
            leftUpperArm.localRotation = Quaternion.Euler(leftSwing, 0, 75);
            rightUpperArm.localRotation = Quaternion.Euler(rightSwing, 0, -75);
            leftForearm.localRotation = Quaternion.Euler(0, elbow, 0);
            rightForearm.localRotation = Quaternion.Euler(0, -elbow, 0);
        }

        private static void Leg(Transform thigh, Transform shin, Transform foot, float wave, float lift, float run)
        {
            float upper = -wave * 38 * run;
            float lower = Mathf.Max(0, -lift) * 65 * run;
            thigh.localRotation = Quaternion.Euler(upper, 0, 0);
            shin.localRotation = Quaternion.Euler(lower, 0, 0);
            foot.localRotation = Quaternion.Euler(-lower * 0.65f - upper * 0.5f, 0, 0);
        }

#if UNITY_EDITOR
        // Used by the opt-in editor pose renderer, not by gameplay.
        public void PreviewPose(float phase, float run, float windup, float strike)
        {
            hipRest = new Vector3(0, 0.69f, -0.03f);
            Pose(phase, run, windup, strike, 0, 0);
        }
#endif
    }
}
