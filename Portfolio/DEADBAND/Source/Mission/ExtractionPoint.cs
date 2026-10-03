using System;
using System.Collections.Generic;
using Deadband.Combat;
using Deadband.Squad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadband.Missions
{
    [DisallowMultipleComponent]
    public sealed class ExtractionPoint : MonoBehaviour
    {
        [SerializeField] private PlayerUnitController controlledUnit;
        [SerializeField] private Renderer padRenderer;
        [SerializeField] private LineRenderer perimeter;
        [SerializeField] private Light beaconLight;
        [SerializeField] private GameObject guidanceBeacon;
        [SerializeField] private GameObject minimapMarker;
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private Material availableMaterial;

        private readonly HashSet<SquadUnit> occupants = new();

        public event Action ActivationRequested;

        public bool IsAvailable { get; private set; }
        public bool IsActive { get; private set; }
        public Vector3 Position => transform.position;

        public bool ContainsPosition(Vector3 worldPosition)
        {
            Collider trigger = GetComponent<Collider>();
            return trigger != null && trigger.bounds.Contains(worldPosition);
        }

        public void Configure(
            PlayerUnitController leader,
            Renderer pad,
            LineRenderer ring,
            Light lightReference,
            GameObject guidance,
            GameObject mapMarker,
            Material locked,
            Material available)
        {
            controlledUnit = leader;
            padRenderer = pad;
            perimeter = ring;
            beaconLight = lightReference;
            guidanceBeacon = guidance;
            minimapMarker = mapMarker;
            lockedMaterial = locked;
            availableMaterial = available;
            RefreshVisuals();
        }

        public void SetAvailable(bool available)
        {
            IsAvailable = available;
            RefreshVisuals();
        }

        public void SetExtractionActive(bool active)
        {
            IsActive = active;
            RefreshVisuals();
        }

        public int CountLivingSquadInside()
        {
            occupants.RemoveWhere(unit => unit == null || !unit.gameObject.activeInHierarchy);
            int count = 0;
            foreach (SquadUnit unit in occupants)
            {
                Health health = unit.GetComponent<Health>();
                if (health == null || health.IsAlive)
                {
                    count++;
                }
            }
            return count;
        }

        private void Update()
        {
            if (!IsAvailable || IsActive || !IsLeaderInside())
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                ActivationRequested?.Invoke();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            SquadUnit unit = other.GetComponentInParent<SquadUnit>();
            if (unit != null)
            {
                occupants.Add(unit);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            SquadUnit unit = other.GetComponentInParent<SquadUnit>();
            if (unit != null)
            {
                occupants.Remove(unit);
            }
        }

        private bool IsLeaderInside()
        {
            if (controlledUnit == null)
            {
                return false;
            }
            SquadUnit leader = controlledUnit.GetComponent<SquadUnit>();
            return leader != null && occupants.Contains(leader);
        }

        private void RefreshVisuals()
        {
            Material material = IsAvailable ? availableMaterial : lockedMaterial;
            if (padRenderer != null)
            {
                padRenderer.sharedMaterial = material;
            }
            if (perimeter != null)
            {
                perimeter.sharedMaterial = material;
            }
            if (beaconLight != null)
            {
                beaconLight.color = IsAvailable ? new Color(0.25f, 1f, 0.45f) : new Color(1f, 0.22f, 0.08f);
                beaconLight.intensity = IsActive ? 7f : IsAvailable ? 3.5f : 1.2f;
            }
            if (guidanceBeacon != null)
            {
                guidanceBeacon.SetActive(IsAvailable);
            }
            if (minimapMarker != null)
            {
                minimapMarker.SetActive(IsAvailable);
            }
        }

        private void OnGUI()
        {
            if (!IsAvailable || IsActive || !IsLeaderInside())
            {
                return;
            }

            GUIStyle promptStyle = new(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.52f, 1f, 0.64f) }
            };
            GUI.Label(
                new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.68f, 380f, 40f),
                "[ E ] START FINAL EXTRACTION  //  HOLD THE ZONE TO WIN",
                promptStyle);
        }
    }
}
