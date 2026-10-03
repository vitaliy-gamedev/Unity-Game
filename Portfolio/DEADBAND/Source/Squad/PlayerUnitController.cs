using Deadband.Combat;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Deadband.Squad
{
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SquadUnit), typeof(NavMeshAgent), typeof(CharacterController))]
    public sealed class PlayerUnitController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private LayerMask aimSurfaceMask = ~0;
        [SerializeField, Min(0.1f)] private float movementSpeed = 5.2f;
        [SerializeField, Min(1f)] private float rotationSpeed = 900f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -24f;

        private SquadUnit squadUnit;
        private NavMeshAgent agent;
        private CharacterController characterController;
        private Health health;
        private float verticalVelocity;

        public Vector3 MovementVelocity { get; private set; }
        public Vector3 FormationForward { get; private set; } = Vector3.forward;
        public Vector3 AimPoint { get; private set; }
        public bool HasAimPoint { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsGrounded => characterController != null && characterController.isGrounded;

        public void Configure(Camera cameraReference, LayerMask surfaceMask)
        {
            gameplayCamera = cameraReference;
            aimSurfaceMask = surfaceMask;
        }

        private void Awake()
        {
            squadUnit = GetComponent<SquadUnit>();
            agent = GetComponent<NavMeshAgent>();
            characterController = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            agent.enabled = false;

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }
        }

        private void Start()
        {
            squadUnit.SetSelected(true);
        }

        private void Update()
        {
            if (characterController == null || !characterController.enabled)
            {
                MovementVelocity = Vector3.zero;
                return;
            }

            Vector3 movementDirection = ReadMovementDirection();
            MovementVelocity = movementDirection * movementSpeed;
            if (movementDirection.sqrMagnitude > 0.01f)
            {
                FormationForward = movementDirection;
            }

            UpdateVerticalVelocity();
            characterController.Move(
                (MovementVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            Vector3 aimDirection = ReadAimDirection();
            Mouse mouse = Mouse.current;
            IsAiming = mouse != null && mouse.rightButton.isPressed &&
                       Cursor.lockState == CursorLockMode.Locked;
            Vector3 facingDirection = IsAiming ? aimDirection : movementDirection;
            if (facingDirection.sqrMagnitude < 0.01f && IsAiming)
            {
                facingDirection = transform.forward;
            }

            if (facingDirection.sqrMagnitude > 0.01f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(facingDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    desiredRotation,
                    rotationSpeed * Time.deltaTime);
            }
        }

        private Vector3 ReadMovementDirection()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || gameplayCamera == null)
            {
                return Vector3.zero;
            }

            Vector2 input = Vector2.zero;
            input.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            if (input.sqrMagnitude < 0.01f)
            {
                return Vector3.zero;
            }

            Vector3 cameraForward = Vector3.ProjectOnPlane(gameplayCamera.transform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(gameplayCamera.transform.right, Vector3.up).normalized;
            return (cameraRight * input.x + cameraForward * input.y).normalized;
        }

        private Vector3 ReadAimDirection()
        {
            HasAimPoint = false;
            if (gameplayCamera == null)
            {
                return Vector3.zero;
            }

            Ray ray = gameplayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 500f, aimSurfaceMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            AimPoint = ray.origin + ray.direction * 100f;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<SquadUnit>() != null)
                {
                    continue;
                }

                AimPoint = hit.point;
                break;
            }

            HasAimPoint = true;
            Vector3 direction = AimPoint - transform.position;
            direction.y = 0f;
            return direction.normalized;
        }

        private void UpdateVerticalVelocity()
        {
            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            Keyboard keyboard = Keyboard.current;
            if (grounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 1f, 0.68f) }
            };
            Color previous = GUI.color;
            GUI.color = new Color(0.015f, 0.03f, 0.025f, 0.86f);
            GUI.DrawTexture(new Rect(16f, 16f, 190f, 38f), Texture2D.whiteTexture);
            GUI.color = previous;
            string healthText = health != null
                ? $"HP {Mathf.CeilToInt(health.CurrentHealth):000}/{Mathf.CeilToInt(health.MaximumHealth):000}"
                : "HP ---";
            GUI.Label(new Rect(28f, 22f, 170f, 26f), healthText, titleStyle);

            Mouse mouse = Mouse.current;
            bool showCrosshair = mouse != null && mouse.rightButton.isPressed &&
                                 Cursor.lockState == CursorLockMode.Locked;
            if (!showCrosshair)
            {
                return;
            }

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            Color crosshairColor = new(0.68f, 1f, 0.76f, 0.95f);
            GUI.color = crosshairColor;
            GUI.DrawTexture(new Rect(centerX - 1f, centerY - 1f, 3f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - 1f, centerY - 13f, 2f, 7f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - 1f, centerY + 6f, 2f, 7f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - 13f, centerY - 1f, 7f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX + 6f, centerY - 1f, 7f, 2f), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
