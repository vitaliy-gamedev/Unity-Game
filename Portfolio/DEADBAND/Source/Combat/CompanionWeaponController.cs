using System;
using Deadband.AudioSystem;
using Deadband.Squad;
using UnityEngine;

namespace Deadband.Combat
{
    [DefaultExecutionOrder(-450)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SquadUnit))]
    public sealed class CompanionWeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private Light muzzleFlash;
        [SerializeField, Range(0.1f, 1f)] private float damageMultiplier = 0.45f;
        [SerializeField, Min(1f)] private float fireIntervalMultiplier = 2.4f;
        [SerializeField, Min(1f)] private float turnSpeed = 540f;

        private Health target;
        private int magazineAmmo;
        private int reserveAmmo;
        private float nextFireTime;
        private float reloadFinishTime;
        private float effectFinishTime;
        private bool isReloading;
        private WeaponAudioEmitter weaponAudio;

        public Health CurrentTarget => target;
        public bool HasTarget => target != null && target.IsAlive && target.gameObject.activeInHierarchy;
        public int TotalShotsFired { get; private set; }
        public event Action Fired;
        public event Action ReloadStarted;

        public void Configure(
            WeaponData data,
            Transform muzzleTransform,
            LayerMask weaponHitMask,
            LineRenderer tracerRenderer,
            Light flash)
        {
            weaponData = data;
            muzzle = muzzleTransform;
            hitMask = weaponHitMask;
            tracer = tracerRenderer;
            muzzleFlash = flash;
        }

        public void SetTarget(Health newTarget)
        {
            if (target == newTarget)
            {
                return;
            }
            target = newTarget;
            nextFireTime = Mathf.Max(nextFireTime, Time.time + 0.15f);
        }

        public void ClearTarget()
        {
            target = null;
        }

        public bool CanEngage(Health candidate, float maximumDistance)
        {
            if (candidate == null || !candidate.IsAlive || !candidate.gameObject.activeInHierarchy ||
                weaponData == null || muzzle == null)
            {
                return false;
            }

            Vector3 aimPoint = GetAimPoint(candidate);
            Vector3 direction = aimPoint - muzzle.position;
            float distance = direction.magnitude;
            if (distance < 0.01f || distance > Mathf.Min(weaponData.Range, maximumDistance))
            {
                return false;
            }

            return HasLineOfSight(direction / distance, distance, candidate, out _);
        }

        private void Awake()
        {
            weaponAudio = GetComponent<WeaponAudioEmitter>();
            if (weaponData != null)
            {
                magazineAmmo = weaponData.MagazineSize;
                reserveAmmo = weaponData.StartingReserveAmmo;
            }

            SetEffects(false);
        }

        private void Update()
        {
            UpdateEffects();
            UpdateReload();

            if (!HasTarget || weaponData == null || muzzle == null)
            {
                if (target != null && !HasTarget)
                {
                    ClearTarget();
                }
                return;
            }

            Vector3 aimPoint = GetAimPoint(target);
            Vector3 flatDirection = aimPoint - transform.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(flatDirection.normalized, Vector3.up),
                    turnSpeed * Time.deltaTime);
            }

            if (isReloading || Time.time < nextFireTime)
            {
                return;
            }

            if (magazineAmmo <= 0)
            {
                BeginReload();
                return;
            }

            Vector3 shotDirection = aimPoint - muzzle.position;
            float distance = shotDirection.magnitude;
            if (distance > weaponData.Range || distance < 0.01f)
            {
                return;
            }

            shotDirection /= distance;
            if (!HasLineOfSight(shotDirection, distance, target, out Vector3 hitPoint))
            {
                return;
            }

            magazineAmmo--;
            nextFireTime = Time.time + weaponData.ShotInterval * fireIntervalMultiplier;
            CombatNoiseSystem.Emit(transform.position, 24f, gameObject);
            TotalShotsFired++;
            Fired?.Invoke();
            weaponAudio?.PlayShot(false);
            target.ApplyDamage(weaponData.Damage * damageMultiplier, hitPoint, shotDirection);
            ShowShot(muzzle.position, hitPoint);
        }

        private bool HasLineOfSight(
            Vector3 direction,
            float distance,
            Health expectedTarget,
            out Vector3 hitPoint)
        {
            hitPoint = muzzle.position + direction * distance;
            RaycastHit[] hits = Physics.RaycastAll(
                muzzle.position,
                direction,
                distance + 0.5f,
                hitMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<SquadUnit>() != null)
                {
                    continue;
                }

                hitPoint = hit.point;
                return hit.collider.GetComponentInParent<Health>() == expectedTarget;
            }

            return false;
        }

        private static Vector3 GetAimPoint(Health targetHealth)
        {
            Collider targetCollider = targetHealth.GetComponent<Collider>();
            if (targetCollider == null)
            {
                targetCollider = targetHealth.GetComponentInChildren<Collider>();
            }
            if (targetCollider != null)
            {
                return targetCollider.bounds.center;
            }
            return targetHealth.transform.position + Vector3.up;
        }

        private void BeginReload()
        {
            if (reserveAmmo <= 0 || isReloading)
            {
                return;
            }

            isReloading = true;
            reloadFinishTime = Time.time + weaponData.ReloadDuration;
            ReloadStarted?.Invoke();
        }

        private void UpdateReload()
        {
            if (!isReloading || Time.time < reloadFinishTime)
            {
                return;
            }

            int needed = weaponData.MagazineSize - magazineAmmo;
            int loaded = Mathf.Min(needed, reserveAmmo);
            magazineAmmo += loaded;
            reserveAmmo -= loaded;
            isReloading = false;
        }

        private void ShowShot(Vector3 origin, Vector3 endPoint)
        {
            if (tracer != null)
            {
                tracer.SetPosition(0, origin);
                tracer.SetPosition(1, endPoint);
            }
            if (muzzleFlash != null)
            {
                muzzleFlash.transform.position = origin;
            }
            SetEffects(true);
            effectFinishTime = Time.time + 0.045f;
        }

        private void UpdateEffects()
        {
            if (effectFinishTime > 0f && Time.time >= effectFinishTime)
            {
                effectFinishTime = 0f;
                SetEffects(false);
            }
        }

        private void SetEffects(bool active)
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
    }
}
