using System;
using GameFoundation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private GameObject lockIcon;

        private LevelData _data;
        private Action<LevelData> _onPicked;
        private bool _isValid;

        private void Awake()
        {
            bool ok = true;
            ok &= GFLogger.RequireField(button, nameof(LevelButton), nameof(button));
            ok &= GFLogger.RequireField(thumbnailImage, nameof(LevelButton), nameof(thumbnailImage));
            ok &= GFLogger.RequireField(lockIcon, nameof(LevelButton), nameof(lockIcon));
            _isValid = ok;
        }

        public void Setup(LevelData data, bool unlocked, Action<LevelData> onPicked)
        {
            if (!_isValid) return; // already logged in Awake — don't cascade into more errors

            _data = data;
            _onPicked = onPicked;

            thumbnailImage.sprite = data.thumbnail;
            button.interactable = unlocked;
            lockIcon.SetActive(!unlocked);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onPicked?.Invoke(_data));
        }
    }
}
