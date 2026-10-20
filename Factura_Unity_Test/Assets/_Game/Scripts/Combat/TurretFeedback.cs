using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Візуальний відгук турелі: спалах, коротка віддача та лінія напрямку пострілу.
    /// Контролер вирішує, коли стріляти; цей компонент лише показує факт пострілу.
    /// Віддача рухає дочірню візуальну модель, не підміняючи бойове наведення.
    /// </summary>
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
            // Зберігаємо базове положення, щоб ефект не накопичував зміщення після кожного пострілу.
            turret = GetComponent<TurretController>();
            restPosition = visual.localPosition;
        }

        private void OnEnable()
        {
            // Кожному Fired відповідає один спалах; окремий таймер стрільби тут не потрібний.
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
            // На екрані результату не лишаються лазерна лінія, віддача чи незавершений спалах.
            recoil = 0;
            visual.localPosition = restPosition;
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnFired()
        {
            // Перезапускаємо готовий ParticleSystem, а не створюємо ефект заново.
            recoil = 0.075f;
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play();
        }

        private void LateUpdate()
        {
            if (!turret.IsActive) return;
            // MoveTowards отримує максимальне зміщення за кадр. deltaTime
            // переводить швидкість повернення в цей крок, щоб віддача не залежала лише від FPS.
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 0.9f);
            visual.localPosition = restPosition - Vector3.forward * recoil;
            // localPosition задається відносно батька. Тут Vector3.forward — локальна
            // вісь дочірньої моделі, а не вимога відсувати всю турель уздовж світового Z.

            // Лінія оновлюється після наведення в Update, щоб відповідати видимому стволу.
            var ray = new Ray(turret.Muzzle.position, turret.Muzzle.forward);
            Vector3 barrel = ray.origin - turret.BarrelOrigin;
            if (barrel.sqrMagnitude > 0.000001f && Physics.SphereCast(turret.BarrelOrigin, turret.ShotRadius,
                    barrel.normalized, out _, barrel.magnitude, targetLayers | environmentLayers, QueryTriggerInteraction.Collide))
            {
                // Перешкода між основою та дулом уже зупинить кулю в Launch;
                // не малюємо підказку крізь неї, наче дальня ціль доступна.
                aimLine.SetPosition(0, ray.origin);
                aimLine.SetPosition(1, ray.origin);
                return;
            }
            float distance = guideLength;
            // Обмежуємо підказку землею або першою перешкодою. Це візуальна
            // допомога Raycast, а фактичне влучання кулі перевіряє її об'ємний SphereCast.
            var ground = new Plane(Vector3.up, Vector3.up * 0.025f);
            if (ground.Raycast(ray, out float groundDistance)) distance = Mathf.Min(distance, groundDistance);
            if (Physics.Raycast(ray, out var hit, distance, targetLayers | environmentLayers, QueryTriggerInteraction.Collide)) distance = hit.distance;
            aimLine.SetPosition(0, ray.origin);
            aimLine.SetPosition(1, ray.GetPoint(distance));
        }
    }
}
