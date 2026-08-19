using System;
using GameFoundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TMP_Text titleText;

        private LevelData _data;
        private Action<LevelData> _onPicked;
        private bool _isValid;

        private void Awake()
        {
            _isValid = ResolveReferences(logMissing: true);
        }

        public void Setup(LevelData data, bool unlocked, Action<LevelData> onPicked)
        {
            if (!_isValid)
                _isValid = ResolveReferences(logMissing: true);

            if (!_isValid) return;

            _data = data;
            _onPicked = onPicked;

            if (thumbnailImage != null)
            {
                thumbnailImage.enabled = data.thumbnail != null;
                thumbnailImage.sprite = data.thumbnail;
            }

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(data.displaySceneName) ? $"Level {data.levelId}" : data.displaySceneName;
                titleText.transform.SetAsLastSibling();
            }

            button.interactable = unlocked;
            if (lockIcon != null)
            {
                lockIcon.SetActive(!unlocked);
                if (!unlocked)
                    lockIcon.transform.SetAsLastSibling();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onPicked?.Invoke(_data));
        }

        private bool ResolveReferences(bool logMissing)
        {
            if (button == null)
                button = GetComponent<Button>();

            if (thumbnailImage == null)
            {
                foreach (var image in GetComponentsInChildren<Image>(true))
                {
                    if (image.gameObject != gameObject)
                    {
                        thumbnailImage = image;
                        break;
                    }
                }
            }

            if (lockIcon == null)
            {
                var lockTransform = transform.Find("lockIcon");
                if (lockTransform != null)
                    lockIcon = lockTransform.gameObject;
            }

            if (titleText == null)
                titleText = GetComponentInChildren<TMP_Text>(true);

            if (!logMissing)
                return button != null && thumbnailImage != null && titleText != null;

            bool ok = true;
            ok &= GFLogger.RequireField(button, nameof(LevelButton), nameof(button));
            ok &= GFLogger.RequireField(thumbnailImage, nameof(LevelButton), nameof(thumbnailImage));
            ok &= GFLogger.RequireField(titleText, nameof(LevelButton), nameof(titleText));
            return ok;
        }
    }
}
