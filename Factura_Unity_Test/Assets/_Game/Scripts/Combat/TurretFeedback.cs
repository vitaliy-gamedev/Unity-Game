using UnityEngine;

namespace Factura
{
    [RequireComponent(typeof(TurretController))]
    public sealed class TurretFeedback : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private LayerMask targetLayers;
        [SerializeField] private LayerMask environmentLayers;
        [SerializeField, Min(1)] private float guideLength = 40;
        private TurretController turret;
        private Vector3 restPosition;
        private float recoil;

        private void Awake()
        {
            turret = GetComponent<TurretController>();
            restPosition = visual.localPosition;
        }

        private void OnEnable()
        {
            turret.Fired += OnFired;
            turret.ActiveChanged += OnActiveChanged;
            OnActiveChanged(turret.IsActive);
        }

        private void OnDisable()
        {
            turret.Fired -= OnFired;
            turret.ActiveChanged -= OnActiveChanged;
            OnActiveChanged(false);
        }

        private void OnActiveChanged(bool active)
        {
            aimLine.enabled = active;
            if (active) return;
            recoil = 0;
            visual.localPosition = restPosition;
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnFired()
        {
            recoil = 0.075f;
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }

        private void LateUpdate()
        {
            if (!turret.IsActive) return;
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 0.9f);
            visual.localPosition = restPosition - Vector3.forward * recoil;

            var ray = new Ray(turret.Muzzle.position, turret.Muzzle.forward);
            Vector3 barrel = ray.origin - turret.BarrelOrigin;
            if (barrel.sqrMagnitude > 0.000001f && Physics.SphereCast(turret.BarrelOrigin, turret.ShotRadius,
                    barrel.normalized, out _, barrel.magnitude, targetLayers | environmentLayers, QueryTriggerInteraction.Collide))
            {
                aimLine.SetPosition(0, ray.origin);
                aimLine.SetPosition(1, ray.origin);
                return;
            }
            float distance = guideLength;
            var ground = new Plane(Vector3.up, Vector3.up * 0.025f);
            if (ground.Raycast(ray, out float groundDistance)) distance = Mathf.Min(distance, groundDistance);
            if (Physics.Raycast(ray, out var hit, distance, targetLayers | environmentLayers, QueryTriggerInteraction.Collide)) distance = hit.distance;
            aimLine.SetPosition(0, ray.origin);
            aimLine.SetPosition(1, ray.GetPoint(distance));
        }
    }
}
