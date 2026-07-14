using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(InputReader))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO _config;

    private Rigidbody2D _rigidbody;
    private InputReader _inputReader;

    private Vector2 _moveDirection;
    private bool _isSprinting;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _inputReader = GetComponent<InputReader>();
    }

    private void OnEnable()
    {
        _inputReader.OnMove += HandleMove;
        _inputReader.OnSprint += HandleSprint;
    }

    private void OnDisable()
    {
        _inputReader.OnMove -= HandleMove;
        _inputReader.OnSprint -= HandleSprint;
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void HandleMove(Vector2 direction)
    {
        _moveDirection = direction;
    }

    private void HandleSprint(bool isSprinting)
    {
        _isSprinting = isSprinting;
    }

    private void ApplyMovement()
    {
        if (_config == null) return;

        Vector2 normalized = _moveDirection;
        if (normalized.magnitude > _config.DiagonalNormalizeThreshold)
        {
            normalized = normalized.normalized;
        }

        float speed = _config.MoveSpeed;
        if (_isSprinting)
        {
            speed *= _config.SprintMultiplier;
        }

        Vector2 velocity = normalized * speed;
        _rigidbody.linearVelocity = velocity;
    }

    public Vector2 GetFacingDirection()
    {
        if (_moveDirection.magnitude > 0.1f)
        {
            return _moveDirection.normalized;
        }

        return Vector2.down;
    }

    public bool IsMoving => _moveDirection.magnitude > 0.1f;
}
