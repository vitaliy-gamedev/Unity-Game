using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float invulnerabilityTime = 2f;

    private PlayerHealth health;
    private PlayerRegeneration regeneration;
    private float invulnerability;

    public bool IsInvulnerable => invulnerability > 0f;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        regeneration = GetComponent<PlayerRegeneration>();
    }

    private void OnEnable()
    {
        health.OnDeath += Respawn;
    }

    private void OnDisable()
    {
        health.OnDeath -= Respawn;
    }

    private void Update()
    {
        if (invulnerability > 0f)
            invulnerability -= Time.deltaTime;
    }

    private void Respawn()
    {
        if (respawnPoint == null)
        {
            Debug.LogWarning("Respawn Point is not assigned!");
            return;
        }

        CharacterController controller =
            GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        transform.SetPositionAndRotation(
            respawnPoint.position,
            respawnPoint.rotation
        );

        if (controller != null)
            controller.enabled = true;

        health.FullHeal();

        invulnerability = invulnerabilityTime;

        if (regeneration != null)
            regeneration.ResetTimer();

        Debug.Log("Player respawned!");
    }
}