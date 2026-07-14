using UnityEngine;

[RequireComponent(typeof(InputReader))]
public class PauseEventDispatcher : MonoBehaviour
{
    private InputReader _inputReader;

    private void Awake()
    {
        _inputReader = GetComponent<InputReader>();
    }

    private void OnEnable()
    {
        _inputReader.OnPause += HandlePause;
    }

    private void OnDisable()
    {
        _inputReader.OnPause -= HandlePause;
    }

    private void HandlePause()
    {
        if (GameStateManager.Instance == null) return;

        if (GameStateManager.Instance.IsPlaying)
        {
            GameStateManager.Instance.SetPause();
        }
        else if (GameStateManager.Instance.IsPaused)
        {
            GameStateManager.Instance.SetPlaying();
        }
    }
}