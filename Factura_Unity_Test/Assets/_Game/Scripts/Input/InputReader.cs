using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Factura
{
    /// <summary>
    /// Перетворює Input System на прості події запуску та наведення.
    /// Миша задає точку на екрані, а дотик — горизонтальне зміщення пальця.
    /// Турель не читає пристрої напряму, тому одна бойова логіка обслуговує обидва способи.
    /// </summary>
    public sealed class InputReader : MonoBehaviour
    {
        private InputAction press;
        private InputAction position;
        private bool isHeld;
        // Запам'ятовуємо пристрій, що почав жест: миша не повинна перебити активний дотик.
        private InputDevice activeDevice;
        private Vector2 previousPosition;
        private readonly List<RaycastResult> uiHits = new();

        public event Action Pressed;
        public event Action<float> AimDelta;
        public event Action<Vector2> PointerAim;

        private void Awake()
        {
            // Для двох дій достатньо локальних InputAction без окремого згенерованого
            // класу input-карти. <Pointer> об'єднує мишу та основний дотик.
            press = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            position = new InputAction("Position", InputActionType.PassThrough, "<Pointer>/position");
            press.performed += OnPress;
            press.canceled += OnRelease;
            position.performed += OnPosition;
        }

        private void OnEnable()
        {
            // Позицію вмикаємо раніше кнопки, щоб на початку жесту читати актуальні координати.
            position.Enable();
            press.Enable();
            previousPosition = position.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            // Вимкнений компонент не приймає ввід і не зберігає старе утримання.
            press.Disable();
            position.Disable();
            isHeld = false;
            activeDevice = null;
        }

        private void OnDestroy()
        {
            // Дії створені цим компонентом, тому він же звільняє їхні ресурси.
            press.Dispose();
            position.Dispose();
        }

        private void OnPress(InputAction.CallbackContext context)
        {
            if (isHeld) return;
            activeDevice = context.control.device;
            isHeld = true;
            previousPosition = ((Pointer)activeDevice).position.ReadValue();
            // Натискання Sound/Exit не повинно також запускати автомобіль.
            if (!IsOverButton(previousPosition)) Pressed?.Invoke();
            if (activeDevice is Mouse) PointerAim?.Invoke(previousPosition);
            // Нульова дельта вибирає touch-режим без стрибка кута при торканні.
            else AimDelta?.Invoke(0);
        }

        private void OnRelease(InputAction.CallbackContext context)
        {
            // Відпускання іншого пристрою не завершує поточний жест.
            if (context.control.device != activeDevice) return;
            isHeld = false;
            activeDevice = null;
        }

        private bool IsOverButton(Vector2 screenPosition)
        {
            // Перевіряємо саме Button (включно з його дочірнім текстом), а не весь HUD:
            // декоративна панель не повинна перекривати tap-to-start по дорозі.
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
                // На ПК можна наводити без утримання ЛКМ, у тому числі позаду авто.
                if (!isHeld || activeDevice is Mouse) PointerAim?.Invoke(currentPosition);
                return;
            }
            if (!isHeld || context.control.device != activeDevice) return;
            // Ділимо на ширину екрана: жест на однакову частку ширини має ту саму
            // чутливість за різних роздільних здатностей. Це не залежить від кількості пікселів.
            AimDelta?.Invoke((currentPosition.x - previousPosition.x) / Mathf.Max(1, Screen.width));
            previousPosition = currentPosition;
        }
    }
}
