using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scout drone (Mavic-type). WASD/Arrows = horizontal, Q/E = altitude,
/// Mouse = camera look. [E] marks a detected target.
/// Attach to the drone root GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ScoutDroneController : MonoBehaviour
{
    [Header("Flight")]
    public float horizontalSpeed = 12f;
    public float verticalSpeed   = 6f;
    public float maxAltitude     = 80f;
    public float minAltitude     = 2f;
    public float rotationSpeed   = 90f;    // yaw degrees/sec
    public float tiltAmount      = 15f;    // visual tilt on movement
    public float stabilizeSpeed  = 5f;

    [Header("Camera")]
    public Camera droneCamera;             // FPV / third-person cam
    public float  cameraSensitivity = 2f;
    public float  cameraMinY        = -60f;
    public float  cameraMaxY        = 20f;

    [Header("Sensor")]
    public float detectionRange  = 40f;
    public float detectionAngle  = 30f;    // half-cone below drone
    public LayerMask targetLayer;          // assign "Target" layer

    [Header("Audio")]
    public AudioSource motorAudioSource;
    public AudioClip   markSound;
    public AudioClip   motorClip;

    // State
    private Rigidbody  _rb;
    private float      _cameraPitch = 0f;
    private float      _cameraYaw   = 0f;
    private GameObject _currentTarget;
    private List<TargetData> _markedTargets = new List<TargetData>();
    private HashSet<GameObject> _markedObjects = new HashSet<GameObject>();

    // HUD event
    public System.Action<GameObject> OnTargetDetected;
    public System.Action<TargetData>  OnTargetMarked;
    public System.Action<Vector3, float, float> OnHUDUpdate; // pos, speed, battery

    private float _battery = 100f;
    private float _batteryDrainRate = 1.5f; // %/sec

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity        = false;
        _rb.linearDamping     = 3f;
        _rb.angularDamping    = 5f;
        _rb.interpolation     = RigidbodyInterpolation.Interpolate;
        _rb.constraints       = RigidbodyConstraints.FreezeRotationX |
                                RigidbodyConstraints.FreezeRotationZ;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (motorAudioSource != null && motorClip != null)
        {
            motorAudioSource.clip   = motorClip;
            motorAudioSource.loop   = true;
            motorAudioSource.volume = 0.4f;
            motorAudioSource.Play();
        }

        if (droneCamera == null) droneCamera = Camera.main;
        _cameraYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        HandleCamera();
        HandleTargetDetection();
        HandleMarking();
        DrainBattery();

        float spd = _rb.linearVelocity.magnitude;
        OnHUDUpdate?.Invoke(transform.position, spd, _battery);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SaveAndExit();
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    // ── Movement ────────────────────────────────────────────────
    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float yaw = 0f;

        if (Input.GetKey(KeyCode.Q)) yaw = -1f;
        if (Input.GetKey(KeyCode.E) && !Input.GetKeyDown(KeyCode.E)) yaw =  1f; // E held (not tap)

        float altUp   = Input.GetKey(KeyCode.Space)      ? 1f : 0f;
        float altDown = Input.GetKey(KeyCode.LeftShift)  ? 1f : 0f;

        // Clamp altitude
        float alt = transform.position.y;
        if (alt >= maxAltitude && altUp   > 0) altUp   = 0;
        if (alt <= minAltitude && altDown > 0) altDown = 0;

        // Directional force relative to yaw
        Vector3 forward = new Vector3(Mathf.Sin(_cameraYaw * Mathf.Deg2Rad), 0,
                                       Mathf.Cos(_cameraYaw * Mathf.Deg2Rad));
        Vector3 right   = new Vector3(forward.z, 0, -forward.x);

        Vector3 move = (forward * v + right * h) * horizontalSpeed;
        move.y = (altUp - altDown) * verticalSpeed;

        _rb.AddForce(move - _rb.linearVelocity * 2f, ForceMode.Acceleration);

        // Yaw rotation
        transform.Rotate(0, yaw * rotationSpeed * Time.fixedDeltaTime, 0);

        // Visual tilt
        float tiltX = -v * tiltAmount;
        float tiltZ = -h * tiltAmount;
        Quaternion targetRot = Quaternion.Euler(tiltX, transform.eulerAngles.y, tiltZ);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stabilizeSpeed * Time.fixedDeltaTime);

        // Motor pitch
        if (motorAudioSource != null)
            motorAudioSource.pitch = Mathf.Lerp(0.9f, 1.4f, _rb.linearVelocity.magnitude / 15f);
    }

    // ── Camera ───────────────────────────────────────────────────
    void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;

        _cameraYaw   += mouseX;
        _cameraPitch -= mouseY;
        _cameraPitch  = Mathf.Clamp(_cameraPitch, cameraMinY, cameraMaxY);

        if (droneCamera != null)
            droneCamera.transform.rotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0f);
    }

    // ── Target Detection ─────────────────────────────────────────
    void HandleTargetDetection()
    {
        _currentTarget = null;
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);
        float closestDist = float.MaxValue;

        foreach (var col in hits)
        {
            Vector3 dir = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(Vector3.down, dir);

            if (angle < detectionAngle)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist && !_markedObjects.Contains(col.gameObject))
                {
                    closestDist    = dist;
                    _currentTarget = col.gameObject;
                }
            }
        }

        OnTargetDetected?.Invoke(_currentTarget);
    }

    // ── Marking ──────────────────────────────────────────────────
    void HandleMarking()
    {
        if (_currentTarget == null) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (_markedObjects.Contains(_currentTarget)) return;

        _markedObjects.Add(_currentTarget);

        // Generate grid coord
        int col  = Mathf.Clamp(Mathf.RoundToInt(_currentTarget.transform.position.x / 20f), 0, 7);
        int row  = Mathf.Clamp(Mathf.RoundToInt(_currentTarget.transform.position.z / 20f), 0, 7);
        string grid = ((char)('A' + col)).ToString() + (row + 1).ToString();

        string type = _currentTarget.GetComponent<TargetEntity>()?.targetType ?? "Unknown";

        var data = new TargetData(_currentTarget.transform.position, grid, type);
        _markedTargets.Add(data);
        OnTargetMarked?.Invoke(data);

        // Visual feedback on target
        var renderer = _currentTarget.GetComponentInChildren<Renderer>();
        if (renderer != null) renderer.material.color = Color.red;

        if (markSound != null && motorAudioSource != null)
            motorAudioSource.PlayOneShot(markSound);

        Debug.Log($"[Scout] Marked: {data.ToDisplayString(GameManager.Language.Ukrainian)}");
    }

    void DrainBattery()
    {
        _battery -= _batteryDrainRate * Time.deltaTime;
        _battery  = Mathf.Max(0f, _battery);
        if (_battery <= 0f) SaveAndExit();
    }

    // ── Save & Exit ──────────────────────────────────────────────
    void SaveAndExit()
    {
        GameManager.Instance?.SaveMarkedTargets(_markedTargets.ToArray());
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        GameManager.Instance?.LoadDroneSelect();
    }

    public List<TargetData> GetMarkedTargets() => _markedTargets;
    public float GetBattery() => _battery;
}
