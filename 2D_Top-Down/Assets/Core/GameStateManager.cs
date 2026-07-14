using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Menu;

    public event Action<GameState> OnStateChanged;
    public event Action OnGamePaused;
    public event Action OnGameResumed;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        GameState previousState = CurrentState;
        CurrentState = newState;

        Debug.Log($"[GameState] {previousState} -> {newState}");

        switch (newState)
        {
            case GameState.Menu:
                Time.timeScale = 1f;
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case GameState.Pause:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.Win:
            case GameState.Lose:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }

        OnStateChanged?.Invoke(newState);

        if (newState == GameState.Pause)
            OnGamePaused?.Invoke();
        else if (previousState == GameState.Pause)
            OnGameResumed?.Invoke();
    }

    public void SetPlaying() => ChangeState(GameState.Playing);
    public void SetPause() => ChangeState(GameState.Pause);
    public void SetWin() => ChangeState(GameState.Win);
    public void SetLose() => ChangeState(GameState.Lose);
    public void SetMenu() => ChangeState(GameState.Menu);

    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsPaused => CurrentState == GameState.Pause;
}

public enum GameState
{
    Menu,
    Playing,
    Pause,
    Win,
    Lose
}