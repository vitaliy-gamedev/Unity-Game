using System;
using System.Collections.Generic;
using UnityEngine;

namespace Factura
{
    /// <summary>
    /// Відповідає за горизонтальне наведення, допоміжний нахил ствола й автострільбу.
    /// Ввід приходить готовими командами з InputReader; турель не залежить від пристрою.
    /// Рендерний Update згладжує поворот, а FixedUpdate задає темп створення пострілів.
    /// </summary>
    public sealed class TurretController : MonoBehaviour
    {
        // Геометрію ствола задає префаб, а спільний пул і список цілей передає координатор.
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform elevationPivot;
        private ProjectilePool projectilePool;
        private IReadOnlyList<EnemyAI> enemies;
        private ExplosiveBarrel barrel;
        // float зберігає дробові параметри: yaw і швидкість повороту задані в градусах
        // та градусах/с, aimSmoothTime — у секундах, shotsPerSecond — у пострілах/с.
        // Літерал 0.06f має тип float; атрибути Range/Min допомагають налаштовувати Inspector.
        [SerializeField, Range(1, 180)] private float maximumYaw = 90;
        [SerializeField, Min(1)] private float degreesPerScreen = 180;
        [SerializeField, Min(1)] private float aimSpeed = 720;
        [SerializeField, Min(0.01f)] private float aimSmoothTime = 0.06f;
        [SerializeField, Min(0.1f)] private float shotsPerSecond = 6;
        private float targetYaw;
        // Бажаний і видимий кути розділені: ввід реагує відразу, модель повертається плавно.
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
            // Зміна активності публікується лише раз: ефекти вмикають/прибирають лінію прицілу.
            if (isActive == value) return;
            isActive = value;
            if (!value) yawVelocity = 0;
            ActiveChanged?.Invoke(value);
        }
        public void Initialize(ProjectilePool pool, IReadOnlyList<EnemyAI> targets, ExplosiveBarrel explosive = null)
        {
            // Зберігаємо посилання на той самий список Spawner, який він наповнює при рестарті.
            // IReadOnlyList не дозволяє турелі самостійно додавати або прибирати ворогів.
            projectilePool = pool;
            enemies = targets;
            barrel = explosive;
        }

        public void Aim(float normalizedDelta)
        {
            if (!isActive) return;
            pointerAim = false;
            // Touch/тестовий ввід задає відносну зміну кута, не абсолютні координати.
            float value = targetYaw + normalizedDelta * degreesPerScreen;
            SetTargetYaw(value);
        }

        public void AimAtScreenPosition(Vector2 screenPosition)
        {
            // Координати зберігаються до Update: точку в світі переобчислюємо
            // і при нерухомій миші, адже авто та камера продовжують рухатися.
            if (!isActive) return;
            pointerPosition = screenPosition;
            pointerAim = true;
        }

        // При дозволених 180° кут обертається по колу без упору в межу ±180°.
        // Для меншого сектора той самий компонент може обмежувати наведення Clamp.
        private void SetTargetYaw(float value) => targetYaw = maximumYaw >= 179.9f
            ? Mathf.Repeat(value + 180, 360) - 180 : Mathf.Clamp(value, -maximumYaw, maximumYaw);

        public void ResetAim()
        {
            // Скидаємо не лише кут, а й швидкість згладжування та таймер стрільби,
            // інакше новий заїзд успадкує рух або затримку від попереднього.
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
            // Кутова версія SmoothDamp враховує перехід через 0/360 та обирає короткий поворот.
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
            // Якщо курсор безпосередньо над ціллю, беремо центр її колайдера:
            // проєкція лише на землю давала б інший кут, особливо впритул до авто.
            RaycastHit hit = default;
            bool onTarget = projectilePool && Physics.Raycast(ray, out hit, aimCamera.farClipPlane,
                projectilePool.TargetLayers, QueryTriggerInteraction.Collide);
            if (onTarget) point = hit.collider.bounds.center;
            else
            {
                // Поза ціллю промінь перетинає допоміжну горизонтальну площину.
                // Це дає зрозумілий напрям миші на 360°, включно з пострілами назад.
                var plane = new Plane(Vector3.up, Vector3.up * 0.9f);
                if (!plane.Raycast(ray, out float distance)) return;
                point = ray.GetPoint(distance);
            }
            Vector3 direction = transform.parent ? transform.parent.InverseTransformDirection(point - BarrelOrigin) : point - BarrelOrigin;
            // Vector3 тут є напрямом, а не точкою: віднімання позицій прибирає початок координат.
            // y = 0 лишає тільки площину дороги; висотою окремо займається elevationPivot.
            direction.y = 0;
            // Біля самої осі маленька зміна координат різко міняла б кут;
            // для явного попадання курсора в ціль залишаємо меншу мертву зону.
            if (direction.sqrMagnitude < (onTarget ? 0.0001f : 0.04f)) return;
            SetTargetYaw(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
        }

        private void UpdateElevation()
        {
            if (!elevationPivot || enemies == null) return;
            float pitch = 8;
            float closest = 40;
            // Допомагаємо лише з висотою найближчої цілі в уже обраному напрямку.
            // Це не автоповорот до будь-якого ворога збоку й не самонавідні кулі.
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
            // Коридор враховує радіус і цілі, і кулі. Перевірка лише центра
            // пропускала б близького ворога при наведенні на край його тіла.
            float hitCorridor = targetRadius + ShotRadius;
            if (point.z <= 0.02f || point.z >= closest || Mathf.Abs(point.x) > hitCorridor) return;
            // Працюємо в локальному просторі повернутої турелі; нахил вниз
            // дозволяє влучити у ворога нижче ствола навіть біля борту машини.
            closest = point.z;
            pitch = Mathf.Clamp(-Mathf.Atan2(point.y, point.z) * Mathf.Rad2Deg, 0, 85);
        }

        private void FixedUpdate()
        {
            if (!isActive || !muzzle || !projectilePool) return;
            cooldown -= Time.fixedDeltaTime;
            // fixedDeltaTime — секунди одного фізичного кроку, не FPS і не час від початку гри.
            if (cooldown > 0) return;
            // Додаємо інтервал до залишку, а не обнуляємо його: так не втрачається
            // дробова частина часу між пострілами на дискретних фізичних кроках.
            cooldown += 1f / shotsPerSecond;
            projectilePool.Fire(muzzle.position, muzzle.forward, BarrelOrigin);
            // Ефекти та звук реагують на факт пострілу, а не дублюють його таймер.
            Fired?.Invoke();
        }
    }
}
