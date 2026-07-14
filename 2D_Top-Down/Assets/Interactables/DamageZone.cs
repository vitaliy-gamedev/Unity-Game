using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int _damagePerTick = 1;
    [SerializeField] private float _tickInterval = 1f;
    [SerializeField] private LayerMask _targetLayer;

    private Coroutine _damageCoroutine;
    private PlayerHealth _targetHealth;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsTargetLayer(other.gameObject)) return;

        _targetHealth = other.GetComponent<PlayerHealth>();
        if (_targetHealth == null) return;

        if (_damageCoroutine == null)
        {
            _damageCoroutine = StartCoroutine(DamageCoroutine());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsTargetLayer(other.gameObject)) return;

        if (other.GetComponent<PlayerHealth>() == _targetHealth)
        {
            _targetHealth = null;

            if (_damageCoroutine != null)
            {
                StopCoroutine(_damageCoroutine);
                _damageCoroutine = null;
            }
        }
    }

    private IEnumerator DamageCoroutine()
    {
        while (_targetHealth != null)
        {
            _targetHealth.TakeDamage(_damagePerTick);
            yield return new WaitForSeconds(_tickInterval);
        }

        _damageCoroutine = null;
    }

    private bool IsTargetLayer(GameObject obj)
    {
        return (_targetLayer.value & (1 << obj.layer)) != 0;
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

        if (col is BoxCollider2D box)
            Gizmos.DrawCube(box.bounds.center, box.bounds.size);
        else if (col is CircleCollider2D circle)
            Gizmos.DrawSphere(circle.bounds.center, circle.radius);
    }
}
