using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private Vector3 zoneSize = new Vector3(5f, 2f, 5f);
    [SerializeField] private LayerMask playerLayer;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageInterval = 1f;

    [Header("Danger Escalation")]
    [SerializeField] private bool increaseDamageOverTime = true;
    [SerializeField] private float damageIncreasePerSecond = 2f;
    [SerializeField] private float maxDamage = 50f;

    private float damageTimer;
    private float timeInsideZone;

    private PlayerHealth playerHealth;

    private void Update()
    {
        CheckForPlayer();
    }

    private void CheckForPlayer()
    {
        Collider[] objectsInsideZone = Physics.OverlapBox(
            transform.position,
            zoneSize / 2f,
            transform.rotation,
            playerLayer
        );

        bool playerFound = false;

        foreach (Collider objectCollider in objectsInsideZone)
        {
            PlayerHealth health = objectCollider.GetComponentInParent<PlayerHealth>();

            if (health == null)
                continue;

            playerFound = true;

            if (playerHealth != health)
            {
                playerHealth = health;
                damageTimer = 0f;
                timeInsideZone = 0f;
            }

            break;
        }

        if (playerFound)
        {
            HandlePlayerInside();
        }
        else
        {
            HandlePlayerOutside();
        }
    }

    private void HandlePlayerInside()
    {
        timeInsideZone += Time.deltaTime;
        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            float currentDamage = damage;

            if (increaseDamageOverTime)
            {
                currentDamage += damageIncreasePerSecond * timeInsideZone;

                currentDamage = Mathf.Clamp(
                    currentDamage,
                    0f,
                    maxDamage
                );
            }

            playerHealth.TakeDamage(currentDamage);

            damageTimer = 0f;
        }
    }

    private void HandlePlayerOutside()
    {
        damageTimer = 0f;
        timeInsideZone = 0f;
        playerHealth = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            zoneSize
        );
    }
}