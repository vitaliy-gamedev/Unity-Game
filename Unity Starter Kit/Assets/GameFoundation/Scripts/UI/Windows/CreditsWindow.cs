using GameFoundation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    public class CreditsWindow : BaseWindow
    {
        [SerializeField] private Button backButton;

        protected override void Awake()
        {
            base.Awake();

            bool ok = GFLogger.RequireField(backButton, nameof(CreditsWindow), nameof(backButton));

            var uiService = ServiceLocator.Get<UIService>();
            uiService?.Register(this);

            if (!ok) return;

            backButton.onClick.AddListener(() => uiService.Back());
        }
    }
}
