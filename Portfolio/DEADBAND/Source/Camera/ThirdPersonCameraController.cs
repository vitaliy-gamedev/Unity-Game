using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadband.CameraSystem
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Cinemachine Rig")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;

        [Header("Mouse Look")]
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private float initialYaw;
        [SerializeField] private float initialPitch = 14f;
        [SerializeField] private Vector2 pitchLimits = new(-25f, 58f);
        [SerializeField, Min(0f)] private float pivotHeight = 1.35f;

        [Header("Aim Down Sights")]
        [SerializeField, Range(25f, 70f)] private float normalFieldOfView = 60f;
        [SerializeField, Range(20f, 60f)] private float aimedFieldOfView = 42f;
        [SerializeField, Min(1f)] private float normalCameraDistance = 5.2f;
        [SerializeField, Min(1f)] private float aimedCameraDistance = 3.25f;
        [SerializeField, Range(0f, 1.5f)] private float normalShoulderOffset = 0.7f;
        [SerializeField, Range(0f, 1.5f)] private float aimedShoulderOffset = 0.92f;
        [SerializeField, Min(1f)] private float aimTransitionSpeed = 12f;
        [SerializeField, Range(0.1f, 1f)] private float aimedSensitivityMultiplier = 0.65f;

        [Header("Weapon Feedback")]
        [SerializeField, Min(1f)] private float recoilReturnSpeed = 18f;
        [SerializeField, Range(0.1f, 5f)] private float maximumRecoilPitch = 2.4f;
        [SerializeField, Range(0.1f, 3f)] private float maximumRecoilYaw = 1.25f;

        private float yaw;
        private float pitch;
        private float recoilPitch;
        private float recoilYaw;

        public bool IsAiming { get; private set; }

        public void AddRecoil(float verticalKick, float horizontalKick)
        {
            recoilPitch = Mathf.Clamp(recoilPitch + Mathf.Abs(verticalKick), 0f, maximumRecoilPitch);
            recoilYaw = Mathf.Clamp(recoilYaw + horizontalKick, -maximumRecoilYaw, maximumRecoilYaw);
        }

        private void Awake()
        {
            if (followTarget == null || cameraPivot == null || virtualCamera == null || thirdPersonFollow == null)
            {
                Debug.LogError("The Cinemachine third-person camera rig is incomplete.", this);
                enabled = false;
                return;
            }

            yaw = initialYaw;
            pitch = initialPitch;
            ApplyPivot();
            LockCursor();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                IsAiming = false;
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    LockCursor();
                }
                IsAiming = false;
                return;
            }

            IsAiming = mouse.rightButton.isPressed;
            Vector2 delta = mouse.delta.ReadValue();
            float sensitivity = mouseSensitivity * (IsAiming ? aimedSensitivityMultiplier : 1f);
            yaw += delta.x * sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, pitchLimits.x, pitchLimits.y);
        }

        private void LateUpdate()
        {
            ApplyPivot();
            ApplyAimTransition();
            float recovery = 1f - Mathf.Exp(-recoilReturnSpeed * Time.unscaledDeltaTime);
            recoilPitch = Mathf.Lerp(recoilPitch, 0f, recovery);
            recoilYaw = Mathf.Lerp(recoilYaw, 0f, recovery);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && Application.isPlaying && Time.timeScale > 0f && !AudioListener.pause)
            {
                LockCursor();
            }
        }

        private void ApplyPivot()
        {
            cameraPivot.position = followTarget.position + Vector3.up * pivotHeight;
            cameraPivot.rotation = Quaternion.Euler(pitch - recoilPitch, yaw + recoilYaw, 0f);
        }

        private void ApplyAimTransition()
        {
            float blend = 1f - Mathf.Exp(-aimTransitionSpeed * Time.unscaledDeltaTime);
            float targetFov = IsAiming ? aimedFieldOfView : normalFieldOfView;
            LensSettings lens = virtualCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, blend);
            virtualCamera.Lens = lens;

            thirdPersonFollow.CameraDistance = Mathf.Lerp(
                thirdPersonFollow.CameraDistance,
                IsAiming ? aimedCameraDistance : normalCameraDistance,
                blend);
            Vector3 shoulder = thirdPersonFollow.ShoulderOffset;
            shoulder.x = Mathf.Lerp(
                shoulder.x,
                IsAiming ? aimedShoulderOffset : normalShoulderOffset,
                blend);
            thirdPersonFollow.ShoulderOffset = shoulder;
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
