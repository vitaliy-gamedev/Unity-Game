using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DemolishManager : MonoBehaviour
{
    public static DemolishManager Instance { get; private set; }

    [Tooltip("Курсор знесення (необов'язково). Змінюється автоматично при вмиканні режиму")]
    [SerializeField] private Texture2D demolishCursor;

    public bool IsActive { get; private set; }
    
    public event System.Action<bool> OnModeChanged;

    private Camera _cam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _cam = Camera.main;
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        if (demolishCursor != null)
            Cursor.SetCursor(active ? demolishCursor : null, Vector2.zero, CursorMode.Auto);

        OnModeChanged?.Invoke(active);
    }
    
    public void Toggle() => SetActive(!IsActive);

    private void Update()
    {
        if (!IsActive || Mouse.current == null) return;
        
        if (Mouse.current.rightButton.wasPressedThisFrame ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            SetActive(false);
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector2 screen = Mouse.current.position.ReadValue();
        float depth = _cam.orthographic ? 10f : -_cam.transform.position.z;
        Vector3 world = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));

        var hit = Physics2D.OverlapPoint(world);
        if (hit == null) return;
        
        var building = hit.GetComponentInParent<Building>();
        if (building != null) { building.Demolish(); return; }

        var slot = hit.GetComponentInParent<BuildSlot>();
        if (slot != null && slot.IsOccupied) slot.Demolish();
    }
}
