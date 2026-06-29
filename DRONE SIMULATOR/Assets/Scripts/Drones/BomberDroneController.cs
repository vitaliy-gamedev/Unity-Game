using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Bomber drone (Vampir-type). Loads target list from GameManager.
/// Navigate over targets and press [SPACE] to drop munition.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BomberDroneController : MonoBehaviour
{
    [Header("Flight")]
    public float horizontalSpeed = 10f;
    public float verticalSpeed   = 5f;
    public float maxAltitude     = 100f;
    public float minAltitude     = 5f;
    public float rotationSpeed   = 80f;
    public float tiltAmount      = 12f;
    public float stabilizeSpeed  = 4f;

    [Header("Camera")]
    public Camera droneCamera;
    public float  cameraSensitivity = 2f;
    public float  cameraMinY = -80f;
    public float  cameraMaxY = 10f;

    [Header("Munition")]
    public GameObject munitionPrefab;      // assign a sphere or custom prefab
    public Transform  dropPoint;           // child empty transform below drone
    public int        maxPayload = 4;
    public float      lockOnRange = 60f;   // auto-lock if within range

    [Header("Audio")]
    public AudioSource motorAudioSource;
    public AudioClip   dropSound;
    public AudioClip   explosionSound;
    public AudioClip   motorClip;

    // HUD events
    public System.Action<int, int>            OnPayloadUpdate;     // current, max
    public System.Action<TargetData, float>   OnLockUpdate;        // target, distance
    public System.Action<TargetData>          OnTargetDestroyed;
    public System.Action<Vector3, float, float> OnHUDUpdate;       // pos, speed, battery

    // State
    private Rigidbody        _rb;
    private float            _cameraPitch, _cameraYaw;
    private int              _payload;
    private float            _battery = 100f;
    private TargetData[]     _targetList;
    private List<TargetData> _destroyed = new List<TargetData>();
    private TargetData       _lockedTarget;
    private bool             _dropping;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity     = false;
        _rb.linearDamping  = 3f;
        _rb.angularDamping = 5f;
        _rb.interpolation  = RigidbodyInterpolation.Interpolate;
        _rb.constraints    = RigidbodyConstraints.FreezeRotationX |
                             RigidbodyConstraints.FreezeRotationZ;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _payload    = maxPayload;
        _targetList = GameManager.Instance?.GetMarkedTargets() ?? new TargetData[0];
        _cameraYaw  = transform.eulerAngles.y;

        if (motorAudioSource != null && motorClip != null)
        {
            motorAudioSource.clip   = motorClip;
            motorAudioSource.loop   = true;
            motorAudioSource.volume = 0.4f;
            motorAudioSource.Play();
        }

        if (droneCamera == null) droneCamera = Camera.main;
        OnPayloadUpdate?.Invoke(_payload, maxPayload);
    }

    void Update()
    {
        HandleCamera();
        UpdateLockOn();
        HandleDrop();
        DrainBattery();

        OnHUDUpdate?.Invoke(transform.position, _rb.linearVelocity.magnitude, _battery);

        if (Input.GetKeyDown(KeyCode.Escape))
            ExitMission();
    }

    void FixedUpdate() => HandleMovement();

    // ── Movement (same as Scout) ─────────────────────────────────
    void HandleMovement()
    {
        float h      = Input.GetAxis("Horizontal");
        float v      = Input.GetAxis("Vertical");
        float altUp  = Input.GetKey(KeyCode.Space) && !_dropping ? 0 : 0;  // space used for drop
        // Use R/F for altitude in bomber
        float up   = Input.GetKey(KeyCode.R) ? 1f : 0f;
        float down = Input.GetKey(KeyCode.F) ? 1f : 0f;

        float alt = transform.position.y;
        if (alt >= maxAltitude) up   = 0;
        if (alt <= minAltitude) down = 0;

        Vector3 fwd   = new Vector3(Mathf.Sin(_cameraYaw * Mathf.Deg2Rad), 0,
                                     Mathf.Cos(_cameraYaw * Mathf.Deg2Rad));
        Vector3 right = new Vector3(fwd.z, 0, -fwd.x);

        Vector3 move = (fwd * v + right * h) * horizontalSpeed;
        move.y = (up - down) * verticalSpeed;

        _rb.AddForce(move - _rb.linearVelocity * 2f, ForceMode.Acceleration);

        // Yaw
        float yaw = 0f;
        if (Input.GetKey(KeyCode.Q)) yaw = -1f;
        if (Input.GetKey(KeyCode.E)) yaw =  1f;
        transform.Rotate(0, yaw * rotationSpeed * Time.fixedDeltaTime, 0);

        // Tilt
        Quaternion targetRot = Quaternion.Euler(-v * tiltAmount, transform.eulerAngles.y, -h * tiltAmount);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stabilizeSpeed * Time.fixedDeltaTime);

        if (motorAudioSource != null)
            motorAudioSource.pitch = Mathf.Lerp(0.9f, 1.4f, _rb.linearVelocity.magnitude / 15f);
    }

    // ── Camera ───────────────────────────────────────────────────
    void HandleCamera()
    {
        _cameraYaw   += Input.GetAxis("Mouse X") * cameraSensitivity;
        _cameraPitch -= Input.GetAxis("Mouse Y") * cameraSensitivity;
        _cameraPitch  = Mathf.Clamp(_cameraPitch, cameraMinY, cameraMaxY);
        if (droneCamera != null)
            droneCamera.transform.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
    }

    // ── Lock On ──────────────────────────────────────────────────
    void UpdateLockOn()
    {
        _lockedTarget = null;
        float closest = float.MaxValue;

        foreach (var t in _targetList)
        {
            if (IsDestroyed(t)) continue;
            float dist = Vector3.Distance(transform.position, t.worldPosition);
            if (dist < lockOnRange && dist < closest)
            {
                closest       = dist;
                _lockedTarget = t;
            }
        }

        OnLockUpdate?.Invoke(_lockedTarget, closest < float.MaxValue ? closest : -1f);
    }

    bool IsDestroyed(TargetData t) => _destroyed.Contains(t);

    // ── Drop ─────────────────────────────────────────────────────
    void HandleDrop()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (_payload <= 0 || _dropping) return;

        _dropping = true;
        _payload--;
        OnPayloadUpdate?.Invoke(_payload, maxPayload);

        if (dropSound != null) motorAudioSource?.PlayOneShot(dropSound);

        // Spawn munition
        Vector3   spawnPos = dropPoint != null ? dropPoint.position : transform.position - Vector3.up;
        GameObject mun     = munitionPrefab != null
            ? Instantiate(munitionPrefab, spawnPos, Quaternion.identity)
            : CreateDefaultMunition(spawnPos);

        var munScript = mun.AddComponent<Munition>();
        munScript.Init(_lockedTarget, OnMunitionLanded);

        StartCoroutine(ResetDrop());
    }

    IEnumerator ResetDrop()
    {
        yield return new WaitForSeconds(0.5f);
        _dropping = false;
    }

    void OnMunitionLanded(TargetData hit)
    {
        if (hit == null) return;
        _destroyed.Add(hit);
        OnTargetDestroyed?.Invoke(hit);

        if (explosionSound != null) motorAudioSource?.PlayOneShot(explosionSound);

        // Check all destroyed
        if (_destroyed.Count >= _targetList.Length && _targetList.Length > 0)
            StartCoroutine(CompleteMission());
    }

    IEnumerator CompleteMission()
    {
        yield return new WaitForSeconds(2f);
        ExitMission();
    }

    GameObject CreateDefaultMunition(Vector3 pos)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.transform.position   = pos;
        g.transform.localScale = Vector3.one * 0.3f;
        g.GetComponent<Renderer>().material.color = Color.black;
        return g;
    }

    void DrainBattery()
    {
        _battery -= 1.2f * Time.deltaTime;
        _battery  = Mathf.Max(0f, _battery);
        if (_battery <= 0f) ExitMission();
    }

    void ExitMission()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        GameManager.Instance?.LoadDroneSelect();
    }

    public int GetPayload() => _payload;
}
