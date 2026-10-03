using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Deadband.Squad
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SquadSelectionController))]
    public sealed class SquadCommandController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SquadSelectionController selection;
        [SerializeField, Min(0.5f)] private float formationSpacing = 1.65f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 2f;

        private void Awake()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (selection == null)
            {
                selection = GetComponent<SquadSelectionController>();
            }
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    TryIssueMove(mouse.position.ReadValue());
                }
            }

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                StopSelected();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                RegroupSelected();
            }
        }

        private void TryIssueMove(Vector2 pointerPosition)
        {
            if (worldCamera == null || selection.SelectedUnits.Count == 0)
            {
                return;
            }

            Ray ray = worldCamera.ScreenPointToRay(pointerPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                MoveSelectedTo(navHit.position);
            }
        }

        private void MoveSelectedTo(Vector3 destination)
        {
            IReadOnlyList<SquadUnit> units = selection.SelectedUnits;
            Vector3 center = GetCenter(units);
            Vector3 forward = destination - center;
            List<Vector3> slots = SquadFormation.CreateTwoColumnSlots(
                destination,
                forward,
                units.Count,
                formationSpacing);
            Dictionary<SquadUnit, Vector3> assignments = SquadFormation.AssignNearestSlots(units, slots);

            foreach (KeyValuePair<SquadUnit, Vector3> assignment in assignments)
            {
                Vector3 destinationSlot = destination;
                if (NavMesh.SamplePosition(assignment.Value, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
                {
                    destinationSlot = navHit.position;
                }

                assignment.Key.MoveTo(destinationSlot);
            }
        }

        private void StopSelected()
        {
            foreach (SquadUnit unit in selection.SelectedUnits)
            {
                unit.Stop();
            }
        }

        private void RegroupSelected()
        {
            if (selection.SelectedUnits.Count < 2)
            {
                return;
            }

            MoveSelectedTo(GetCenter(selection.SelectedUnits));
        }

        private static Vector3 GetCenter(IReadOnlyList<SquadUnit> units)
        {
            Vector3 center = Vector3.zero;
            foreach (SquadUnit unit in units)
            {
                center += unit.Position;
            }

            return units.Count > 0 ? center / units.Count : center;
        }
    }
}
