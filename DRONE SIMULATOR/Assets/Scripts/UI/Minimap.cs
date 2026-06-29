using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple radar/minimap using a RawImage + secondary Camera.
/// Add a "MinimapCamera" child to the drone and assign it here.
/// Or use the auto-setup in Start().
/// </summary>
public class Minimap : MonoBehaviour
{
    [Header("References")]
    public RawImage minimapImage;   // UI RawImage in HUD
    public int      renderTexSize = 256;
    public float    cameraHeight  = 80f;

    private Camera          _minimapCam;
    private RenderTexture   _rt;
    private Transform       _droneTransform;

    void Start()
    {
        SetupCamera();
    }

    void SetupCamera()
    {
        // Find drone
        var scout  = FindObjectOfType<ScoutDroneController>();
        var bomber = FindObjectOfType<BomberDroneController>();
        _droneTransform = scout != null ? scout.transform : bomber?.transform;

        if (_droneTransform == null) return;

        // Create minimap camera
        var camGO = new GameObject("MinimapCamera");
        camGO.transform.SetParent(_droneTransform, false);
        camGO.transform.localPosition = new Vector3(0, cameraHeight, 0);
        camGO.transform.localRotation = Quaternion.Euler(90f, 0, 0);

        _minimapCam                    = camGO.AddComponent<Camera>();
        _minimapCam.orthographic       = true;
        _minimapCam.orthographicSize   = 60f;
        _minimapCam.clearFlags         = CameraClearFlags.SolidColor;
        _minimapCam.backgroundColor    = new Color(0.1f, 0.12f, 0.08f);
        _minimapCam.cullingMask        = -1; // all layers
        _minimapCam.depth              = -2;

        // Render texture
        _rt = new RenderTexture(renderTexSize, renderTexSize, 16);
        _minimapCam.targetTexture = _rt;

        if (minimapImage != null)
            minimapImage.texture = _rt;
    }

    void LateUpdate()
    {
        // Camera follows drone horizontally, fixed height
        if (_minimapCam != null && _droneTransform != null)
        {
            _minimapCam.transform.position = new Vector3(
                _droneTransform.position.x, cameraHeight, _droneTransform.position.z);
        }
    }

    void OnDestroy()
    {
        if (_rt != null) _rt.Release();
    }
}
