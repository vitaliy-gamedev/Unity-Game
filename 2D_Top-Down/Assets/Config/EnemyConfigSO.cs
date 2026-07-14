using UnityEngine;

[CreateAssetMenu(menuName = "Config/Enemy Config", fileName = "EnemyConfig")]
public class EnemyConfigSO : ScriptableObject
{
    [Header("Patrol")] [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _patrolRange = 3f;
    [SerializeField] private float _waitTimeAtPoint = 1f;

    [Header("Chase")] [SerializeField] private float _chaseSpeed = 3.5f;
    [SerializeField] private float _detectionRadius = 5f;
    [SerializeField] private float _losePlayerRadius = 8f;

    [Header("Attack")] [SerializeField] private int _damage = 1;
    [SerializeField] private float _attackCooldown = 1f;
    [SerializeField] private float _attackRange = 0.5f;

    [Header("Health")] [SerializeField] private int _maxHealth = 2;

    [Header("Score")] [SerializeField] private int _scoreReward = 100;

    public float PatrolSpeed => _patrolSpeed;
    public float PatrolRange => _patrolRange;
    public float WaitTimeAtPoint => _waitTimeAtPoint;
    public float ChaseSpeed => _chaseSpeed;
    public float DetectionRadius => _detectionRadius;
    public float LosePlayerRadius => _losePlayerRadius;
    public int Damage => _damage;
    public float AttackCooldown => _attackCooldown;
    public float AttackRange => _attackRange;
    public int MaxHealth => _maxHealth;
    public int ScoreReward => _scoreReward;
}