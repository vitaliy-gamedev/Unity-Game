using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerRegeneration : MonoBehaviour
{
    [SerializeField] private float delay = 5f;
    [SerializeField] private float healPerSecond = 10f;
    [SerializeField] private float movementThreshold = 0.01f;

    private PlayerHealth health;
    private Vector3 lastPosition;
    private float timer;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (health.IsDead) return;

        float distance = Vector3.Distance(
            transform.position,
            lastPosition
        );

        if (distance > movementThreshold)
            timer = 0f;
        else
            timer += Time.deltaTime;

        lastPosition = transform.position;

        if (timer >= delay && health.CurrentHealth < health.MaxHealth)
        {
            float oldHealth = health.CurrentHealth;

            health.Heal(healPerSecond * Time.deltaTime);

            Debug.Log(
                $"Player regenerating: {oldHealth:F1} → {health.CurrentHealth:F1} HP"
            );
        }
    }

    public void ResetTimer()
    {
        timer = 0f;
        lastPosition = transform.position;
    }
}