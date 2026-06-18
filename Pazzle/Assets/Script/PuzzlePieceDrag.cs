using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class PuzzlePieceDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public bool IsPlaced { get; private set; }

    private RectTransform rectTransform;
    private RectTransform pieceLayer;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 targetPosition;
    private Vector2 startPosition;
    private float snapDistance;
    private bool lockWhenPlaced;
    private bool interactable = true;
    private Action<PuzzlePieceDrag> onPlaced;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Initialize(
        Canvas ownerCanvas,
        RectTransform ownerPieceLayer,
        CanvasGroup ownerCanvasGroup,
        Vector2 snapTarget,
        float snapRadius,
        bool shouldLockWhenPlaced,
        Action<PuzzlePieceDrag> placedCallback)
    {
        canvas = ownerCanvas;
        pieceLayer = ownerPieceLayer;
        canvasGroup = ownerCanvasGroup;
        targetPosition = snapTarget;
        snapDistance = snapRadius;
        lockWhenPlaced = shouldLockWhenPlaced;
        onPlaced = placedCallback;
        SetInteractable(true);
    }

    public void SetStartPosition(Vector2 position)
    {
        startPosition = position;
        rectTransform.anchoredPosition = position;
    }

    public void SetInteractable(bool value)
    {
        interactable = value;

        if (canvasGroup == null)
            return;

        canvasGroup.blocksRaycasts = value;
        canvasGroup.interactable = value;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;

        transform.SetAsLastSibling();
        startPosition = rectTransform.anchoredPosition;
        canvasGroup.alpha = 0.85f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;

        Camera cam = GetEventCamera(eventData);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                pieceLayer,
                eventData.position,
                cam,
                out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = interactable;

        if (Vector2.Distance(rectTransform.anchoredPosition, targetPosition) <= snapDistance)
            Place();
    }

    public void ReturnToStart()
    {
        if (IsPlaced)
            return;

        rectTransform.anchoredPosition = startPosition;
    }

    public void Place()
    {
        if (IsPlaced)
            return;

        rectTransform.anchoredPosition = targetPosition;
        IsPlaced = true;

        if (lockWhenPlaced)
            SetInteractable(false);

        onPlaced?.Invoke(this);
    }

    private bool CanDrag()
    {
        return interactable && !IsPlaced;
    }

    private Camera GetEventCamera(PointerEventData eventData)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return eventData.pressEventCamera != null
            ? eventData.pressEventCamera
            : canvas.worldCamera;
    }
}
