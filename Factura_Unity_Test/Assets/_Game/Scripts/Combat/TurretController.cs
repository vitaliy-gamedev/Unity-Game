using System;
using System.Collections.Generic;
using UnityEngine;

namespace Factura
{
    public sealed class TurretController : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform elevationPivot;
        private ProjectilePool projectilePool;
        private IReadOnlyList<EnemyAI> enemies;
        private ExplosiveBarrel barrel;
        [SerializeField, Range(1, 180)] private float maximumYaw = 90;
        [SerializeField, Min(1)] private float degreesPerScreen = 180;
        [SerializeField, Min(1)] private float aimSpeed = 720;
        [SerializeField, Min(0.01f)] private float aimSmoothTime = 0.06f;
        [SerializeField, Min(0.1f)] private float shotsPerSecond = 6;
        private float targetYaw;
        private float currentYaw;
        private float cooldown;
        private bool isActive;
        private float currentPitch;
        private float yawVelocity;
        private bool pointerAim;
        private Vector2 pointerPosition;
        private Camera aimCamera;
        public bool IsActive => isActive;
        public Transform Muzzle => muzzle;
        public Vector3 BarrelOrigin => elevationPivot ? elevationPivot.position : muzzle.position;
        public float ShotRadius => projectilePool ? projectilePool.HitRadius : 0;
        public event Action Fired;
        public event Action<bool> ActiveChanged;

        private void Awake() => aimCamera = Camera.main;

        public void SetActive(bool value)
        {
            if (isActive == value) return;
            isActive = value;
            if (!value) yawVelocity = 0;
            ActiveChanged?.Invoke(value);
        }
        public void Initialize(ProjectilePool pool, IReadOnlyList<EnemyAI> targets, ExplosiveBarrel explosive = null)
        {
            projectilePool = pool;
            enemies = targets;
            barrel = explosive;
        }

        public void Aim(float normalizedDelta)
        {
            if (!isActive) return;
            pointerAim = false;
            float value = targetYaw + normalizedDelta * degreesPerScreen;
            SetTargetYaw(value);
        }

        public void AimAtScreenPosition(Vector2 screenPosition)
        {
            if (!isActive) return;
            pointerPosition = screenPosition;
            pointerAim = true;
        }

        private void SetTargetYaw(float value) => targetYaw = maximumYaw >= 179.9f
            ? Mathf.Repeat(value + 180, 360) - 180 : Mathf.Clamp(value, -maximumYaw, maximumYaw);

        public void ResetAim()
        {
            SetActive(false);
            targetYaw = currentYaw = cooldown = 0;
            yawVelocity = 0;
            pointerAim = false;
            transform.localRotation = Quaternion.identity;
            currentPitch = 0;
            if (elevationPivot) elevationPivot.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            if (!isActive) return;
            if (pointerAim) UpdatePointerAim();
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, aimSmoothTime, aimSpeed, Time.deltaTime);
            transform.localRotation = Quaternion.Euler(0, currentYaw, 0);
            UpdateElevation();
        }

        private void UpdatePointerAim()
        {
            if (!aimCamera) aimCamera = Camera.main;
            if (!aimCamera || !aimCamera.pixelRect.Contains(pointerPosition)) return;
            Ray ray = aimCamera.ScreenPointToRay(pointerPosition);
            Vector3 point;
            // Pick the body under the cursor before projecting empty space. This avoids camera-height
            // parallax at contact distance, without automatically choosing enemies elsewhere on screen.
            RaycastHit hit = default;
            bool onTarget = projectilePool && Physics.Raycast(ray, out hit, aimCamera.farClipPlane,
                projectilePool.TargetLayers, QueryTriggerInteraction.Collide);
            if (onTarget) point = hit.collider.bounds.center;
            else
            {
                var plane = new Plane(Vector3.up, Vector3.up * 0.9f);
                if (!plane.Raycast(ray, out float distance)) return;
                point = ray.GetPoint(distance);
            }
            Vector3 direction = transform.parent ? transform.parent.InverseTransformDirection(point - BarrelOrigin) : point - BarrelOrigin;
            direction.y = 0;
            // Keep the last direction when hovering on the pivot itself rather than flipping 180 degrees.
            if (direction.sqrMagnitude < (onTarget ? 0.0001f : 0.04f)) return;
            SetTargetYaw(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        }

        private void UpdateElevation()
        {
            if (!elevationPivot || enemies == null) return;
            float pitch = 8;
            float closest = 40;
            for (int index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (!enemy || !enemy.isActiveAndEnabled || !enemy.IsAlive) continue;
                ConsiderHeight(enemy.AimPoint, enemy.AimRadius, ref closest, ref pitch);
            }
            if (barrel && barrel.isActiveAndEnabled && barrel.IsAlive) ConsiderHeight(barrel.AimPoint, barrel.AimRadius, ref closest, ref pitch);
            currentPitch = Mathf.MoveTowards(currentPitch, pitch, 240 * Time.deltaTime);
            elevationPivot.localRotation = Quaternion.Euler(currentPitch, 0, 0);
        }

        private void ConsiderHeight(Vector3 aimPoint, float targetRadius, ref float closest, ref float pitch)
        {
            Vector3 point = transform.InverseTransformPoint(aimPoint) - elevationPivot.localPosition;
            // Match the actual hit corridor, including grazing hits; do not rotate the player's yaw.
            float hitCorridor = targetRadius + ShotRadius;
            if (point.z <= 0.02f || point.z >= closest || Mathf.Abs(point.x) > hitCorridor) return;
            // Height assistance does not change the player's horizontal aim.
            closest = point.z;
            pitch = Mathf.Clamp(-Mathf.Atan2(point.y, point.z) * Mathf.Rad2Deg, 0, 85);
        }

        private void FixedUpdate()
        {
            if (!isActive || !muzzle || !projectilePool) return;
            cooldown -= Time.fixedDeltaTime;
            if (cooldown > 0) return;
            cooldown += 1f / shotsPerSecond;
            projectilePool.Fire(muzzle.position, muzzle.forward, BarrelOrigin);
            Fired?.Invoke();
        }
    }
}
