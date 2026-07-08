using System.Collections.Generic;
using UnityEngine;

public class ExplosionDamage : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float _radius = 5f;
    [SerializeField] private float _maxDamage = 100f;
    [SerializeField] private float _explosionForce = 700f;
    [SerializeField] private LayerMask _targetLayers = -1;

    [Header("Effects")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [SerializeField] private AudioClip _explosionSound;

    [Header("Settings")]
    [SerializeField] private bool _explodeOnStart = false;
    [SerializeField] private bool _destroyAfterExplosion = true;
    [SerializeField] private float _delayBeforeExplosion = 0f;

    private bool _hasExploded = false;

    private void Start()
    {
        if (_explodeOnStart)
            Explode();
    }

    public void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (_explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        if (_explosionSound != null && TryGetComponent(out AudioSource source))
            source.PlayOneShot(_explosionSound);

        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _targetLayers);
        HashSet<DestructibleObject> processed = new HashSet<DestructibleObject>();

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(transform.position, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(distance / _radius);

            Vector3 direction = (hit.transform.position - transform.position).normalized;
            if (direction.magnitude < 0.01f)
                direction = Vector3.up;

            DestructibleObject destructible = hit.GetComponentInParent<DestructibleObject>();
            if (destructible != null && !processed.Contains(destructible))
            {
                processed.Add(destructible);
                float damage = _maxDamage * falloff;
                Vector3 force = direction * _explosionForce * falloff;
                destructible.TakeDamage(damage, transform.position, force);
            }

            if (hit.TryGetComponent(out Rigidbody rb) && destructible == null)
            {
                rb.AddExplosionForce(_explosionForce, transform.position, _radius, 1f, ForceMode.Impulse);
            }
        }

        if (_destroyAfterExplosion)
            Destroy(gameObject, 2f);
    }

    public void ExplodeDelayed(float delay)
    {
        Invoke(nameof(Explode), delay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, _radius);
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
