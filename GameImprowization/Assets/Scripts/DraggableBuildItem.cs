using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DraggableBuildItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector, SerializeField] private BuildingData data;

    [Header("Привид у світі")]
    [Tooltip("Sorting Layer, на якому малюється привид (як у твоїх спрайтів світу)")]
    [SerializeField] private string ghostSortingLayer = "Default";
    [SerializeField] private int ghostSortingOrder = 100;

    private Camera _cam;
    private GameObject _ghost;
    private SpriteRenderer _ghostRenderer;
    private BuildSlot _hoveredSlot;
    
    public void SetData(BuildingData newData)
    {
        data = newData;
        var img = GetComponent<Image>();
        if (data != null && data.icon != null) img.sprite = data.icon;
    }

    private void Awake() => _cam = Camera.main;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (data == null) return;
        if (_cam == null) _cam = Camera.main;

        _ghost = new GameObject("BuildGhost");
        _ghostRenderer = _ghost.AddComponent<SpriteRenderer>();
        _ghostRenderer.sprite = data.worldSprite != null ? data.worldSprite : data.icon;
        _ghostRenderer.color = new Color(1f, 1f, 1f, 0.6f);
        _ghostRenderer.sortingLayerName = ghostSortingLayer;
        _ghostRenderer.sortingOrder = ghostSortingOrder;

        MoveGhost(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghost == null) return;

        MoveGhost(eventData.position);

        var slot = SlotUnder(eventData.position);
        if (slot != _hoveredSlot)
        {
            if (_hoveredSlot != null) _hoveredSlot.ResetHighlight();
            _hoveredSlot = slot;
        }
        if (_hoveredSlot != null)
            _hoveredSlot.ShowHighlight(_hoveredSlot.CanAccept(data));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var slot = SlotUnder(eventData.position);
        if (slot != null && slot.CanAccept(data))
        {
            bool paid = data.costAmount <= 0
                        || ResourceManager.Instance == null
                        || ResourceManager.Instance.TrySpend(data.costType, data.costAmount);

            if (paid) slot.Place(data);
        }

        if (_hoveredSlot != null) _hoveredSlot.ResetHighlight();
        _hoveredSlot = null;

        if (_ghost != null) Destroy(_ghost);
        _ghost = null;
    }

    private void MoveGhost(Vector2 screenPos) => _ghost.transform.position = ScreenToWorld(screenPos);

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        float depth = _cam.orthographic ? 10f : -_cam.transform.position.z;
        Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        wp.z = 0f;
        return wp;
    }

    private BuildSlot SlotUnder(Vector2 screenPos)
    {
        Vector3 wp = ScreenToWorld(screenPos);
        var hit = Physics2D.OverlapPoint(wp);
        return hit != null ? hit.GetComponentInParent<BuildSlot>() : null;
    }
}
