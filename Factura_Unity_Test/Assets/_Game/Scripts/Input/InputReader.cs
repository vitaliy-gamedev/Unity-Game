using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Factura
{
    public sealed class InputReader : MonoBehaviour
    {
        private InputAction press;
        private InputAction position;
        private bool isHeld;
        private InputDevice activeDevice;
        private Vector2 previousPosition;

        public event Action Pressed;
        public event Action<float> AimDelta;
        public event Action<Vector2> PointerAim;

        private void Awake()
        {
            press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            // PassThrough lets the pressed pointer own the drag even when a mouse and touch coexist.
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
            Pressed?.Invoke();
            if (activeDevice is Mouse) PointerAim?.Invoke(previousPosition);
            else AimDelta?.Invoke(0); // A touch takes ownership even before its first movement.
        }

        private void OnRelease(InputAction.CallbackContext context)
        {
            if (context.control.device != activeDevice) return;
            isHeld = false;
            activeDevice = null;
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
            AimDelta?.Invoke((currentPosition.x - previousPosition.x) / Mathf.Max(1, Screen.width));
            previousPosition = currentPosition;
        }
    }
}
