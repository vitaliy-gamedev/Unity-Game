using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Deadband.Squad
{
    [DisallowMultipleComponent]
    public sealed class SquadSelectionController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField, Min(1f)] private float dragThreshold = 12f;
        [SerializeField] private Color selectionBoxColor = new(0.2f, 1f, 0.45f, 0.18f);
        [SerializeField] private Color selectionBoxBorderColor = new(0.2f, 1f, 0.45f, 0.9f);

        private readonly List<SquadUnit> allUnits = new();
        private readonly List<SquadUnit> selectedUnits = new();
        private Vector2 dragStart;
        private Vector2 dragCurrent;
        private bool isDragging;

        public IReadOnlyList<SquadUnit> SelectedUnits => selectedUnits;
        public event Action SelectionChanged;

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            RefreshUnits();
        }

        private void OnEnable()
        {
            RefreshUnits();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || worldCamera == null)
            {
                return;
            }

            if (Keyboard.current != null &&
                Keyboard.current.ctrlKey.isPressed &&
                Keyboard.current.aKey.wasPressedThisFrame)
            {
                SelectAll();
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                dragStart = mouse.position.ReadValue();
                dragCurrent = dragStart;
                isDragging = true;
            }

            if (isDragging && mouse.leftButton.isPressed)
            {
                dragCurrent = mouse.position.ReadValue();
            }

            if (isDragging && mouse.leftButton.wasReleasedThisFrame)
            {
                dragCurrent = mouse.position.ReadValue();
                bool additive = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

                if ((dragCurrent - dragStart).magnitude >= dragThreshold)
                {
                    SelectInsideScreenRect(additive);
                }
                else
                {
                    SelectUnderPointer(additive);
                }

                isDragging = false;
            }
        }

        public void SelectAll()
        {
            RefreshUnits();
            ClearSelection(false);

            foreach (SquadUnit unit in allUnits)
            {
                AddToSelection(unit);
            }

            SelectionChanged?.Invoke();
        }

        public void ClearSelection(bool notify = true)
        {
            foreach (SquadUnit unit in selectedUnits)
            {
                if (unit != null)
                {
                    unit.SetSelected(false);
                }
            }

            selectedUnits.Clear();
            if (notify)
            {
                SelectionChanged?.Invoke();
            }
        }

        private void RefreshUnits()
        {
            allUnits.Clear();
            allUnits.AddRange(FindObjectsByType<SquadUnit>(FindObjectsSortMode.None));
            selectedUnits.RemoveAll(unit => unit == null || !unit.isActiveAndEnabled);
        }

        private void SelectUnderPointer(bool additive)
        {
            Ray ray = worldCamera.ScreenPointToRay(dragCurrent);
            SquadUnit clickedUnit = null;

            if (Physics.Raycast(ray, out RaycastHit hit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                clickedUnit = hit.collider.GetComponentInParent<SquadUnit>();
            }

            if (!additive)
            {
                ClearSelection(false);
            }

            if (clickedUnit != null)
            {
                if (additive && selectedUnits.Contains(clickedUnit))
                {
                    RemoveFromSelection(clickedUnit);
                }
                else
                {
                    AddToSelection(clickedUnit);
                }
            }

            SelectionChanged?.Invoke();
        }

        private void SelectInsideScreenRect(bool additive)
        {
            RefreshUnits();
            if (!additive)
            {
                ClearSelection(false);
            }

            Rect selectionRect = GetScreenRect(dragStart, dragCurrent);
            foreach (SquadUnit unit in allUnits)
            {
                Vector3 screenPosition = worldCamera.WorldToScreenPoint(unit.Position + Vector3.up);
                if (screenPosition.z > 0f && selectionRect.Contains(screenPosition))
                {
                    AddToSelection(unit);
                }
            }

            SelectionChanged?.Invoke();
        }

        private void AddToSelection(SquadUnit unit)
        {
            if (unit == null || selectedUnits.Contains(unit))
            {
                return;
            }

            selectedUnits.Add(unit);
            unit.SetSelected(true);
        }

        private void RemoveFromSelection(SquadUnit unit)
        {
            if (!selectedUnits.Remove(unit))
            {
                return;
            }

            unit.SetSelected(false);
        }

        private static Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            return Rect.MinMaxRect(
                Mathf.Min(start.x, end.x),
                Mathf.Min(start.y, end.y),
                Mathf.Max(start.x, end.x),
                Mathf.Max(start.y, end.y));
        }

        private void OnGUI()
        {
            if (!isDragging || (dragCurrent - dragStart).magnitude < dragThreshold)
            {
                return;
            }

            Rect screenRect = GetScreenRect(dragStart, dragCurrent);
            screenRect.y = Screen.height - screenRect.yMax;
            DrawRect(screenRect, selectionBoxColor);
            DrawBorder(screenRect, 2f, selectionBoxBorderColor);
        }

        private void DrawInstructions()
        {
            GUIStyle titleStyle = new(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 1f, 0.68f) }
            };
            GUIStyle bodyStyle = new(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            DrawRect(new Rect(16f, 16f, 410f, 128f), new Color(0.015f, 0.03f, 0.025f, 0.86f));
            GUI.Label(new Rect(30f, 24f, 380f, 26f), "DEADBAND // SQUAD PROTOTYPE", titleStyle);
            GUI.Label(new Rect(30f, 54f, 380f, 82f),
                $"Selected: {selectedUnits.Count}/{allUnits.Count}\n" +
                "LMB / drag: select   Shift: add/remove   Ctrl+A: all\n" +
                "RMB: move   Space: hold   R: regroup   F: frame squad\n" +
                "WASD: pan   Q/E: rotate   Wheel: zoom",
                bodyStyle);
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawBorder(Rect rect, float thickness, Color color)
        {
            DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }
    }
}
