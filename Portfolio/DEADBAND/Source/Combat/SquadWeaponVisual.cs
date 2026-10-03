using System;
using Deadband.Squad;
using UnityEngine;

namespace Deadband.Combat
{
    [DefaultExecutionOrder(-580)]
    [DisallowMultipleComponent]
    public sealed class SquadWeaponVisual : MonoBehaviour
    {
        [SerializeField] private PlayerWeaponController playerWeaponController;
        [SerializeField] private GameObject rifleVisual;
        [SerializeField] private GameObject pistolVisual;
        [SerializeField] private Transform rifleMuzzle;
        [SerializeField] private Transform pistolMuzzle;
        [SerializeField] private float rifleForwardOffset = 0.16f;
        [SerializeField] private float rifleVerticalOffset = 0.02f;
        [SerializeField] private float pistolForwardOffset = 0.09f;
        private PlayerUnitController playerUnitController;
        private Animator animator;
        private Transform rightShoulder;
        public GameObject RifleVisual => rifleVisual;
        public GameObject PistolVisual => pistolVisual;
        public Transform RifleMuzzle => rifleMuzzle;
        public Transform PistolMuzzle => pistolMuzzle;

        public void Configure(
            GameObject rifle,
            Transform rifleBarrel,
            GameObject pistol,
            Transform pistolBarrel)
        {
            rifleVisual = rifle;
            rifleMuzzle = rifleBarrel;
            pistolVisual = pistol;
            pistolMuzzle = pistolBarrel;
            ApplyWeaponIndex(0);
        }

        private void Awake()
        {
            playerUnitController = GetComponent<PlayerUnitController>();
            animator = GetComponentInChildren<Animator>();
            rightShoulder = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.RightShoulder)
                : null;
            playerWeaponController = GetComponent<PlayerWeaponController>();
            if (playerWeaponController != null)
            {
                playerWeaponController.WeaponChanged += HandleWeaponChanged;
                ApplyWeaponIndex(playerWeaponController.CurrentWeaponIndex);
            }
            else
            {
                ApplyWeaponIndex(0);
            }
        }

        private void OnDestroy()
        {
            if (playerWeaponController != null)
            {
                playerWeaponController.WeaponChanged -= HandleWeaponChanged;
            }
        }

        private void LateUpdate()
        {
            Vector3 horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (horizontalForward.sqrMagnitude < 0.01f)
            {
                horizontalForward = Vector3.forward;
            }
            Vector3 aimForward = horizontalForward;
            if (playerUnitController != null && playerUnitController.IsAiming &&
                playerUnitController.HasAimPoint && rightShoulder != null)
            {
                Vector3 shoulderToAim = playerUnitController.AimPoint - rightShoulder.position;
                if (shoulderToAim.sqrMagnitude > 0.01f)
                {
                    aimForward = shoulderToAim.normalized;
                }
            }

            StabilizeRifleAtShoulder(
                rifleVisual,
                rifleMuzzle,
                rightShoulder,
                aimForward,
                rifleForwardOffset,
                rifleVerticalOffset);
            StabilizeWeapon(pistolVisual, pistolMuzzle, aimForward, pistolForwardOffset, 0f);
        }

        private void HandleWeaponChanged(int weaponIndex, WeaponData _)
        {
            ApplyWeaponIndex(weaponIndex);
        }

        private void ApplyWeaponIndex(int weaponIndex)
        {
            bool pistolSelected = weaponIndex == 1 && pistolVisual != null;
            if (rifleVisual != null)
            {
                rifleVisual.SetActive(!pistolSelected);
            }
            if (pistolVisual != null)
            {
                pistolVisual.SetActive(pistolSelected);
            }

            if (playerWeaponController != null)
            {
                Transform activeMuzzle = pistolSelected ? pistolMuzzle : rifleMuzzle;
                playerWeaponController.SetMuzzle(activeMuzzle);
            }
        }

        private static void StabilizeWeapon(
            GameObject weaponVisual,
            Transform muzzle,
            Vector3 direction,
            float forwardOffset,
            float verticalOffset)
        {
            if (weaponVisual == null || weaponVisual.transform.parent == null)
            {
                return;
            }

            Transform hand = weaponVisual.transform.parent;
            Vector3 desiredCenter = hand.position + direction * forwardOffset + Vector3.up * verticalOffset;
            weaponVisual.transform.SetPositionAndRotation(hand.position, Quaternion.LookRotation(direction, Vector3.up));
            if (!TryGetRendererBounds(weaponVisual, out Bounds bounds))
            {
                weaponVisual.transform.position = desiredCenter;
                return;
            }

            weaponVisual.transform.position += desiredCenter - bounds.center;
            if (muzzle != null)
            {
                float forwardExtent = Vector3.Dot(bounds.extents, new Vector3(
                    Mathf.Abs(direction.x),
                    Mathf.Abs(direction.y),
                    Mathf.Abs(direction.z)));
                muzzle.SetPositionAndRotation(
                    desiredCenter + direction * forwardExtent,
                    Quaternion.LookRotation(direction, Vector3.up));
            }
        }

        private static void StabilizeRifleAtShoulder(
            GameObject weaponVisual,
            Transform muzzle,
            Transform shoulder,
            Vector3 direction,
            float fallbackForwardOffset,
            float fallbackVerticalOffset)
        {
            if (weaponVisual == null || weaponVisual.transform.parent == null)
            {
                return;
            }
            if (shoulder == null)
            {
                StabilizeWeapon(
                    weaponVisual,
                    muzzle,
                    direction,
                    fallbackForwardOffset,
                    fallbackVerticalOffset);
                return;
            }

            weaponVisual.transform.SetPositionAndRotation(
                shoulder.position,
                Quaternion.LookRotation(direction, Vector3.up));
            if (!TryGetRendererBounds(weaponVisual, out Bounds bounds))
            {
                return;
            }

            float forwardExtent = GetExtentAlongDirection(bounds, direction);
            Vector3 shoulderPocket = shoulder.position + direction * 0.055f - Vector3.up * 0.085f;
            Vector3 currentStock = bounds.center - direction * forwardExtent;
            Vector3 shift = shoulderPocket - currentStock;
            weaponVisual.transform.position += shift;
            if (muzzle != null)
            {
                Vector3 shiftedCenter = bounds.center + shift;
                muzzle.SetPositionAndRotation(
                    shiftedCenter + direction * forwardExtent,
                    Quaternion.LookRotation(direction, Vector3.up));
            }
        }

        private static float GetExtentAlongDirection(Bounds bounds, Vector3 direction)
        {
            return Vector3.Dot(bounds.extents, new Vector3(
                Mathf.Abs(direction.x),
                Mathf.Abs(direction.y),
                Mathf.Abs(direction.z)));
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            bounds = default;
            bool found = false;
            foreach (Renderer targetRenderer in renderers)
            {
                if (!found)
                {
                    bounds = targetRenderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }
            return found;
        }

    }
}
