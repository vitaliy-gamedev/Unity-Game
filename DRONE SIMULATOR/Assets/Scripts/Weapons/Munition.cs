using UnityEngine;
using System.Collections;

/// <summary>
/// Attached at runtime to a dropped munition object.
/// Optionally homes toward a TargetData world position.
/// </summary>
public class Munition : MonoBehaviour
{
    [Header("Physics")]
    public float gravity        = 9.8f;
    public float homingStrength = 0f;   // 0 = dumb bomb, >0 = guided
    public float lifetime       = 10f;

    [Header("Explosion")]
    public float blastRadius   = 8f;
    public GameObject explosionVFXPrefab;   // optional particle system

    private TargetData                    _target;
    private System.Action<TargetData>     _onLanded;
    private Vector3                       _velocity;
    private bool                          _exploded;

    public void Init(TargetData target, System.Action<TargetData> onLanded, float initialForwardSpeed = 5f)
    {
        _target   = target;
        _onLanded = onLanded;
        _velocity = Vector3.forward * initialForwardSpeed;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (_exploded) return;

        // Gravity
        _velocity.y -= gravity * Time.deltaTime;

        // Optional homing
        if (_target != null && homingStrength > 0f)
        {
            Vector3 dir = (_target.worldPosition - transform.position).normalized;
            _velocity = Vector3.Lerp(_velocity, dir * _velocity.magnitude, homingStrength * Time.deltaTime);
        }

        transform.position += _velocity * Time.deltaTime;

        // Orient to velocity
        if (_velocity.sqrMagnitude > 0.01f)
            transform.forward = _velocity.normalized;
    }

    void OnCollisionEnter(Collision col)
    {
        if (_exploded) return;
        Explode(col.contacts[0].point);
    }

    // Fallback: if no physics collision (terrain may not have collider set up)
    void Update_AltGroundCheck()
    {
        if (_exploded && transform.position.y <= 0.5f)
            Explode(transform.position);
    }

    void LateUpdate()
    {
        if (!_exploded && transform.position.y <= 0.2f)
            Explode(transform.position);
    }

    void Explode(Vector3 point)
    {
        _exploded = true;

        // VFX
        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, point, Quaternion.identity);
        else
            CreateSimpleExplosion(point);

        // Damage nearby targets
        TargetData hitTarget = null;
        if (_target != null && Vector3.Distance(point, _target.worldPosition) < blastRadius)
            hitTarget = _target;

        _onLanded?.Invoke(hitTarget);

        Destroy(gameObject);
    }

    void CreateSimpleExplosion(Vector3 pos)
    {
        // Flash sphere
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position   = pos;
        sphere.transform.localScale = Vector3.one * 0.5f;
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.5f, 0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(2f, 1f, 0f));
        sphere.GetComponent<Renderer>().material = mat;
        Destroy(sphere.GetComponent<Collider>());

        // Animate scale up then destroy
        sphere.AddComponent<ExplosionAnimator>();
    }
}

/// <summary>Simple explosion scale-up animation.</summary>
public class ExplosionAnimator : MonoBehaviour
{
    float _t;
    void Update()
    {
        _t += Time.deltaTime * 3f;
        float s = Mathf.Lerp(0.5f, blastRadius, _t);
        transform.localScale = Vector3.one * s;
        var r = GetComponent<Renderer>();
        if (r != null) r.material.color = Color.Lerp(new Color(1f, 0.5f, 0f, 1f), new Color(0.2f, 0.2f, 0.2f, 0f), _t);
        if (_t >= 1f) Destroy(gameObject);
    }
    float blastRadius = 8f;
}
