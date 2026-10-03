using UnityEngine;

namespace Deadband.AI
{
    [CreateAssetMenu(menuName = "DEADBAND/AI/Enemy Data", fileName = "ED_Humanoid")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0.1f)] private float patrolSpeed = 2.1f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 4.2f;
        [SerializeField, Min(1f)] private float visionRange = 15f;
        [SerializeField, Range(1f, 180f)] private float visionAngle = 92f;
        [SerializeField, Min(0.1f)] private float hearingMultiplier = 1f;
        [SerializeField, Min(0.05f)] private float reactionTime = 0.55f;
        [SerializeField, Min(0.1f)] private float searchDuration = 6f;
        [SerializeField, Min(1f)] private float weaponRange = 17f;
        [SerializeField, Min(0.1f)] private float weaponDamage = 8f;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.85f;

        public float MaximumHealth => maximumHealth;
        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float VisionRange => visionRange;
        public float VisionAngle => visionAngle;
        public float HearingMultiplier => hearingMultiplier;
        public float ReactionTime => reactionTime;
        public float SearchDuration => searchDuration;
        public float WeaponRange => weaponRange;
        public float WeaponDamage => weaponDamage;
        public float FireInterval => fireInterval;

        public void Configure()
        {
            maximumHealth = 100f;
            patrolSpeed = 2.1f;
            chaseSpeed = 4.2f;
            visionRange = 15f;
            visionAngle = 92f;
            hearingMultiplier = 1f;
            reactionTime = 0.55f;
            searchDuration = 6f;
            weaponRange = 17f;
            weaponDamage = 8f;
            fireInterval = 0.85f;
        }
    }
}
