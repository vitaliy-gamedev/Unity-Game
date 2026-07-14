using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")] [SerializeField] private Transform _target;

    [Header("Follow Settings")] [SerializeField]
    private float _smoothSpeed = 5f;

    [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

    [Header("Bounds")] [SerializeField] private bool _hasBounds = false;
    [SerializeField] private Vector2 _minBounds;
    [SerializeField] private Vector2 _maxBounds;

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 targetPosition = _target.position + _offset;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, _smoothSpeed * Time.deltaTime);

        if (_hasBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, _minBounds.x, _maxBounds.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, _minBounds.y, _maxBounds.y);
        }

        transform.position = smoothedPosition;
    }

    private void Start()
    {
        if (_target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _target = player.transform;
        }
    }

    public void SetBounds(Vector2 min, Vector2 max)
    {
        _minBounds = min;
        _maxBounds = max;
        _hasBounds = true;
    }

    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_hasBounds) return;

        Gizmos.color = Color.green;
        Vector3 center = (_minBounds + _maxBounds) / 2f;
        Vector3 size = _maxBounds - _minBounds;
        Gizmos.DrawWireCube(center, size);
    }
}