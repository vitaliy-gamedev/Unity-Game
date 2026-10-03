using UnityEngine;
using UnityEngine.InputSystem;

namespace Deadband.Combat
{
    public enum FieldSupplyKind
    {
        Ammunition,
        Medical
    }

    [DisallowMultipleComponent]
    public sealed class FieldSupplyPoint : MonoBehaviour
    {
        [SerializeField] private FieldSupplyKind kind;
        [SerializeField] private Transform player;
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerWeaponController playerWeapons;
        [SerializeField] private GameObject availableVisual;
        [SerializeField, Min(1f)] private float interactionRange = 3.2f;
        [SerializeField, Range(0.1f, 1f)] private float restoreFraction = 0.55f;

        public FieldSupplyKind Kind => kind;
        public bool IsAvailable { get; private set; } = true;

        public void Configure(
            FieldSupplyKind supplyKind,
            Transform playerTransform,
            Health health,
            PlayerWeaponController weapons,
            GameObject visual)
        {
            kind = supplyKind;
            player = playerTransform;
            playerHealth = health;
            playerWeapons = weapons;
            availableVisual = visual;
        }

        private void Update()
        {
            if (!IsAvailable || player == null ||
                Vector3.Distance(transform.position, player.position) > interactionRange) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.eKey.wasPressedThisFrame) return;

            bool consumed = kind == FieldSupplyKind.Medical
                ? playerHealth != null && playerHealth.Heal(playerHealth.MaximumHealth * restoreFraction)
                : playerWeapons != null && playerWeapons.RefillReserveAmmo(restoreFraction);
            if (!consumed) return;
            IsAvailable = false;
            if (availableVisual != null) availableVisual.SetActive(false);
        }

        private void OnGUI()
        {
            if (!IsAvailable || player == null ||
                Vector3.Distance(transform.position, player.position) > interactionRange) return;
            GUIStyle style = new(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = kind == FieldSupplyKind.Medical
                    ? new Color(0.38f, 1f, 0.55f)
                    : new Color(1f, 0.78f, 0.22f) }
            };
            string label = kind == FieldSupplyKind.Medical
                ? "[ E ] USE MEDICAL SUPPLIES"
                : "[ E ] TAKE AMMUNITION";
            GUI.Label(new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.72f, 380f, 30f), label, style);
        }
    }
}
