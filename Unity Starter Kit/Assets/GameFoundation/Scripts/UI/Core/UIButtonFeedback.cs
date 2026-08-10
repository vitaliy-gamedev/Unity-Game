using System.Collections;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private float punchScale = 0.92f;
        [SerializeField] private float punchDuration = 0.08f;
        [SerializeField] private float hoverScale = 1.03f;
        [SerializeField] private bool playHoverSound = true;

        private Vector3 _baseScale;
        private Coroutine _routine;
        private Button _button;
        private bool _isHovered;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            // Гарантуємо правильний розмір при увімкненні об'єкта
            _isHovered = false;
            transform.localScale = _baseScale;
        }

        private void OnDisable()
        {
            // Зупиняємо анімації та скидаємо scale, якщо вікно/кнопку вимкнули
            if (_routine != null) StopCoroutine(_routine);
            _isHovered = false;
            transform.localScale = _baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable || !gameObject.activeInHierarchy) return;

            _isHovered = true;

            if (playHoverSound && ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.PlayUIHover();

            StartScaleRoutine(_baseScale * hoverScale, punchDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (!gameObject.activeInHierarchy) return;

            // Повертаємо до базового scale
            StartScaleRoutine(_baseScale, punchDuration);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_button.interactable || !gameObject.activeInHierarchy) return;

            if (ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.PlayUIClick();

            StartScaleRoutineSequence();
        }

        private void StartScaleRoutine(Vector3 targetScale, float duration)
        {
            if (!gameObject.activeInHierarchy) return;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ScaleTo(targetScale, duration));
        }

        private void StartScaleRoutineSequence()
        {
            if (!gameObject.activeInHierarchy) return;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(PunchSequence());
        }

        private IEnumerator PunchSequence()
        {
            // 1. Стискаємо (Punch down)
            yield return ScaleTo(_baseScale * punchScale, punchDuration * 0.5f);

            // 2. Повертаємо у відповідний стан (якщо курсор все ще на кнопці — до Hover, якщо ні — до Base)
            Vector3 target = _isHovered ? (_baseScale * hoverScale) : _baseScale;
            yield return ScaleTo(target, punchDuration * 0.5f);
        }

        private IEnumerator ScaleTo(Vector3 targetScale, float duration)
        {
            Vector3 from = transform.localScale;
            float t = 0f;

            while (t < duration)
            {
                if (!gameObject.activeInHierarchy) yield break;

                t += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(from, targetScale, t / duration);
                yield return null;
            }

            transform.localScale = targetScale;
        }
    }
}