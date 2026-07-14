using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public class EnemyBase : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private EnemyConfigSO _config;

    [Header("Patrol Points")]
    [SerializeField] private Transform _pointA;
    [SerializeField] private Transform _pointB;

    [Header("Attack")]
    [SerializeField] private Transform _attackPoint;
    [SerializeField] private LayerMask _playerLayer;

    private Rigidbody2D _rigidbody;
    private Transform _playerTransform;
    private EnemyHealth _health;

    private EnemyState _currentState = EnemyState.Patrol;
    private Vector2 _targetPatrolPoint;
    private bool _movingToB = true;
    private float _lastAttackTime;

    public EnemyConfigSO Config => _config;
    public Transform PlayerTransform => _playerTransform;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        if (_pointA != null)
            transform.position = _pointA.position;

        _targetPatrolPoint = _pointB != null ? _pointB.position : (Vector2)transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _playerTransform = playerObj.transform;
    }

    private void Update()
    {
        if (_health != null && _health.IsDead) return;

        EvaluateState();
    }

    private void FixedUpdate()
    {
        if (_health != null && _health.IsDead) return;

        ExecuteState();
    }

    private void EvaluateState()
    {
        if (_playerTransform == null || _config == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

        if (distanceToPlayer <= _config.AttackRange)
        {
            _currentState = EnemyState.Attack;
        }
        else if (distanceToPlayer <= _config.DetectionRadius)
        {
            _currentState = EnemyState.Chase;
        }
        else
        {
            _currentState = EnemyState.Patrol;
        }
    }

    private void ExecuteState()
    {
        switch (_currentState)
        {
            case EnemyState.Patrol: PatrolBehavior(); break;
            case EnemyState.Chase:  ChaseBehavior(); break;
            case EnemyState.Attack: AttackBehavior(); break;
        }
    }

    private void PatrolBehavior()
    {
        if (_config == null) return;

        Vector2 currentPos = transform.position;
        float distance = Vector2.Distance(currentPos, _targetPatrolPoint);

        if (distance < 0.5f)
        {
            _movingToB = !_movingToB;
            _targetPatrolPoint = _movingToB
                ? (_pointB != null ? _pointB.position : currentPos)
                : (_pointA != null ? _pointA.position : currentPos);
        }

        Vector2 direction = (_targetPatrolPoint - currentPos).normalized;
        _rigidbody.linearVelocity = direction * _config.PatrolSpeed;

        FlipSprite(direction.x);
    }

    private void ChaseBehavior()
    {
        if (_config == null || _playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);

        if (distance > _config.LosePlayerRadius)
        {
            _currentState = EnemyState.Patrol;
            return;
        }

        Vector2 direction = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
        _rigidbody.linearVelocity = direction * _config.ChaseSpeed;

        FlipSprite(direction.x);
    }

    private void AttackBehavior()
    {
        if (_config == null || _playerTransform == null) return;

        _rigidbody.linearVelocity = Vector2.zero;

        if (Time.time < _lastAttackTime + _config.AttackCooldown) return;
        _lastAttackTime = Time.time;

        float distance = Vector2.Distance(transform.position, _playerTransform.position);
        if (distance > _config.AttackRange)
        {
            _currentState = EnemyState.Chase;
            return;
        }

        Collider2D hit = Physics2D.OverlapCircle(
            _attackPoint != null ? _attackPoint.position : transform.position,
            _config.AttackRange,
            _playerLayer
        );

        if (hit != null)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(_config.Damage);
            }
        }
    }

    private void FlipSprite(float directionX)
    {
        if (directionX != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = directionX > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_config == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _config.DetectionRadius);

        Gizmos.color = Color.red;
        Vector3 attackPos = _attackPoint != null ? _attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(attackPos, _config.AttackRange);
    }
}
