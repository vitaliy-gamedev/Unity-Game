using UnityEngine;

[CreateAssetMenu(menuName = "Config/Player Config", fileName = "PlayerConfig")]
public class PlayerConfigSO : ScriptableObject
{
    [Header("Movement")] [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintMultiplier = 1.5f;
    [SerializeField] private float _diagonalNormalizeThreshold = 0.1f;

    [Header("Health")] [SerializeField] private int _maxHealth = 3;
    [SerializeField] private float _invulnerabilityDuration = 1f;

    [Header("Interaction")] [SerializeField]
    private float _interactionRadius = 1.5f;

    public float MoveSpeed => _moveSpeed;
    public float SprintMultiplier => _sprintMultiplier;
    public float DiagonalNormalizeThreshold => _diagonalNormalizeThreshold;
    public int MaxHealth => _maxHealth;
    public float InvulnerabilityDuration => _invulnerabilityDuration;
    public float InteractionRadius => _interactionRadius;
}