using GameFoundation.Core;
using GameFoundation.Pro.Achievements;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>
    /// Put this on the Canvas root (or any always-active GameObject) in
    /// MainMenuScene. Windows start closed, so this component opens the first
    /// screen after scene startup. UIService can resolve inactive scene windows
    /// even when Awake order prevented an early registration.
    /// </summary>
    public class MainMenuEntryPoint : MonoBehaviour
    {
        private void Start()
        {
            var uiService = ServiceLocator.Get<UIService>();
            if (uiService == null)
            {
                Debug.LogError("[MainMenuEntryPoint] UIService is not registered. Did MainMenuScene load without going through Bootstrap first?");
                return;
            }

            // Some legacy sample scenes contain this entry point on both the Canvas
            // and a standalone object. Only the first one should build the Pro overlay.
            if (FindFirstObjectByType<AchievementToastPresenter>() != null)
                return;

            var toastPresenter = GetComponent<AchievementToastPresenter>();
            if (toastPresenter == null)
                toastPresenter = gameObject.AddComponent<AchievementToastPresenter>();

            toastPresenter.Initialize();
            uiService.Open<MainMenuWindow>();
            ServiceLocator.Get<IAchievementService>()?.Unlock("first_launch");
        }
    }
}
