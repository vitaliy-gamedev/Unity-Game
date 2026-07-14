using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    [Header("References")] [SerializeField]
    private InputActionAsset _inputAsset;

    public event Action<Vector2> OnMove;
    public event Action<bool> OnSprint;
    public event Action OnInteract;
    public event Action OnPause;

    private GameplayActions _gameplayActions;
    private Vector2 _moveInput;
    private bool _sprintHeld;

    private void Awake()
    {
        if (_inputAsset == null)
        {
            Debug.LogError("[InputReader] No InputActionAsset assigned!");
            return;
        }

        _gameplayActions = new GameplayActions(_inputAsset);
    }

    private void OnEnable()
    {
        if (_gameplayActions == null) return;

        _gameplayActions.Move.started += OnMoveInput;
        _gameplayActions.Move.performed += OnMoveInput;
        _gameplayActions.Move.canceled += OnMoveInput;

        _gameplayActions.Sprint.started += OnSprintStarted;
        _gameplayActions.Sprint.canceled += OnSprintCanceled;

        _gameplayActions.Interact.performed += OnInteractInput;
        _gameplayActions.Pause.performed += OnPauseInput;

        _gameplayActions.EnableGameplay();
    }

    private void OnDisable()
    {
        if (_gameplayActions == null) return;

        _gameplayActions.Move.started -= OnMoveInput;
        _gameplayActions.Move.performed -= OnMoveInput;
        _gameplayActions.Move.canceled -= OnMoveInput;

        _gameplayActions.Sprint.started -= OnSprintStarted;
        _gameplayActions.Sprint.canceled -= OnSprintCanceled;

        _gameplayActions.Interact.performed -= OnInteractInput;
        _gameplayActions.Pause.performed -= OnPauseInput;

        _gameplayActions.DisableGameplay();
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
        OnMove?.Invoke(_moveInput);
    }

    private void OnSprintStarted(InputAction.CallbackContext context)
    {
        _sprintHeld = true;
        OnSprint?.Invoke(true);
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        _sprintHeld = false;
        OnSprint?.Invoke(false);
    }

    private void OnInteractInput(InputAction.CallbackContext context)
    {
        OnInteract?.Invoke();
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        OnPause?.Invoke();
    }

    public Vector2 GetMoveInput() => _moveInput;
    public bool IsSprinting() => _sprintHeld;
    public void DisableInput() => _gameplayActions?.DisableGameplay();
    public void EnableInput() => _gameplayActions?.EnableGameplay();
}