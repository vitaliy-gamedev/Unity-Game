using UnityEngine;

/// <summary>
/// Simple waypoint patrol for ground targets.
/// Assign waypoints array in Inspector or it will auto-create a small patrol loop.
/// </summary>
public class TargetMovement : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] waypoints;
    public float       moveSpeed    = 1.5f;
    public float       waypointTolerance = 0.5f;
    public bool        randomizeOrder    = false;

    private int   _currentWP = 0;
    private bool  _generated = false;

    void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
            GenerateLocalWaypoints();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[_currentWP];
        Vector3   dir    = (target.position - transform.position);
        dir.y = 0;

        if (dir.magnitude < waypointTolerance)
        {
            NextWaypoint();
            return;
        }

        transform.position += dir.normalized * moveSpeed * Time.deltaTime;
        transform.rotation  = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir.normalized), 5f * Time.deltaTime);
    }

    void NextWaypoint()
    {
        if (randomizeOrder)
            _currentWP = Random.Range(0, waypoints.Length);
        else
            _currentWP = (_currentWP + 1) % waypoints.Length;
    }

    void GenerateLocalWaypoints()
    {
        _generated = true;
        waypoints  = new Transform[4];
        float r    = Random.Range(5f, 12f);

        for (int i = 0; i < 4; i++)
        {
            var wp = new GameObject($"WP_{name}_{i}").transform;
            float angle    = i * 90f * Mathf.Deg2Rad;
            wp.position    = transform.position + new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
            waypoints[i]   = wp;
        }
    }

    void OnDestroy()
    {
        if (!_generated) return;
        foreach (var wp in waypoints)
            if (wp != null) Destroy(wp.gameObject);
    }
}
