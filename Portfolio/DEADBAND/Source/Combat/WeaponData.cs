using UnityEngine;

namespace Deadband.Combat
{
    [CreateAssetMenu(menuName = "DEADBAND/Combat/Weapon Data", fileName = "WD_NewWeapon")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string displayName = "Weapon";
        [SerializeField, Min(1f)] private float damage = 20f;
        [SerializeField, Min(0.01f)] private float shotsPerSecond = 5f;
        [SerializeField, Min(1)] private int magazineSize = 20;
        [SerializeField, Min(0)] private int startingReserveAmmo = 80;
        [SerializeField, Min(0.05f)] private float reloadDuration = 1.5f;
        [SerializeField, Min(1f)] private float range = 45f;
        [SerializeField] private bool automatic = true;

        public string DisplayName => displayName;
        public float Damage => damage;
        public float ShotInterval => 1f / shotsPerSecond;
        public int MagazineSize => magazineSize;
        public int StartingReserveAmmo => startingReserveAmmo;
        public float ReloadDuration => reloadDuration;
        public float Range => range;
        public bool Automatic => automatic;

        public void Configure(
            string weaponName,
            float weaponDamage,
            float fireRate,
            int magazine,
            int reserve,
            float reloadSeconds,
            float weaponRange,
            bool isAutomatic)
        {
            displayName = weaponName;
            damage = weaponDamage;
            shotsPerSecond = fireRate;
            magazineSize = magazine;
            startingReserveAmmo = reserve;
            reloadDuration = reloadSeconds;
            range = weaponRange;
            automatic = isAutomatic;
        }
    }
}
