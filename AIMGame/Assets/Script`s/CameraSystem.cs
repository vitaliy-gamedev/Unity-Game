using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private Camera _topCamera;

    [SerializeField] private float _normalFov = 60f;
    [SerializeField] private float _zoomFov = 20f;
    [SerializeField] private float _zoomSpeed = 10f;

    [SerializeField] private KeyCode _zoomKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode _topCameraKey = KeyCode.G;

    [SerializeField] private bool _showCrosshair = true;
    [SerializeField] private bool _showOnlyWhenZooming = false;

    // FIX #2: було true — не відповідало реальному стану камери при старті
    [SerializeField] private bool _isTopView = false;

    [SerializeField] private string _crosshairText = "+";
    [SerializeField] private int _crosshairFontSize = 40;
    [SerializeField] private Color _crosshairColor = Color.red;

    [SerializeField] private Vector3 _topCameraRotation = new Vector3(90f, 0f, 0f);

    void Start()
    {
        if (_playerCamera == null)
        {
            Debug.LogWarning("CameraSystem: No player camera assigned!");
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _playerCamera.fieldOfView = _normalFov;

        if (_topCamera != null)
        {
            // При старті топ-камера вимкнена, відповідно _isTopView = false
            _topCamera.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        HandleZoom();
        HandleTopView();
    }

    private void HandleZoom()
    {
        if (_isTopView) return;
        if (_playerCamera == null) return;

        bool isZooming = IsKeyHeld(_zoomKey);
        float targetFov = isZooming ? _zoomFov : _normalFov;

        _playerCamera.fieldOfView = Mathf.Lerp(
            _playerCamera.fieldOfView,
            targetFov,
            _zoomSpeed * Time.deltaTime
        );
    }

    private bool IsKeyHeld(KeyCode key)
    {
        return key switch
        {
            KeyCode.Mouse0 => Input.GetMouseButton(0),
            KeyCode.Mouse1 => Input.GetMouseButton(1),
            KeyCode.Mouse2 => Input.GetMouseButton(2),
            _ => Input.GetKey(key)
        };
    }

    private void HandleTopView()
    {
        if (!Input.GetKeyDown(_topCameraKey)) return;

        _isTopView = !_isTopView;

        if (_playerCamera != null)
            _playerCamera.gameObject.SetActive(!_isTopView);

        if (_topCamera != null)
        {
            // FIX #1: було SetActive(_topCamera) — передавався об'єкт замість bool
            _topCamera.gameObject.SetActive(_isTopView);
        }
    }

    private void LateUpdate()
    {
        if (!_isTopView) return;
        if (_topCamera == null) return;

        // FIX #5: ротація застосовується тут — FollowTop.cs не повинен її перевизначати
        _topCamera.transform.rotation = Quaternion.Euler(_topCameraRotation);
    }

    private void OnGUI()
    {
        if (!_showCrosshair) return;

        // FIX #3: було навпаки (ховало під час зуму) і використовувало GetMouseButtonDown замість GetKey
        if (_showOnlyWhenZooming && !IsKeyHeld(_zoomKey)) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = _crosshairFontSize
        };
        style.normal.textColor = _crosshairColor;

        Vector2 textSize = style.CalcSize(new GUIContent(_crosshairText));

        float centerX = (Screen.width - textSize.x) / 2f;
        float centerY = (Screen.height - textSize.y) / 2f;

        GUI.Label(new Rect(centerX, centerY, textSize.x, textSize.y), _crosshairText, style);
    }
}