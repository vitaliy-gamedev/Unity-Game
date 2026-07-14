using UnityEngine;
using UnityEngine.InputSystem;

public class GameplayActions
{
    private readonly InputActionAsset _asset;
    private readonly InputActionMap _gameplayMap;
    private readonly InputActionMap _uiMap;

    public InputAction Move { get; }
    public InputAction Sprint { get; }
    public InputAction Interact { get; }
    public InputAction Pause { get; }
    public InputAction Navigate { get; }

    public GameplayActions(InputActionAsset asset)
    {
        _asset = asset;
        _gameplayMap = asset.FindActionMap("Player");
        _uiMap = asset.FindActionMap("UI");

        if (_gameplayMap == null)
        {
            Debug.LogError("[GameplayActions] 'Player' action map not found!");
            return;
        }

        Move = GetAction("Move");
        Sprint = GetAction("Sprint");
        Interact = GetAction("Interact");
        Pause = GetAction("Pause") ?? GetAction("Menu");
        Navigate = _uiMap?.FindAction("Navigate");
    }

    public void EnableGameplay() => _gameplayMap?.Enable();
    public void DisableGameplay() => _gameplayMap?.Disable();
    public void EnableUI() => _uiMap?.Enable();
    public void DisableUI() => _uiMap?.Disable();

    public void DisableAll()
    {
        _gameplayMap?.Disable();
        _uiMap?.Disable();
    }

    private InputAction GetAction(string name)
    {
        var action = _gameplayMap.FindAction(name);
        if (action == null)
            Debug.LogWarning($"[GameplayActions] Action '{name}' not found in 'Player'.");
        return action;
    }
}