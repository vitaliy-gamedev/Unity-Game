using UnityEngine;

public class CollisionDamage : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float _baseDamage = 20f;
    [SerializeField] private float _forceMultiplier = 0.1f;
    [SerializeField] private float _minRelativeVelocity = 2f;

    [Header("Projectile")]
    [SerializeField] private bool _destroyOnHit = true;
    [SerializeField] private GameObject _hitEffectPrefab;
    [SerializeField] private AudioClip _hitSound;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        float relativeVelocity = collision.relativeVelocity.magnitude;
        if (relativeVelocity < _minRelativeVelocity) return;

        DestructibleObject destructible = collision.gameObject.GetComponentInParent<DestructibleObject>();

        if (destructible != null)
        {
            float damage = _baseDamage + (relativeVelocity * _forceMultiplier);

            Vector3 hitPoint = collision.contacts.Length > 0
                ? collision.contacts[0].point
                : collision.transform.position;

            float mass = _rb != null ? _rb.mass : 1f;
            Vector3 hitForce = collision.relativeVelocity * mass;

            destructible.TakeDamage(damage, hitPoint, hitForce);
        }

        Rigidbody hitRb = collision.gameObject.GetComponent<Rigidbody>();
        if (hitRb != null && destructible == null)
        {
            float mass = _rb != null ? _rb.mass : 1f;
            Vector3 force = collision.relativeVelocity * mass * 0.5f;
            hitRb.AddForceAtPosition(force, collision.contacts[0].point, ForceMode.Impulse);
        }

        if (_hitEffectPrefab != null && collision.contacts.Length > 0)
        {
            Vector3 point = collision.contacts[0].point;
            Quaternion rotation = Quaternion.LookRotation(collision.contacts[0].normal);
            GameObject effect = Instantiate(_hitEffectPrefab, point, rotation);
            Destroy(effect, 2f);
        }

        if (_hitSound != null && collision.contacts.Length > 0)
            AudioSource.PlayClipAtPoint(_hitSound, collision.contacts[0].point);

        if (_destroyOnHit)
            Destroy(gameObject);
    }

    public void DealDamage(GameObject target, float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (target == null) return;

        DestructibleObject destructible = target.GetComponentInParent<DestructibleObject>();
        if (destructible != null)
            destructible.TakeDamage(damage, hitPoint, hitDirection * damage * 2f);

        if (_hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(_hitEffectPrefab, hitPoint, Quaternion.LookRotation(hitDirection));
            Destroy(effect, 2f);
        }

        if (_destroyOnHit)
            Destroy(gameObject);
    }
}
