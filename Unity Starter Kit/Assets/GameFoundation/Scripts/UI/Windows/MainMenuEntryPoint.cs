using GameFoundation.Core;
using UnityEngine;

namespace GameFoundation.UI
{
    /// <summary>
    /// Put this on the Canvas root (or any always-active GameObject) in
    /// MainMenuScene. Every window deactivates itself in its own Awake() — by
    /// design, so they all start closed — which means something has to
    /// explicitly open the first one. Start() runs after every window's Awake()
    /// has already registered it with UIService, so this is guaranteed to work
    /// regardless of GameObject order in the hierarchy.
    ///
    /// Without this component, MainMenuScene loads with every window inactive
    /// and the player sees a blank screen.
    /// </summary>
    public class MainMenuEntryPoint : MonoBehaviour
    {
        private void Start()
        {
            var uiService = ServiceLocator.Get<UIService>();
            if (uiService == null)
            {
                Debug.LogError("[MainMenuEntryPoint] UIService is not registered — did MainMenuScene load without going through Bootstrap first?");
                return;
            }

            uiService.Open<MainMenuWindow>();
        }
    }
}
