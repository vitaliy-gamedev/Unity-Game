using UnityEngine;

public class GroundBounds : MonoBehaviour
{
    public static GroundBounds Instance { get; private set; }

    private Vector3 _min;
    private Vector3 _max;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CalculateBounds();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void CalculateBounds()
    {
        var center = transform.position;
        var meshFilter = GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            var meshBounds = meshFilter.sharedMesh.bounds;
            var worldSize = Vector3.Scale(meshBounds.size, transform.lossyScale);

            _min = center - new Vector3(worldSize.x * 0.5f, center.y, worldSize.z * 0.5f);
            _max = center + new Vector3(worldSize.x * 0.5f, center.y, worldSize.z * 0.5f);
        }
        else
        {
            _min = center - new Vector3(10f, 0f, 10f);
            _max = center + new Vector3(10f, 0f, 10f);
        }
    }

    public Vector3 ClampPosition(Vector3 position)
    {
        if (this == null)
            return position;

        position.x = Mathf.Clamp(position.x, _min.x, _max.x);
        position.z = Mathf.Clamp(position.z, _min.z, _max.z);
        return position;
    }

    private void OnDrawGizmosSelected()
    {
        CalculateBounds();
        Gizmos.color = Color.yellow;

        var center = new Vector3((_min.x + _max.x) * 0.5f, transform.position.y, (_min.z + _max.z) * 0.5f);
        var size = new Vector3(_max.x - _min.x, 0.1f, _max.z - _min.z);

        Gizmos.DrawWireCube(center, size);
    }
}