using System;
using System.Collections;
using Deadband.AudioSystem;
using Deadband.Squad;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Deadband.Combat
{
    [DefaultExecutionOrder(-600)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerUnitController))]
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private PlayerUnitController playerController;
        [SerializeField] private Transform muzzle;
        [SerializeField] private WeaponData[] loadout;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private Light muzzleFlash;

        private int[] magazineAmmo;
        private int[] reserveAmmo;
        private int currentWeaponIndex;
        private float nextFireTime;
        private bool isReloading;
        private bool suppressFireUntilRelease;
        private Coroutine reloadRoutine;
        private Coroutine shotEffectRoutine;
        private WeaponAudioEmitter weaponAudio;
        private PlayerCombatFeedback combatFeedback;

        public event Action<int, WeaponData> WeaponChanged;
        public event Action Fired;
        public event Action ReloadStarted;

        public WeaponData CurrentWeapon => loadout != null && loadout.Length > 0
            ? loadout[currentWeaponIndex]
            : null;
        public int CurrentWeaponIndex => currentWeaponIndex;
        public Vector3 LastShotEndPoint { get; private set; }
        public Vector3 LastShotAimPoint { get; private set; }
        public int MagazineAmmo => magazineAmmo != null && magazineAmmo.Length > currentWeaponIndex
            ? magazineAmmo[currentWeaponIndex]
            : 0;
        public int ReserveAmmo => reserveAmmo != null && reserveAmmo.Length > currentWeaponIndex
            ? reserveAmmo[currentWeaponIndex]
            : 0;
        public bool IsReloading => isReloading;
        public bool IsLowAmmo => CurrentWeapon != null && MagazineAmmo > 0 &&
                                 MagazineAmmo <= Mathf.Max(3, Mathf.CeilToInt(CurrentWeapon.MagazineSize * 0.25f));

        public bool RefillReserveAmmo(float fraction)
        {
            if (loadout == null || reserveAmmo == null || fraction <= 0f) return false;
            bool changed = false;
            for (int index = 0; index < loadout.Length; index++)
            {
                int capacity = loadout[index].StartingReserveAmmo;
                int restored = Mathf.Max(1, Mathf.CeilToInt(capacity * fraction));
                int next = Mathf.Min(capacity, reserveAmmo[index] + restored);
                changed |= next != reserveAmmo[index];
                reserveAmmo[index] = next;
            }
            return changed;
        }

        public void Configure(
            Camera cameraReference,
            PlayerUnitController movementController,
            Transform muzzleTransform,
            WeaponData[] weapons,
            LayerMask weaponHitMask,
            LineRenderer tracerRenderer,
            Light flash)
        {
            gameplayCamera = cameraReference;
            playerController = movementController;
            muzzle = muzzleTransform;
            loadout = weapons;
            hitMask = weaponHitMask;
            tracer = tracerRenderer;
            muzzleFlash = flash;
        }

        public void SetMuzzle(Transform muzzleTransform)
        {
            if (muzzleTransform != null)
            {
                muzzle = muzzleTransform;
            }
        }

        private void Awake()
        {
            weaponAudio = GetComponent<WeaponAudioEmitter>();
            combatFeedback = GetComponent<PlayerCombatFeedback>();
            if (combatFeedback == null)
            {
                combatFeedback = gameObject.AddComponent<PlayerCombatFeedback>();
            }
            if (playerController == null)
            {
                playerController = GetComponent<PlayerUnitController>();
            }

            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (tracer != null)
            {
                tracer.enabled = false;
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.enabled = false;
            }

            InitializeAmmo();
        }

        private void Update()
        {
            if (CurrentWeapon == null || Time.timeScale <= 0f ||
                Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    SwitchWeapon(0);
                }
                else if (keyboard.digit2Key.wasPressedThisFrame)
                {
                    SwitchWeapon(1);
                }

                if (keyboard.rKey.wasPressedThisFrame)
                {
                    StartReload();
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            {
                return;
            }

            if (suppressFireUntilRelease)
            {
                if (!mouse.leftButton.isPressed) suppressFireUntilRelease = false;
                return;
            }

            bool wantsToFire = CurrentWeapon.Automatic
                ? mouse.leftButton.isPressed
                : mouse.leftButton.wasPressedThisFrame;
            if (wantsToFire)
            {
                TryFire();
            }
        }

        public void SuppressFireUntilRelease()
        {
            suppressFireUntilRelease = true;
        }

        private void InitializeAmmo()
        {
            int weaponCount = loadout?.Length ?? 0;
            magazineAmmo = new int[weaponCount];
            reserveAmmo = new int[weaponCount];
            for (int index = 0; index < weaponCount; index++)
            {
                magazineAmmo[index] = loadout[index].MagazineSize;
                reserveAmmo[index] = loadout[index].StartingReserveAmmo;
            }
        }

        public bool TryFire()
        {
            if (isReloading || Time.time < nextFireTime)
            {
                return false;
            }

            if (magazineAmmo[currentWeaponIndex] <= 0)
            {
                nextFireTime = Time.time + Mathf.Max(0.18f, CurrentWeapon.ShotInterval);
                weaponAudio?.PlayDryFire();
                combatFeedback?.NotifyDryFire();
                StartReload();
                return false;
            }

            if (muzzle == null || playerController == null || !playerController.HasAimPoint)
            {
                return false;
            }

            magazineAmmo[currentWeaponIndex]--;
            nextFireTime = Time.time + CurrentWeapon.ShotInterval;
            CombatNoiseSystem.Emit(transform.position, 28f, gameObject);
            Fired?.Invoke();
            weaponAudio?.PlayShot(currentWeaponIndex == 1);
            combatFeedback?.NotifyShot(currentWeaponIndex == 1);

            Vector3 origin = muzzle.position;
            LastShotAimPoint = playerController.AimPoint;
            Vector3 direction = LastShotAimPoint - origin;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = transform.forward;
            }
            direction.Normalize();

            Vector3 endPoint = origin + direction * CurrentWeapon.Range;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                CurrentWeapon.Range,
                hitMask,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<SquadUnit>() != null)
                {
                    continue;
                }

                endPoint = hit.point;
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    Health targetHealth = hit.collider.GetComponentInParent<Health>();
                    damageable.ApplyDamage(CurrentWeapon.Damage, hit.point, direction);
                    combatFeedback?.NotifyHit(targetHealth != null && !targetHealth.IsAlive, hit.point);
                }
                else
                {
                    combatFeedback?.NotifyWorldImpact(hit.point);
                }
                break;
            }

            LastShotEndPoint = endPoint;

            if (shotEffectRoutine != null)
            {
                StopCoroutine(shotEffectRoutine);
            }
            shotEffectRoutine = StartCoroutine(ShowShotEffect(origin, endPoint));
            return true;
        }

        private void StartReload()
        {
            if (isReloading || CurrentWeapon == null ||
                magazineAmmo[currentWeaponIndex] >= CurrentWeapon.MagazineSize ||
                reserveAmmo[currentWeaponIndex] <= 0)
            {
                return;
            }

            reloadRoutine = StartCoroutine(Reload());
            ReloadStarted?.Invoke();
            combatFeedback?.NotifyReloadStarted(CurrentWeapon.ReloadDuration);
        }

        private IEnumerator Reload()
        {
            isReloading = true;
            yield return new WaitForSeconds(CurrentWeapon.ReloadDuration);
            int needed = CurrentWeapon.MagazineSize - magazineAmmo[currentWeaponIndex];
            int loaded = Mathf.Min(needed, reserveAmmo[currentWeaponIndex]);
            magazineAmmo[currentWeaponIndex] += loaded;
            reserveAmmo[currentWeaponIndex] -= loaded;
            isReloading = false;
            reloadRoutine = null;
            combatFeedback?.NotifyReloadCompleted();
        }

        private void SwitchWeapon(int index)
        {
            if (loadout == null || index < 0 || index >= loadout.Length || index == currentWeaponIndex)
            {
                return;
            }

            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
                isReloading = false;
            }

            currentWeaponIndex = index;
            nextFireTime = Time.time + 0.12f;
            WeaponChanged?.Invoke(currentWeaponIndex, CurrentWeapon);
        }

        private IEnumerator ShowShotEffect(Vector3 origin, Vector3 endPoint)
        {
            if (tracer != null)
            {
                tracer.enabled = true;
                tracer.SetPosition(0, origin);
                tracer.SetPosition(1, endPoint);
            }
            if (muzzleFlash != null)
            {
                muzzleFlash.transform.position = origin;
                muzzleFlash.enabled = true;
            }

            yield return new WaitForSeconds(0.045f);

            if (tracer != null)
            {
                tracer.enabled = false;
            }
            if (muzzleFlash != null)
            {
                muzzleFlash.enabled = false;
            }
            shotEffectRoutine = null;
        }

        private void OnGUI()
        {
            if (CurrentWeapon == null || magazineAmmo == null || magazineAmmo.Length == 0)
            {
                return;
            }

            GUIStyle weaponStyle = new(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.62f, 1f, 0.73f) }
            };
            GUIStyle ammoStyle = new(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                normal =
                {
                    textColor = MagazineAmmo <= 0
                        ? new Color(1f, 0.16f, 0.08f)
                        : IsLowAmmo
                            ? Color.Lerp(
                                new Color(1f, 0.42f, 0.08f),
                                new Color(1f, 0.86f, 0.22f),
                                Mathf.PingPong(Time.unscaledTime * 3.2f, 1f))
                            : Color.white
                }
            };

            Rect panel = new(16f, Screen.height - 92f, 300f, 72f);
            Color previous = GUI.color;
            GUI.color = new Color(0.015f, 0.03f, 0.025f, 0.88f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(30f, Screen.height - 84f, 170f, 28f), CurrentWeapon.DisplayName, weaponStyle);
            GUI.Label(
                new Rect(150f, Screen.height - 83f, 150f, 36f),
                $"{magazineAmmo[currentWeaponIndex]:00} / {reserveAmmo[currentWeaponIndex]:000}",
                ammoStyle);
            GUI.Label(
                new Rect(30f, Screen.height - 52f, 250f, 24f),
                isReloading
                    ? "RELOADING..."
                    : MagazineAmmo <= 0
                        ? "EMPTY  //  R RELOAD"
                        : IsLowAmmo
                            ? "LOW AMMO  //  R RELOAD"
                            : $"SLOT {currentWeaponIndex + 1}",
                weaponStyle);
        }
    }
}
