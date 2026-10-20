// Відділяє абсолютне наведення мишею від відносного touch-жесту та команд запуску.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Factura
{
    public sealed class InputReader : MonoBehaviour
    {
        private InputAction press;
        private InputAction position;
        private bool isHeld;
        private InputDevice activeDevice;
        private Vector2 previousPosition;
        private readonly List<RaycastResult> uiHits = new();

        public event Action Pressed;
        public event Action<float> AimDelta;
        public event Action<Vector2> PointerAim;

        private void Awake()
        {
            press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            position = new InputAction("Position", InputActionType.PassThrough, "<Pointer>/position");
            press.performed += OnPress;
            press.canceled += OnRelease;
            position.performed += OnPosition;
        }

        private void OnEnable()
        {
            position.Enable();
            press.Enable();
            previousPosition = position.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            press.Disable();
            position.Disable();
            isHeld = false;
            activeDevice = null;
        }

        private void OnDestroy()
        {
            press.Dispose();
            position.Dispose();
        }

        private void OnPress(InputAction.CallbackContext context)
        {
            if (isHeld) return;
            activeDevice = context.control.device;
            isHeld = true;
            previousPosition = ((Pointer)activeDevice).position.ReadValue();
            // Клік Sound/Exit не повинен одночасно запускати заїзд.
            if (!IsOverButton(previousPosition)) Pressed?.Invoke();
            if (activeDevice is Mouse) PointerAim?.Invoke(previousPosition);
            else AimDelta?.Invoke(0);
        }

        private void OnRelease(InputAction.CallbackContext context)
        {
            // Інший пристрій не завершує активний жест.
            if (context.control.device != activeDevice) return;
            isHeld = false;
            activeDevice = null;
        }

        private bool IsOverButton(Vector2 screenPosition)
        {
            if (!EventSystem.current) return false;
            var pointer = new PointerEventData(EventSystem.current) { position = screenPosition };
            uiHits.Clear();
            EventSystem.current.RaycastAll(pointer, uiHits);
            foreach (var hit in uiHits)
                if (hit.gameObject.GetComponentInParent<Button>()) return true;
            return false;
        }

        private void OnPosition(InputAction.CallbackContext context)
        {
            Vector2 currentPosition = context.ReadValue<Vector2>();
            if (context.control.device is Mouse)
            {
                if (!isHeld || activeDevice is Mouse) PointerAim?.Invoke(currentPosition);
                return;
            }
            if (!isHeld || context.control.device != activeDevice) return;
            // Нормалізація на ширину екрана зберігає чутливість за часткою пройденої ширини.
            AimDelta?.Invoke((currentPosition.x - previousPosition.x) / Mathf.Max(1, Screen.width));
            previousPosition = currentPosition;
        }
    }
}
